using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("rapid_combination")]
    public class RapidCombination0 : GameAction, IRapidCombinationV1
    {
        [Serializable]
        public class ResultModel : CombinationConsumable5.ResultModel
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
#pragma warning disable LAA1002
                new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "cost"] = cost.Serialize(),
                }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        public Address avatarAddress;
        public int slotIndex;

        Address IRapidCombinationV1.AvatarAddress => avatarAddress;
        int IRapidCombinationV1.SlotIndex => slotIndex;

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

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            Log.Warning("{AddressesHex}rapid_combination is deprecated. Please use rapid_combination2", addressesHex);
            if (!states.TryGetAgentAvatarStates(
                context.Signer,
                avatarAddress,
                out var agentState,
                out var avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState?.Result is null)
            {
                throw new CombinationSlotResultNullException($"{addressesHex}CombinationSlot Result is null. ({avatarAddress}), ({slotIndex})");
            }

            if (!slotState.Validate(avatarState, context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid. slot index: {slotIndex}, context block index: {context.BlockIndex}");
            }

            var diff = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
            if (diff <= 0)
            {
                throw new RequiredBlockIndexException($"{addressesHex}Already met the required block index. context block index: {context.BlockIndex}, required block index: {slotState.Result.itemUsable.RequiredBlockIndex}");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the GameConfigState was failed to load.");
            }

            var count = CalculateHourglassCount(gameConfigState, diff);
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            var hourGlass = ItemFactory.CreateMaterial(row);
            if (!avatarState.inventory.RemoveFungibleItem2(hourGlass, count))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({row.Id} * {count})");
            }

            slotState.Update(context.BlockIndex, hourGlass, count);
            avatarState.UpdateFromRapidCombination(
                (CombinationConsumable5.ResultModel) slotState.Result,
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
            return Math.Max(1, (int) cost);
        }
    }
}
