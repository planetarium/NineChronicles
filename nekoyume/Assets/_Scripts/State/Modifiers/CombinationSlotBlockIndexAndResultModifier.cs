using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class CombinationSlotBlockIndexAndResultModifier : CombinationSlotStateModifier
    {
        private readonly long _unlockBlockIndex;
        private readonly AttachmentActionResult _result;
        private readonly long _blockIndex;

        public override bool IsEmpty => !(_result is null);

        public CombinationSlotBlockIndexAndResultModifier(
            AttachmentActionResult resultModel,
            long blockIndex,
            long unlockBlockIndex
        )
        {
            _unlockBlockIndex = unlockBlockIndex;
            _result = resultModel;
            _blockIndex = blockIndex;
        }

        public override CombinationSlotState Modify(CombinationSlotState state)
        {
            state.Update(_result, _blockIndex, _unlockBlockIndex);
            return state;
        }
    }
}
