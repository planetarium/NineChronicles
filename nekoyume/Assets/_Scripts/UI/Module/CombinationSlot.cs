using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Game;
    using Scroller;
    using UniRx;

    public class CombinationSlot : MonoBehaviour
    {
        public enum SlotUIState
        {
            Empty,
            Appraise,
            Working,
            WaitingReceive,
            Locked
        }

        public enum CacheType
        {
            Appraise,
            WaitingReceive
        }

        [SerializeField]
        private SimpleItemView itemView;

        [SerializeField]
        private SimpleItemView waitingReceiveItemView;

        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        private Slider progressBar;

        [SerializeField]
        private Image hasNotificationImage;

        [SerializeField]
        private TextMeshProUGUI requiredBlockIndexText;

        [SerializeField]
        private TextMeshProUGUI requiredTimeText;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI hourglassCountText;

        [SerializeField]
        private TextMeshProUGUI preparingText;

        [SerializeField]
        private TextMeshProUGUI waitingReceiveText;

        [SerializeField]
        private GameObject lockContainer;

        [SerializeField]
        private GameObject baseContainer;

        [SerializeField]
        private GameObject noneContainer;

        [SerializeField]
        private GameObject preparingContainer;

        [SerializeField]
        private GameObject workingContainer;

        [SerializeField]
        private GameObject waitReceiveContainer;

        [SerializeField]
        private PetSelectButton petSelectButton;

        private CombinationSlotLockObject _lockObject;
        
        private CombinationSlotState _state;
        private CacheType _cachedType;
        private int _slotIndex;

        private readonly List<IDisposable> _disposablesOfOnEnable = new();
        private readonly Dictionary<Address, bool> cached = new();

        public SlotUIState UIState { get; private set; } = SlotUIState.Locked;

        public void SetCached(
            Address avatarAddress,
            bool value,
            long requiredBlockIndex,
            SlotUIState slotUIState,
            ItemUsable itemUsable = null)
        {
            cached[avatarAddress] = value;

            var currentBlockIndex = Game.instance.Agent.BlockIndex;
            switch (slotUIState)
            {
                case SlotUIState.Appraise:
                    if (itemUsable == null)
                    {
                        break;
                    }

                    _cachedType = CacheType.Appraise;
                    UpdateItemInformation(itemUsable, slotUIState);
                    UpdateRequiredBlockInformation(
                        requiredBlockIndex + currentBlockIndex,
                        currentBlockIndex,
                        currentBlockIndex);
                    break;

                case SlotUIState.WaitingReceive:
                    _cachedType = CacheType.WaitingReceive;
                    UpdateInformation(UIState, currentBlockIndex, _state, IsCached(avatarAddress));
                    break;
            }
        }

        private bool IsCached(Address avatarAddress)
        {
            return cached.ContainsKey(avatarAddress) && cached[avatarAddress];
        }

        private void Awake()
        {
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClickSlot(UIState, _state, _slotIndex, Game.instance.Agent.BlockIndex);
            }).AddTo(gameObject);
            
            _lockObject = lockContainer.GetComponent<CombinationSlotLockObject>();
        }

        private void OnEnable()
        {
            Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(OnUpdateBlock)
                .AddTo(_disposablesOfOnEnable);
            ReactiveAvatarState.Inventory
                .Select(_ => Game.instance.Agent.BlockIndex)
                .Subscribe(OnUpdateBlock)
                .AddTo(_disposablesOfOnEnable);
        }

        private void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
        }

        private void SetLockObject()
        {
            if (_lockObject == null)
            {
                NcDebug.LogError("Can't find CombinationSlotLockObject component.");
                return;
            }

            if (TableSheets.Instance.UnlockCombinationSlotCostSheet.TryGetValue(_slotIndex, out var data))
            {
                _lockObject.SetData(data);
            }
        }

        public void SetSlot(
            Address avatarAddress,
            long currentBlockIndex,
            int slotIndex,
            CombinationSlotState state = null)
        {
            _slotIndex = slotIndex;
            _state = state;
            UIState = GetSlotType(state, currentBlockIndex, IsCached(avatarAddress));
            UpdateInformation(UIState, currentBlockIndex, state, IsCached(avatarAddress));
            SetLockObject();
        }

#region OnBlockRender
        private void OnUpdateBlock(long currentBlockIndex)
        {
            switch (UIState)
            {
                case SlotUIState.Empty:
                    OnBlockRenderEmpty(currentBlockIndex);
                    break;
                case SlotUIState.Appraise:
                    OnBlockRenderAppraise(currentBlockIndex);
                    break;
                case SlotUIState.Working:
                    OnBlockRenderWorking(currentBlockIndex);
                    break;
                case SlotUIState.WaitingReceive:
                    OnBlockRenderWaitingReceive(currentBlockIndex);
                    break;
                case SlotUIState.Locked:
                    OnBlockRenderLocked(currentBlockIndex);
                    break;
            }
        }
        
        private void OnBlockRenderEmpty(long currentBlockIndex)
        {
            // Do nothing.
        }
        
        private void OnBlockRenderAppraise(long currentBlockIndex)
        {
            var workingBlockIndex = _state.StartBlockIndex + States.Instance.GameConfigState.RequiredAppraiseBlock;
            if (currentBlockIndex <= workingBlockIndex)
            {
                return;
            }

            UIState = SlotUIState.Working;
            UpdateInformation(currentBlockIndex);
        }
        
        private void OnBlockRenderWorking(long currentBlockIndex)
        { 
            if (_state.Result == null || !_state.ValidateV2(currentBlockIndex))
            {
                return;
            }
            // 제작 완료(BlockIndex체크)나 제작 아이템 정보가 없는 경우 Empty로 변경
            UIState = SlotUIState.Empty;
            UpdateInformation(currentBlockIndex);
        }
        
        private void OnBlockRenderWaitingReceive(long currentBlockIndex)
        {
            // Not Cached && slot null
            if (_state.Result != null)
            {
                return;
            }

            UIState = SlotUIState.Empty;
            UpdateInformation(currentBlockIndex);
        }
        
        private void OnBlockRenderLocked(long currentBlockIndex)
        {
            // Do nothing.
        }
#endregion OnBlockRender

        private void UpdateInformation(long blockIndex)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            UpdateInformation(UIState, blockIndex, _state, IsCached(avatarAddress));
        }
        

        private void UpdateInformation(
            SlotUIState uiState,
            long currentBlockIndex,
            CombinationSlotState state,
            bool isCached)
        {
            petSelectButton.SetData(state?.PetId ?? null);

            switch (uiState)
            {
                case SlotUIState.Empty:
                    SetContainer(false, false, true, false);
                    itemView.Clear();
                    break;

                case SlotUIState.Appraise:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(true);
                    workingContainer.gameObject.SetActive(false);
                    if (state != null)
                    {
                        UpdateItemInformation(state.Result.itemUsable, uiState);
                        UpdateHourglass(state, currentBlockIndex);
                        UpdateRequiredBlockInformation(
                            state.UnlockBlockIndex,
                            state.StartBlockIndex,
                            currentBlockIndex);
                    }
                    hasNotificationImage.enabled = false;
                    break;

                case SlotUIState.Working:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(false);
                    workingContainer.gameObject.SetActive(true);
                    if (state != null)
                    {
                        UpdateItemInformation(state.Result.itemUsable, uiState);
                        UpdateHourglass(state, currentBlockIndex);
                        UpdateRequiredBlockInformation(
                            state.UnlockBlockIndex,
                            state.StartBlockIndex,
                            currentBlockIndex);
                        UpdateNotification(state, currentBlockIndex, isCached);
                    }
                    break;

                case SlotUIState.WaitingReceive:
                    SetContainer(false, false, false, true);
                    if (state != null)
                    {
                        waitingReceiveItemView.SetData(new Item(state.Result.itemUsable));
                        waitingReceiveText.text = string.Format(
                            L10nManager.Localize("UI_SENDING_THROUGH_MAIL"),
                            state.Result.itemUsable.GetLocalizedName(
                                false,
                                true));
                    }
                    break;
                
                case SlotUIState.Locked:
                    SetContainer(true, false, false, false);
                    itemView.Clear();
                    break;
            }
        }

        private void SetContainer(
            bool isLock,
            bool isWorking,
            bool isEmpty,
            bool isWaitingReceive)
        {
            lockContainer.gameObject.SetActive(isLock);
            baseContainer.gameObject.SetActive(isWorking);
            noneContainer.gameObject.SetActive(isEmpty);
            waitReceiveContainer.gameObject.SetActive(isWaitingReceive);
        }

        private SlotUIState GetSlotType(
            CombinationSlotState state,
            long currentBlockIndex,
            bool isCached)
        {
            if (isCached)
            {
                return _cachedType == CacheType.Appraise
                    ? SlotUIState.Appraise
                    : SlotUIState.WaitingReceive;
            }

            if (state == null)
            {
                return SlotUIState.Empty;
            }

            if (state is not { IsUnlocked: true })
            {
                return SlotUIState.Locked;
            }

            if (state.Result is null)
            {
                return SlotUIState.Empty;
            }

            return currentBlockIndex < state.StartBlockIndex +
                States.Instance.GameConfigState.RequiredAppraiseBlock
                    ? SlotUIState.Appraise
                    : SlotUIState.Working;
        }

        private void UpdateRequiredBlockInformation(
            long unlockBlockIndex,
            long startBlockIndex,
            long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(unlockBlockIndex - startBlockIndex, 1);
            var diff = Math.Max(unlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}";
            requiredTimeText.text = $"({diff.BlockRangeToTimeSpanString(true)})";
        }

        private void UpdateNotification(
            CombinationSlotState state,
            long currentBlockIndex,
            bool isCached)
        {
            if (GetSlotType(state, currentBlockIndex, isCached) != SlotUIState.Working)
            {
                hasNotificationImage.enabled = false;
                return;
            }

            var gameConfigState = Game.instance.States.GameConfigState;
            var diff = state.RequiredBlockIndex - currentBlockIndex;
            int cost;
            if (state.PetId.HasValue &&
                States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
            {
                cost = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    States.Instance.GameConfigState.HourglassPerBlock,
                    petState,
                    TableSheets.Instance.PetOptionSheet);
            }
            else
            {
                cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            }

            var row = Game.instance.TableSheets.MaterialItemSheet
                .OrderedList
                .First(r => r.ItemSubType == ItemSubType.Hourglass);
            var isEnough = States.Instance.CurrentAvatarState.inventory
                .HasFungibleItem(row.ItemId, currentBlockIndex, cost);
            hasNotificationImage.enabled = isEnough;
        }

        private void UpdateHourglass(CombinationSlotState state, long blockIndex)
        {
            var diff = state.UnlockBlockIndex - blockIndex;
            int cost;
            if (state.PetId.HasValue &&
                States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
            {
                cost = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    States.Instance.GameConfigState.HourglassPerBlock,
                    petState,
                    TableSheets.Instance.PetOptionSheet);
            }
            else
            {
                cost = RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            }

            var inventory = States.Instance.CurrentAvatarState.inventory;
            var count = inventory.GetUsableItemCount(CostType.Hourglass, blockIndex);
            hourglassCountText.text = cost.ToString();
            hourglassCountText.color = count >= cost
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }

        private void UpdateItemInformation(ItemUsable item, SlotUIState slotUIState)
        {
            if (slotUIState == SlotUIState.Working)
            {
                itemView.SetData(new Item(item));
            }
            else
            {
                itemView.SetDataExceptOptionTag(new Item(item));
            }

            itemNameText.text = TextHelper.GetItemNameInCombinationSlot(item);
            preparingText.text = string.Format(
                L10nManager.Localize("UI_COMBINATION_SLOT_IDENTIFYING"),
                item.GetLocalizedName(false, true));
        }

        private static void OnClickSlot(
            SlotUIState uiState,
            CombinationSlotState state,
            int slotIndex,
            long currentBlockIndex)
        {
            switch (uiState)
            {
                case SlotUIState.Empty:
                    if (BattleRenderer.Instance.IsOnBattle)
                    {
                        NotificationSystem.Push(
                            Nekoyume.Model.Mail.MailType.System,
                            L10nManager.Localize("UI_BLOCK_EXIT"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    var widgetLayerRoot = MainCanvas.instance
                        .GetLayerRootTransform(WidgetType.Widget);
                    var statusWidget = Widget.Find<Status>();
                    foreach (var widget in MainCanvas.instance.Widgets
                        .Where(widget =>
                            widget.isActiveAndEnabled
                            && widget.transform.parent.Equals(widgetLayerRoot)
                            && !widget.Equals(statusWidget)))
                    {
                        widget.Close(true);
                    }

                    Widget.Find<Craft>()?.gameObject.SetActive(false);
                    Widget.Find<Enhancement>()?.gameObject.SetActive(false);
                    Widget.Find<HeaderMenuStatic>()
                        .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                    Widget.Find<CombinationMain>().Show();
                    Widget.Find<CombinationSlotsPopup>().Close();
                    break;

                case SlotUIState.Working:
                    Widget.Find<CombinationSlotPopup>().Show(state, slotIndex, currentBlockIndex);
                    break;

                case SlotUIState.Appraise:
                    NotificationSystem.Push(
                        Nekoyume.Model.Mail.MailType.System,
                        L10nManager.Localize("UI_COMBINATION_NOTIFY_IDENTIFYING"),
                        NotificationCell.NotificationType.Information);
                    break;
            }
        }
    }
}
