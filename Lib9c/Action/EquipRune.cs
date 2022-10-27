using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("equip_rune")]
    public class EquipRune : GameAction
    {
        public Address AvatarAddress;
        public BattleType BattleType;
        public List<RuneSlotInfo> RuneInfos;
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["b"] = BattleType.Serialize(),
                    ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            BattleType = plainValue["b"].ToEnum<BattleType>();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (RuneInfos is null || !RuneInfos.Any())
            {
                throw new RuneInfosIsEmptyException(
                    $"[{nameof(EquipRune)}] my avatar address : {AvatarAddress}");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(RuneListSheet),
                });

            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType);
            var runeSlotState = states.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType);
            foreach (var info in RuneInfos)
            {
                var runeListSheet = sheets.GetSheet<RuneListSheet>();
                if (!runeListSheet.TryGetValue(info.RuneId, out var row))
                {
                    throw new RuneListNotFoundException(
                        $"[{nameof(EquipRune)}] my avatar address : {AvatarAddress}");
                }

                var runeStateAddress = RuneState.DeriveAddress(AvatarAddress, info.RuneId);
                if (states.TryGetState(runeStateAddress, out List rawRuneState))
                {
                    var runeState = new RuneState(rawRuneState);
                    var runeType = (RuneType)row.RuneType;
                    var runeUsePlace = (RuneUsePlace)row.UsePlace;
                    runeSlotState.UpdateSlot(info.SlotIndex, runeState, runeType, runeUsePlace);
                }
                else
                {
                    throw new RuneStateNotFoundException(
                        $"[{nameof(EquipRune)}] my avatar address : {AvatarAddress}");
                }
            }

            return states.SetState(runeSlotStateAddress, runeSlotState.Serialize());
        }
    }
}
