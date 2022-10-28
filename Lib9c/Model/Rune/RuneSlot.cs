using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Rune
{
    public class RuneSlot : IState
    {
        public int Index { get; }
        public RuneSlotType RuneSlotType { get; }
        public RuneType RuneType { get; }
        public bool IsLock { get; private set; }

        private readonly List<RuneState> _runeStates;

        public RuneSlot(
            int index,
            RuneSlotType runeSlotType,
            RuneType runeType,
            bool isLock)
        {
            _runeStates = new List<RuneState>();
            Index = index;
            RuneSlotType = runeSlotType;
            RuneType = runeType;
            IsLock = isLock;
        }

        public RuneSlot(List serialized)
        {
            Index = serialized[0].ToInteger();
            RuneSlotType = serialized[1].ToEnum<RuneSlotType>();
            RuneType = serialized[2].ToEnum<RuneType>();
            IsLock = serialized[3].ToBoolean();
            _runeStates = ((List)serialized[4]).Select(x => new RuneState((List)x)).ToList();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Index.Serialize())
                .Add(RuneSlotType.Serialize())
                .Add(RuneType.Serialize())
                .Add(IsLock.Serialize())
                .Add(new List(_runeStates.Select(x => x.Serialize())));
        }

        public void Equip(RuneState runeState)
        {
            _runeStates.Clear();
            _runeStates.Add(runeState);
        }

        public void Unequip()
        {
            _runeStates.Clear();
        }

        public bool Equipped(out RuneState runeState)
        {
            runeState = _runeStates.Any() ? _runeStates.First() : null;
            return _runeStates.Any();
        }

        public void Unlock()
        {
            IsLock = false;
        }
    }
}
