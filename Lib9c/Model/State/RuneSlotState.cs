using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.TableData;

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
            _slots.Add(new RuneSlot(1, RuneSlotType.Ncg, RuneType.Stat, true));
            _slots.Add(new RuneSlot(2, RuneSlotType.Stake, RuneType.Stat, true));
            _slots.Add(new RuneSlot(3, RuneSlotType.Default, RuneType.Skill, false));
            _slots.Add(new RuneSlot(4, RuneSlotType.Ncg, RuneType.Skill, true));
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

        public void UpdateSlot(List<RuneSlotInfo> runeInfos, RuneListSheet runeListSheet)
        {
            if (runeInfos is null)
            {
                throw new RuneInfosIsEmptyException($"[{nameof(RuneSlotState)}]");
            }

            if (runeInfos.GroupBy(x => x.SlotIndex).Count() != runeInfos.Count)
            {
                throw new DuplicatedRuneSlotIndexException($"[{nameof(RuneSlotState)}]");
            }

            foreach (var runeSlotInfo in runeInfos)
            {
                if (_slots.All(x => x.Index != runeSlotInfo.SlotIndex))
                {
                    throw new SlotNotFoundException($"[{nameof(RuneSlotState)}]");
                }
            }

            foreach (var slot in _slots)
            {
                slot.Unequip();
            }

            foreach (var slot in _slots)
            {
                if (slot.IsLock)
                {
                    continue;
                }

                var runeInfo = runeInfos.FirstOrDefault(x => x.SlotIndex == slot.Index);
                if (runeInfo == null)
                {
                    continue;
                }

                if (IsUsableSlot(runeListSheet, slot, runeInfo))
                {
                    slot.Equip(runeInfo.RuneId);
                }
            }

            var emptySlotCount = _slots.Count(x => !x.RuneId.HasValue);
            var slotCount = _slots.Where(x => x.RuneId.HasValue).GroupBy(x => x.RuneId).Count();
            if(emptySlotCount + slotCount != _slots.Count)
            {
                throw new DuplicatedRuneIdException($"[{nameof(RuneSlotState)}]");
            }
        }

        [Obsolete("UpdateSlotV2")]
        public void UpdateSlotV2(List<RuneSlotInfo> runeInfos, RuneListSheet runeListSheet)
        {
            if (runeInfos is null)
            {
                throw new RuneInfosIsEmptyException($"[{nameof(RuneSlotState)}]");
            }

            if (runeInfos.GroupBy(x => x.SlotIndex).Count() != runeInfos.Count)
            {
                throw new DuplicatedRuneSlotIndexException($"[{nameof(RuneSlotState)}]");
            }

            foreach (var runeSlotInfo in runeInfos)
            {
                if (_slots.All(x => x.Index != runeSlotInfo.SlotIndex))
                {
                    throw new SlotNotFoundException($"[{nameof(RuneSlotState)}]");
                }
            }

            foreach (var slot in _slots)
            {
                var runeInfo = runeInfos.FirstOrDefault(x => x.SlotIndex == slot.Index);
                if (runeInfo is null)
                {
                    slot.Unequip();
                }
                else
                {
                    if (IsUsableSlotV2(runeListSheet, slot, runeInfo))
                    {
                        slot.Equip(runeInfo.RuneId);
                    }
                    else
                    {
                        throw new IsUsableSlotException(
                            $"[{nameof(RuneSlotState)}] Index : {slot.Index}");
                    }
                }
            }
        }

        private bool IsUsableSlot(RuneListSheet runeListSheet, RuneSlot slot, RuneSlotInfo runeInfo)
        {
            if (slot.IsLock)
            {
                throw new SlotIsLockedException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index}");
            }

            var runeId = runeInfo.RuneId;
            if (!runeListSheet.TryGetValue(runeId, out var row))
            {
                throw new RuneListNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runeId : {runeId}");
            }

            var runeType = (RuneType)row.RuneType;
            if (slot.RuneType != runeType)
            {
                throw new SlotRuneTypeException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / {slot.RuneType} != {runeType}");
            }

            var runePlace = (RuneUsePlace)row.UsePlace;
            if (!BattleType.IsEquippableRune(runePlace))
            {
                throw new IsEquippableRuneException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runePlace : {runePlace}");
            }

            return true;
        }

        [Obsolete("IsUsableSlot")]
        private bool IsUsableSlotV2(RuneListSheet runeListSheet, RuneSlot slot, RuneSlotInfo runeInfo)
        {
            if (slot.IsLock)
            {
                throw new SlotIsLockedException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index}");
            }

            var runeId = runeInfo.RuneId;
            if (!runeListSheet.TryGetValue(runeId, out var row))
            {
                throw new RuneListNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runeId : {runeId}");
            }

            var runeType = (RuneType)row.RuneType;
            if (slot.RuneType != runeType)
            {
                throw new SlotRuneTypeException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / {slot.RuneType} != {runeType}");
            }

            var runePlace = (RuneUsePlace)row.UsePlace;
            if (!BattleType.IsEquippableRune(runePlace))
            {
                throw new IsEquippableRuneException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runePlace : {runePlace}");
            }

            if (_slots.Where(x => x.Index != slot.Index).Any(x => x.RuneId == runeId))
            {
                throw new DuplicatedRuneIdException(
                    $"[{nameof(RuneSlotState)}] RuneId : {runeId}");
            }

            return true;
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

        public List<RuneSlot> GetRuneSlot()
        {
            return _slots;
        }

        public List<RuneSlotInfo> GetEquippedRuneSlotInfos()
        {
            var result = new List<RuneSlotInfo>();
            foreach (var slot in _slots.Where(slot => !slot.IsLock))
            {
                if (slot.RuneId.HasValue)
                {
                    result.Add(new RuneSlotInfo(slot.Index, slot.RuneId.Value));
                }
            }

            return result;
        }
    }
}
