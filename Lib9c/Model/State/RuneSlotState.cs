using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;

namespace Nekoyume.Model.State
{
    public class RuneSlotState : IState
    {
        public static Address DeriveAddress(Address avatarAddress, BattleType battleType) =>
            avatarAddress.Derive($"rune_slot_state_{battleType}");

        public BattleType BattleType { get; }

        private readonly List<RuneSlot> _slots = new List<RuneSlot>();

        public RuneSlotState(BattleType battleType)
        {
            BattleType = battleType;
            _slots.Add(new RuneSlot(0, RuneSlotType.Default, RuneType.Stat, false));
            _slots.Add(new RuneSlot(1, RuneSlotType.Default, RuneType.Skill, false));
            _slots.Add(new RuneSlot(2, RuneSlotType.Ncg, RuneType.Stat, true));
            _slots.Add(new RuneSlot(3, RuneSlotType.Ncg, RuneType.Skill, true));
            _slots.Add(new RuneSlot(4, RuneSlotType.Stake, RuneType.Stat, true));
            _slots.Add(new RuneSlot(5, RuneSlotType.Stake, RuneType.Skill,true));
        }

        public RuneSlotState(List serialized)
        {
            BattleType = serialized[0].ToEnum<BattleType>();
            _slots = ((List)serialized[1]).Select(x => new RuneSlot((List)x)).ToList();
        }

        public IValue Serialize()
        {
            var result = List.Empty
                .Add(BattleType.Serialize())
                .Add(new List(_slots.Select(x => x.Serialize())));
            return result;
        }

        public void UpdateSlot(int index,
            RuneState runeState,
            RuneType runeType,
            RuneUsePlace runePlace)
        {
            var slot = _slots.FirstOrDefault(x => x.Index == index);
            if (slot is null)
            {
                throw new SlotNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {index}");
            }

            if (slot.IsLock)
            {
                throw new SlotIsLockedException(
                    $"[{nameof(RuneSlotState)}] Index : {index}");
            }

            if (slot.RuneType != runeType)
            {
                throw new SlotRuneTypeException(
                    $"[{nameof(RuneSlotState)}] Index : {index} / {slot.RuneType} != {runeType}");
            }

            if (!BattleType.IsEquippableRune(runePlace))
            {
                throw new IsEquippableRuneException(
                    $"[{nameof(RuneSlotState)}] Index : {index} / runePlace : {runePlace}");
            }

            slot.SetRuneState(runeState);
        }

        public void Unlock(int index)
        {
            var slot = _slots.FirstOrDefault(x => x.Index == index);
            if (slot is null)
            {
                throw new SlotNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {index}");
            }

            if (!slot.IsLock)
            {
                throw new SlotIsAlreadyUnlockedException(
                    $"[{nameof(RuneSlotState)}] Index : {index}");
            }

            slot.Unlock();
        }

        public Dictionary<int, RuneSlot> GetRuneSlot()
        {
            return _slots.ToDictionary(runeSlot => runeSlot.Index);
        }
    }
}
