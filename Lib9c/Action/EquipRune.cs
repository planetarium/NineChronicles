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

            if (RuneInfos is null)
            {
                throw new RuneInfosIsEmptyException(
                    $"[{nameof(EquipRune)}] my avatar address : {AvatarAddress}");
            }

            if (RuneInfos.GroupBy(x => x.SlotIndex).Count() != RuneInfos.Count)
            {
                throw new DuplicatedRuneSlotIndexException(
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

            if (RuneInfos.Exists(x => x.SlotIndex >= runeSlotState.GetRuneSlot().Count))
            {
                throw new SlotNotFoundException(
                    $"[{nameof(EquipRune)}] my avatar address : {AvatarAddress}");
            }

            var runeStates = new List<RuneState>();
            foreach (var address in RuneInfos.Select(info => RuneState.DeriveAddress(AvatarAddress, info.RuneId)))
            {
                if (states.TryGetState(address, out List rawRuneState))
                {
                    runeStates.Add(new RuneState(rawRuneState));
                }
            }

            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeStates, runeListSheet);
            return states.SetState(runeSlotStateAddress, runeSlotState.Serialize());
        }
    }
}
