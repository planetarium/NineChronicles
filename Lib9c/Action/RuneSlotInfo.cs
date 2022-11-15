using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    public class RuneSlotInfo
    {
        public int SlotIndex { get; }
        public int RuneId { get; }
        public int Level { get; }

        public RuneSlotInfo(int slotIndex, int runeId, int level)
        {
            SlotIndex = slotIndex;
            RuneId = runeId;
            Level = level;
        }

        public RuneSlotInfo(List serialized)
        {
            SlotIndex = serialized[0].ToInteger();
            RuneId = serialized[1].ToInteger();
            Level = serialized[2].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(SlotIndex.Serialize())
                .Add(RuneId.Serialize())
                .Add(Level.Serialize());
        }
    }
}
