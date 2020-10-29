using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class CombinationSlotBlockIndexModifier : CombinationSlotStateModifier
    {
        private readonly long _blockIndex;

        public override bool IsEmpty => _blockIndex == 0;

        public CombinationSlotBlockIndexModifier(long blockIndex)
        {
            _blockIndex = blockIndex;
        }

        public override CombinationSlotState Modify(CombinationSlotState state)
        {
            state.Update(_blockIndex);
            return state;
        }
    }
}
