using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class CombinationSlotBlockIndexModifier: IStateModifier<CombinationSlotState>
    {
        private readonly long _blockIndex;
        public bool dirty { get; set; }
        public bool IsEmpty => _blockIndex == 0;

        public CombinationSlotBlockIndexModifier(long blockIndex)
        {
            _blockIndex = blockIndex;
        }
        public CombinationSlotState Modify(CombinationSlotState state)
        {
            state.Update(_blockIndex);
            return state;
        }
    }
}
