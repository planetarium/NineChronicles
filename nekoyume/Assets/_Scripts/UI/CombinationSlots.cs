using System.Globalization;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CombinationSlots : XTweenWidget
    {
        public CombinationSlot[] slots;
        private long _blockIndex;

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();
            CombinationSlotStateSubject.CombinationSlotState.Subscribe(SetSlot).AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            _blockIndex = Game.Game.instance.Agent.BlockIndex;
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateSlotAll();
        }

        private void SetSlot(CombinationSlotState state)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            UpdateSlot(state);
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateSlotAll();
        }

        private void UpdateSlotAll()
        {
            foreach (var state in States.Instance.CombinationSlotStates.Values)
            {
                UpdateSlot(state);
            }
        }

        private void UpdateSlot(CombinationSlotState state)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var address = States.Instance.CurrentAvatarState.address.Derive(
                    string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat,
                        i));
                if (address == state.address)
                {
                    slot.SetData(state, _blockIndex, i);
                    break;
                }
            }
        }
    }
}
