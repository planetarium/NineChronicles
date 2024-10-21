using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using Game;
    using UniRx;

    public class CombinationSlotsPopup : PopupWidget
    {
        [SerializeField]
        private List<CombinationSlot> slots;

        [SerializeField]
        private PetInventory petInventory;
        
        [SerializeField]
        private ConditionalButton rapidCombinationButton;
        
        [SerializeField]
        private TMP_Text openCostText;
        
        private readonly List<IDisposable> _disposablesOnEnable = new();

        protected override void Awake()
        {
            base.Awake();
            
            rapidCombinationButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    var currentBlockIndex = Game.instance.Agent.BlockIndex;
                    Find<CombinationSlotAllPopup>().Show(GetWorkingSlotStateList(), currentBlockIndex);
                })
                .AddTo(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();
            petInventory.Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Game.instance.Agent.BlockIndexSubject
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
        
        public void OnSendRapidCombination(List<int> slotIndex)
        {
            foreach (var index in slotIndex)
            {
                OnSendRapidCombination(index);
            }
            
            UpdateSlots();
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
            var blockIndex = Game.instance.Agent.BlockIndex;
            var slotDict = States.Instance.GetCombinationSlotState(States.Instance.CurrentAvatarState);
            var states = slotDict?.Values.ToList();
            if (states == null)
            {
                slotIndex = -1;
                return false;
            }
            
            for (var i = 0; i < states.Count; i++)
            {
                var stateIdx = states[i].Index;
                if (!states[i].ValidateV2(blockIndex))
                {
                    continue;
                }

                if (slots[i] == null)
                {
                    continue;
                }

                var uiSlotState = slots[stateIdx].UIState;
                if (uiSlotState != CombinationSlot.SlotUIState.Empty)
                {
                    continue;
                }

                slotIndex = stateIdx;
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
            UpdateSlots(Game.instance.Agent.BlockIndex);
        }
        
        private void UpdateSlots(long blockIndex)
        {
            UpdateSlots(blockIndex, null);
            UpdateAllOpenCost(blockIndex);
        }

        public void ClearSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                return;
            }
            
            slots[index].Clear();
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
        
        private void UpdateAllOpenCost(long currentBlockIndex)
        {
            var stateList = GetWorkingSlotStateList();
            var cost = GetWorkingSlotsOpenCost(stateList, currentBlockIndex);
            openCostText.text = cost.ToString();
            rapidCombinationButton.SetState(SetRapidCombinationButtonCondition(cost));
        }

        private ConditionalButton.State SetRapidCombinationButtonCondition(int cost)
        {
            if (cost > 0)
            {
                rapidCombinationButton.Interactable = true;
                return ConditionalButton.State.Normal;
            }
            
            rapidCombinationButton.Interactable = false;
            return ConditionalButton.State.Disabled;
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
        
        private List<CombinationSlotState> GetWorkingSlotStateList()
        {
            return slots
                .Where(s => s.UIState == CombinationSlot.SlotUIState.Working && s.State != null)
                .Select(s => s.State).ToList();
        }

        public static int GetWorkingSlotsOpenCost(List<CombinationSlotState> stateList, long currentBlockIndex)
        {
            var cost = 0;
            foreach (var state in stateList)
            {
                var diff = state.WorkCompleteBlockIndex - currentBlockIndex;
                if (state.PetId.HasValue && States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
                {
                    cost += PetHelper.CalculateDiscountedHourglass(
                        diff,
                        States.Instance.GameConfigState.HourglassPerBlock,
                        petState,
                        TableSheets.Instance.PetOptionSheet);
                }
                else
                {
                    cost += RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
                }
            }

            return cost;
        }
    }
}
