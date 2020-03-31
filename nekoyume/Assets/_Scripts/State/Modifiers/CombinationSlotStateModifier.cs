using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class CombinationSlotStateModifier : IStateModifier<CombinationSlotState>
    {
        private readonly long _unlockBlockIndex;
        private readonly AttachmentActionResult _result;
        private readonly long _blockIndex;
        public bool IsEmpty => !(_result is null);

        public CombinationSlotStateModifier(
            AttachmentActionResult resultModel,
            long blockIndex,
            long unlockBlockIndex
        )
        {
            _unlockBlockIndex = unlockBlockIndex;
            _result = resultModel;
            _blockIndex = blockIndex;
        }
        public CombinationSlotState Modify(CombinationSlotState state)
        {
            state.Update(_result, _blockIndex, _unlockBlockIndex);
            return state;
        }
    }
}
