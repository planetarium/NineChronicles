using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game.Battle;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationSlotsPopup : PopupWidget
    {
        [SerializeField]
        private List<CombinationSlot> slots;

        [SerializeField]
        private PetInventory petInventory;

        private readonly List<IDisposable> _disposablesOnEnable = new();

        public override void Initialize()
        {
            petInventory.Initialize(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateSlots)
                .AddTo(_disposablesOnEnable);
            petInventory.OnSelectedSubject
                .Subscribe(_ =>
                {
                    if (BattleRenderer.Instance.IsOnBattle)
                    {
                        NotificationSystem.Push(
                            Nekoyume.Model.Mail.MailType.System,
                            L10nManager.Localize("UI_BLOCK_EXIT"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    Find<DccCollection>().Show();
                    Close(true);
                })
                .AddTo(_disposablesOnEnable);
            petInventory.Hide();
        }

        protected override void OnDisable()
        {
            _disposablesOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            petInventory.Hide();
        }

        public void SetCaching(
            Address avatarAddress,
            int slotIndex,
            bool value,
            long requiredBlockIndex = 0,
            CombinationSlot.SlotType slotType = CombinationSlot.SlotType.Appraise,
            ItemUsable itemUsable = null)
        {
            slots[slotIndex].SetCached(avatarAddress, value, requiredBlockIndex, slotType, itemUsable);
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

        public void TogglePetPopup(int slotIndex)
        {
            petInventory.Toggle(slotIndex);
        }

        private void UpdateSlots(long blockIndex)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var states = States.Instance.GetCombinationSlotState(avatarState, blockIndex);
            for (var i = 0; i < slots.Count; i++)
            {
                if (states.ContainsKey(i))
                {
                    if (states.TryGetValue(i, out var state))
                    {
                        slots[i].SetSlot(avatarState.address, blockIndex, i, state);
                    }
                    else
                    {
                        slots[i].SetSlot(avatarState.address, blockIndex, i);
                    }
                }
                else
                {
                    slots[i].SetSlot(avatarState.address, blockIndex, i);
                }
            }
        }
    }
}
