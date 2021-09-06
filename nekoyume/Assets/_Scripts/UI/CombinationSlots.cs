using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using RedBlueGames.Tools.TextTyper;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationSlots : XTweenWidget
    {
        [SerializeField]
        private List<CombinationSlot> slots;

        [SerializeField]
        private Blur blur;

        private readonly List<IDisposable> _disposablesOfOnEnable = new List<IDisposable>();

        public override WidgetType WidgetType => WidgetType.Popup;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;

        protected override void OnEnable()
        {
            base.OnEnable();
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(_disposablesOfOnEnable);
        }

        protected override void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (blur)
            {
                blur.Show();
            }

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

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur && blur.isActiveAndEnabled)
            {
                blur.Close();
                AudioController.PlayClick();
            }

            base.Close(ignoreCloseAnimation);
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
