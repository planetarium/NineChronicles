using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UniRx;

namespace _Scripts.UI
{
    public class CombinationSlots : XTweenWidget
    {
        public CombinationSlot[] slots;

        protected override void Awake()
        {
            base.Awake();
            CombinationSlotStatesSubject.CombinationSlotStates.Subscribe(SetSlots)
                .AddTo(gameObject);
        }

        private void SetSlots(Dictionary<int, CombinationSlotState> states)
        {
            var blockIndex = Game.instance.Agent.BlockIndex;
            foreach (var pair in states.Where(pair => !(pair.Value is null)))
            {
                slots[pair.Key].SetData(pair.Value, blockIndex, pair.Key);
            }
        }
    }
}
