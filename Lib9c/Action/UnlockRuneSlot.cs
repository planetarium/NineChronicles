using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
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
    [ActionType("unlock_rune_slot")]
    public class UnlockRuneSlot : GameAction, IUnlockRuneSlotV1
    {
        public Address AvatarAddress;
        public int SlotIndex;

        Address IUnlockRuneSlotV1.AvatarAddress => AvatarAddress;
        int IUnlockRuneSlotV1.SlotIndex => SlotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["s"] = SlotIndex.Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            SlotIndex = plainValue["s"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });

            var adventureSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var adventureSlotState = states.TryGetState(adventureSlotStateAddress, out List rawAdventureSlotState)
                ? new RuneSlotState(rawAdventureSlotState)
                : new RuneSlotState(BattleType.Adventure);


            var arenaSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Arena);
            var arenaSlotState = states.TryGetState(arenaSlotStateAddress, out List rawArenaSlotState)
                ? new RuneSlotState(rawArenaSlotState)
                : new RuneSlotState(BattleType.Arena);

            var raidSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Raid);
            var raidSlotState = states.TryGetState(raidSlotStateAddress, out List rawRaidSlotState)
                ? new RuneSlotState(rawRaidSlotState)
                : new RuneSlotState(BattleType.Raid);


            var slot = adventureSlotState.GetRuneSlot().FirstOrDefault(x => x.Index == SlotIndex);
            if (slot == null)
            {
                throw new SlotNotFoundException(
                    $"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }

            // note : You will need to modify it later when applying staking unlock.
            if (slot.RuneSlotType != RuneSlotType.Ncg)
            {
                throw new MismatchRuneSlotTypeException(
                    $"[{nameof(UnlockRuneSlot)}] RuneSlotType : {slot.RuneSlotType}");
            }

            var gameConfigState = states.GetGameConfigState();
            var cost = slot.RuneType == RuneType.Stat
                ? gameConfigState.RuneStatSlotUnlockCost
                : gameConfigState.RuneSkillSlotUnlockCost;
            var ncgCurrency = states.GetGoldCurrency();
            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);

            adventureSlotState.Unlock(SlotIndex);
            arenaSlotState.Unlock(SlotIndex);
            raidSlotState.Unlock(SlotIndex);

            return states
                .TransferAsset(context.Signer, feeStoreAddress, cost * ncgCurrency)
                .SetState(adventureSlotStateAddress, adventureSlotState.Serialize())
                .SetState(arenaSlotStateAddress, arenaSlotState.Serialize())
                .SetState(raidSlotStateAddress, raidSlotState.Serialize());
        }
    }
}
