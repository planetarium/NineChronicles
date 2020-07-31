using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("rapid_combination")]
    public class RapidCombination : GameAction
    {
        [Serializable]
        public class ResultModel : CombinationConsumable.ResultModel
        {
            public Dictionary<Material, int> cost;

            protected override string TypeId => "rapidCombination.result";

            public ResultModel(Dictionary serialized) : base(serialized)
            {
                if (serialized.TryGetValue((Text) "cost", out var value))
                {
                    cost = value.ToDictionary_Material_int();
                }
            }

            public override IValue Serialize() =>
                new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "cost"] = cost.Serialize(),
                }.Union((Dictionary) base.Serialize()));
        }

        public Address avatarAddress;
        public int slotIndex;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress,
                out var agentState, out var avatarState))
            {
                return states;
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState?.Result is null)
            {
                Log.Warning("CombinationSlot Result is null.");
                return states;
            }
            if (slotState.UnlockBlockIndex <= context.BlockIndex)
            {
                Log.Warning($"Can't use combination slot. it unlock on {slotState.UnlockBlockIndex} block.");
                return states;
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                return states;
            }

            var diff = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
            if (diff < 0)
            {
                Log.Information("Skip rapid combination.");
                return states;
            }

            var count = CalculateHourglassCount(gameConfigState, diff);
            var tableSheets = TableSheets.FromActionContext(context);
            var row = tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            var hourGlass = ItemFactory.CreateMaterial(row);
            if (!avatarState.inventory.RemoveFungibleItem(hourGlass, count))
            {
                Log.Error($"Not enough item {hourGlass} : {count}");
                return states;
            }

            slotState.Update(context.BlockIndex, hourGlass, count);
            avatarState.UpdateFromRapidCombination(
                ((CombinationConsumable.ResultModel) slotState.Result),
                context.BlockIndex
            );
            return states
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["slotIndex"] = slotIndex.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            slotIndex = plainValue["slotIndex"].ToInteger();
        }

        public static int CalculateHourglassCount(GameConfigState state, long diff)
        {
            if (diff <= 0)
            {
                return 0;
            }

            var cost = Math.Ceiling((decimal) diff / state.HourglassPerBlock);
            return (int) cost;
        }
    }
}
