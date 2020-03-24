using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class CombinationSlotStateModifier : IStateModifier<CombinationSlotState>
    {
        private readonly long _unlockBlockIndex;
        private readonly AttachmentActionResult _result;
        public bool IsEmpty => !(_result is null);

        public CombinationSlotStateModifier(CombinationConsumable.ResultModel resultModel)
        {
            _unlockBlockIndex = resultModel.itemUsable.RequiredBlockIndex;
            _result = resultModel;
        }
        public CombinationSlotState Modify(CombinationSlotState state)
        {
            state.Update(_result, _unlockBlockIndex);
            return state;
        }
    }
}
