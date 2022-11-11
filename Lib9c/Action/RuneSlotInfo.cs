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
            SlotIndex = ((List)serialized[0]).ToInteger();
            RuneId = ((List)serialized[1]).ToInteger();
            Level = ((List)serialized[2]).ToInteger();
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
