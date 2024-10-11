using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class CombinationSlotBlockIndexAndResultModifier : CombinationSlotStateModifier
    {
        private readonly long _workCompleteBlockIndex;
        private readonly AttachmentActionResult _result;
        private readonly long _blockIndex;

        public override bool IsEmpty => !(_result is null);

        public CombinationSlotBlockIndexAndResultModifier(
            AttachmentActionResult resultModel,
            long blockIndex,
            long workCompleteBlockIndex
        )
        {
            _workCompleteBlockIndex = workCompleteBlockIndex;
            _result = resultModel;
            _blockIndex = blockIndex;
        }

        public override CombinationSlotState Modify(CombinationSlotState state)
        {
            state.Update(_result, _blockIndex, _workCompleteBlockIndex);
            return state;
        }
    }
}
