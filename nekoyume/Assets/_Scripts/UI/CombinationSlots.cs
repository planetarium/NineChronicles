using Bencodex.Types;
using Nekoyume.Game;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Module;

namespace _Scripts.UI
{
    public class CombinationSlots : XTweenWidget
    {
        public CombinationSlot[] slots;

        public override void Show()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var idx = Game.instance.Agent.BlockIndex;
            for (var i = 0; i < avatarState.combinationSlotAddresses.Count; i++)
            {
                var value = Game.instance.Agent.GetState(avatarState.combinationSlotAddresses[i]);
                if (value is null)
                    continue;
                var slotState = new CombinationSlotState((Dictionary)value);
                slots[i].SetData(slotState, idx);
            }
            base.Show();
        }
    }
}
