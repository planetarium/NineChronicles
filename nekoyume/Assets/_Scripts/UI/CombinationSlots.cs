using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationSlots : XTweenWidget
    {
        [SerializeField] private List<CombinationSlot> slots;

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
            HelpPopup.HelpMe(100008, true);
        }

        public void SetCaching(int slotIndex, bool value, long requiredBlockIndex = 0, ItemUsable itemUsable = null)
        {
            slots[slotIndex].SetCached(value, requiredBlockIndex, itemUsable);
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
        }

        public bool TryGetEmptyCombinationSlot(out int slotIndex)
        {
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Type != CombinationSlot.SlotType.Empty)
                {
                    continue;
                }

                slotIndex = i;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            UpdateSlots(blockIndex);
        }

        private void UpdateSlots(long blockIndex)
        {
            var states = States.Instance.GetCombinationSlotState(blockIndex);

            for (var i = 0; i < slots.Count; i++)
            {
                if (states != null && states.TryGetValue(i, out var state))
                {
                    slots[i].SetSlot(blockIndex, i, state);
                }
                else
                {
                    slots[i].SetSlot(blockIndex, i );
                }
            }
        }
    }
}
