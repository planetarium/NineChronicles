using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game.Battle;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

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
            base.Initialize();
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
            UpdateSlots();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            petInventory.Hide();
        }
        
        public void OnSendCombinationAction(
            int slotIndex,
            long requiredBlockIndex,
            ItemUsable itemUsable)
        {
            slots[slotIndex].OnSendCombinationAction(requiredBlockIndex, itemUsable);
        }

        public void OnSendRapidCombination(int slotIndex)
        {
            slots[slotIndex].OnSendRapidCombinationAction();
        }
        
        public void OnCraftActionRender(int slotIndex)
        {            
            // Prepare Render 단계에서 갱신된 State를 반영하기 위해 UpdateSlots를 호출합니다. 
            UpdateSlots();
            slots[slotIndex].OnCraftActionRender();
        }

        public bool TryGetEmptyCombinationSlot(out int slotIndex)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var slotDict = States.Instance.GetCombinationSlotState(States.Instance.CurrentAvatarState);
            var states = slotDict?.Values.ToList();
            if (states == null)
            {
                slotIndex = -1;
                return false;
            }
            
            for (var i = 0; i < states.Count; i++)
            {
                if (!states[i].ValidateV2(blockIndex))
                {
                    continue;
                }

                if (slots[i] == null)
                {
                    continue;
                }

                var uiSlotState = slots[i].UIState;
                if (uiSlotState == CombinationSlot.SlotUIState.Appraise ||
                    uiSlotState == CombinationSlot.SlotUIState.WaitingReceive)
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
        
        public void UpdateSlots()
        {
            UpdateSlots(Game.Game.instance.Agent.BlockIndex, null);
        }
        
        public void UpdateSlots(long blockIndex)
        {
            UpdateSlots(blockIndex, null);
        }
        
        public void ClearSlots()
        {
            foreach (var slot in slots)
            {
                slot.Clear();
            }

            UpdateSlots();
        }
        
        public void SetLockLoading(int slotIndex, bool isLoading)
        {
            slots[slotIndex].SetLockLoading(isLoading);
        }

        private void UpdateSlots(long blockIndex, IDictionary<int, CombinationSlotState> states)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            states ??= States.Instance.GetCombinationSlotState(avatarState);
            
            for (var i = 0; i < slots.Count; i++)
            {
                if (states == null)
                {
                    slots[i].SetSlot(blockIndex, i);
                }
                else if (states.ContainsKey(i))
                {
                    if (states.TryGetValue(i, out var state))
                    {
                        slots[i].SetSlot(blockIndex, i, state);
                    }
                    else
                    {
                        slots[i].SetSlot(blockIndex, i);
                    }
                }
                else
                {
                    slots[i].SetSlot(blockIndex, i);
                }
            }
        }
    }
}
