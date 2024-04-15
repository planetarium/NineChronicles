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
        public enum SlotType
        {
            Empty,
            Appraise,
            Working,
            WaitingReceive,
        }

        public enum CacheType
        {
            Appraise,
            WaitingReceive,
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
        private TextMeshProUGUI lockText;

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

        private CombinationSlotState _state;
        private CacheType _cachedType;
        private int _slotIndex;

        private readonly List<IDisposable> _disposablesOfOnEnable = new();
        private readonly Dictionary<Address, bool> cached = new();

        public SlotType Type { get; private set; } = SlotType.Empty;

        public void SetCached(
            Address avatarAddress,
            bool value,
            long requiredBlockIndex,
            SlotType slotType,
            ItemUsable itemUsable = null)
        {
            if (cached.ContainsKey(avatarAddress))
            {
                cached[avatarAddress] = value;
            }
            else
            {
                cached.Add(avatarAddress, value);
            }

            var currentBlockIndex = Game.instance.Agent.BlockIndex;
            switch (slotType)
            {
                case SlotType.Appraise:
                    if (itemUsable == null)
                    {
                        break;
                    }

                    _cachedType = CacheType.Appraise;
                    UpdateItemInformation(itemUsable, slotType);
                    UpdateRequiredBlockInformation(
                        requiredBlockIndex + currentBlockIndex,
                        currentBlockIndex,
                        currentBlockIndex);
                    break;

                case SlotType.WaitingReceive:
                    _cachedType = CacheType.WaitingReceive;
                    UpdateInformation(Type, currentBlockIndex, _state, IsCached(avatarAddress));
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
                OnClickSlot(Type, _state, _slotIndex, Game.instance.Agent.BlockIndex);
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
            ReactiveAvatarState.Inventory
                .Select(_ => Game.instance.Agent.BlockIndex)
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
        }

        private void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
        }

        public void SetSlot(
            Address avatarAddress,
            long currentBlockIndex,
            int slotIndex,
            CombinationSlotState state = null)
        {
            _slotIndex = slotIndex;
            _state = state;
            Type = GetSlotType(state, currentBlockIndex, IsCached(avatarAddress));
            UpdateInformation(Type, currentBlockIndex, state, IsCached(avatarAddress));
        }

        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Type = GetSlotType(_state, currentBlockIndex, IsCached(avatarAddress));
            UpdateInformation(Type, currentBlockIndex, _state, IsCached(avatarAddress));
        }

        private void UpdateInformation(
            SlotType type,
            long currentBlockIndex,
            CombinationSlotState state,
            bool isCached)
        {
            petSelectButton.SetData(state?.PetId ?? null);

            switch (type)
            {
                case SlotType.Empty:
                    SetContainer(false, false, true, false);
                    itemView.Clear();
                    break;

                case SlotType.Appraise:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(true);
                    workingContainer.gameObject.SetActive(false);
                    if (state != null)
                    {
                        UpdateItemInformation(state.Result.itemUsable, type);
                        UpdateHourglass(state, currentBlockIndex);
                        UpdateRequiredBlockInformation(
                            state.UnlockBlockIndex,
                            state.StartBlockIndex,
                            currentBlockIndex);
                    }

                    hasNotificationImage.enabled = false;
                    break;

                case SlotType.Working:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(false);
                    workingContainer.gameObject.SetActive(true);
                    UpdateItemInformation(state.Result.itemUsable, type);
                    UpdateHourglass(state, currentBlockIndex);
                    UpdateRequiredBlockInformation(
                        state.UnlockBlockIndex,
                        state.StartBlockIndex,
                        currentBlockIndex);
                    UpdateNotification(state, currentBlockIndex, isCached);
                    break;

                case SlotType.WaitingReceive:
                    SetContainer(false, false, false, true);
                    if(state != null)
                    {
                        waitingReceiveItemView.SetData(new Item(state.Result.itemUsable));
                        waitingReceiveText.text = string.Format(
                            L10nManager.Localize("UI_SENDING_THROUGH_MAIL"),
                            state.Result.itemUsable.GetLocalizedName(
                                useElementalIcon: false,
                                ignoreLevel: true));
                    }
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

        private SlotType GetSlotType(
            CombinationSlotState state,
            long currentBlockIndex,
            bool isCached)
        {
            if (isCached)
            {
                return _cachedType == CacheType.Appraise
                    ? SlotType.Appraise
                    : SlotType.WaitingReceive;
            }

            if (state?.Result is null)
            {
                return SlotType.Empty;
            }

            return currentBlockIndex < state.StartBlockIndex +
                States.Instance.GameConfigState.RequiredAppraiseBlock
                    ? SlotType.Appraise
                    : SlotType.Working;
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
            if (GetSlotType(state, currentBlockIndex, isCached) != SlotType.Working)
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

        private void UpdateHourglass(CombinationSlotState state, long currentBlockIndex)
        {
            var diff = state.UnlockBlockIndex - currentBlockIndex;
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
            var count = Util.GetHourglassCount(inventory, currentBlockIndex);
            hourglassCountText.text = cost.ToString();
            hourglassCountText.color = count >= cost
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }

        private void UpdateItemInformation(ItemUsable item, SlotType slotType)
        {
            if (slotType == SlotType.Working)
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
                item.GetLocalizedName(useElementalIcon: false, ignoreLevel: true));

        }

        private static void OnClickSlot(
            SlotType type,
            CombinationSlotState state,
            int slotIndex,
            long currentBlockIndex)
        {
            switch (type)
            {
                case SlotType.Empty:
                    if (BattleRenderer.Instance.IsOnBattle)
                    {
                        UI.NotificationSystem.Push(
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

                case SlotType.Working:
                    Widget.Find<CombinationSlotPopup>().Show(state, slotIndex, currentBlockIndex);
                    break;

                case SlotType.Appraise:
                    NotificationSystem.Push(
                        Nekoyume.Model.Mail.MailType.System,
                        L10nManager.Localize("UI_COMBINATION_NOTIFY_IDENTIFYING"),
                        NotificationCell.NotificationType.Information);
                    break;
            }
        }
    }
}
