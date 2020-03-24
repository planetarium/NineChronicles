using System.Linq;
using Nekoyume.Game;
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
            var blockIndex = Game.instance.Agent.BlockIndex;
            foreach (var pair in States.Instance.CombinationSlotStates
                .Where(pair => !(pair.Value is null)))
            {
                slots[pair.Key].SetData(pair.Value, blockIndex, pair.Key);
            }
            base.Show();
        }
    }
}
