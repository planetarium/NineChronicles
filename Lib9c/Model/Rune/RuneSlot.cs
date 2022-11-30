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
        public int? RuneId { get; private set; }

        public RuneSlot(
            int index,
            RuneSlotType runeSlotType,
            RuneType runeType,
            bool isLock)
        {
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
            if (serialized.Count > 4)
            {
                RuneId = serialized[4].ToNullableInteger();
            }
        }

        public IValue Serialize()
        {
            var result = List.Empty
                .Add(Index.Serialize())
                .Add(RuneSlotType.Serialize())
                .Add(RuneType.Serialize())
                .Add(IsLock.Serialize());

            if (RuneId.HasValue)
            {
                result = result.Add(RuneId.Serialize());
            }

            return result;
        }

        public void Equip(int runeId)
        {
            RuneId = runeId;
        }

        public void Unequip()
        {
            RuneId = null;
        }

        public void Unlock()
        {
            IsLock = false;
        }
    }
}
