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
        private long _blockIndex;
        private Dictionary<int, CombinationSlotState> _states;

        protected override void Awake()
        {
            base.Awake();
            CombinationSlotStatesSubject.CombinationSlotStates.Subscribe(SetSlots)
                .AddTo(gameObject);
            Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        private void SetSlots(Dictionary<int, CombinationSlotState> states)
        {
            _states = states;
            UpdateSlots();
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateSlots();
        }

        private void UpdateSlots()
        {
            foreach (var pair in _states.Where(pair => !(pair.Value is null)))
            {
                slots[pair.Key].SetData(pair.Value, _blockIndex, pair.Key);
            }
        }
    }
}
