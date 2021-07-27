using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
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
    using UniRx;

    public class CombinationSlot : MonoBehaviour
    {
        private enum SlotState
        {
            Lock,
            Empty,
            Working,
        }

        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>();

        [SerializeField] private SimpleItemView itemView;
        [SerializeField] private TouchHandler touchHandler;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image hasNotificationImage;
        [SerializeField] private TextMeshProUGUI lockText;
        [SerializeField] private TextMeshProUGUI requiredBlockIndexText;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI hourglassCountText;
        [SerializeField] private GameObject lockContainer;
        [SerializeField] private GameObject baseContainer;
        [SerializeField] private GameObject noneContainer;
        [SerializeField] private GameObject preparingContainer;
        [SerializeField] private GameObject workingContainer;
        [SerializeField] private RandomNumberRoulette randomNumberRoulette;

        private CombinationSlotState _combinationSlotState;

        private long _currentBlockIndex;
        private int _slotIndex;

        private void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex).AddTo(gameObject);

            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                SelectSlot();
            }).AddTo(gameObject);

            itemView.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                SelectSlot();
            }).AddTo(gameObject);

            HasNotification.SubscribeTo(hasNotificationImage).AddTo(gameObject);
        }

        private SlotState GetSlotState(CombinationSlotState state, long currentBlockIndex)
        {
            var isLock =
                !States.Instance.CurrentAvatarState?.worldInformation.IsStageCleared(
                    state.UnlockStage) ?? true;
            if (isLock)
            {
                return SlotState.Lock;
            }

            if (state.Result is null)
            {
                return SlotState.Empty;
            }

            var isValid = state.Validate(States.Instance.CurrentAvatarState, currentBlockIndex);
            if (!isValid)
            {
                return SlotState.Working;
            }

            var itemRequiredBlockIndex = state.Result.itemUsable.RequiredBlockIndex;
            return itemRequiredBlockIndex <= currentBlockIndex
                ? SlotState.Empty
                : SlotState.Working;
        }

        public void SetData(CombinationSlotState state, long currentBlockIndex, int slotIndex)
        {
            _combinationSlotState = state;
            _slotIndex = slotIndex;

            switch (GetSlotState(state, currentBlockIndex))
            {
                case SlotState.Lock:
                    lockContainer.gameObject.SetActive(true);
                    baseContainer.gameObject.SetActive(false);
                    noneContainer.gameObject.SetActive(false);
                    var text = L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE");
                    lockText.text = string.Format(text, state.UnlockStage);
                    break;
                case SlotState.Empty:
                    lockContainer.gameObject.SetActive(false);
                    baseContainer.gameObject.SetActive(false);
                    noneContainer.gameObject.SetActive(true);
                    itemView.Clear();
                    break;
                case SlotState.Working:
                    lockContainer.gameObject.SetActive(false);
                    baseContainer.gameObject.SetActive(true);
                    noneContainer.gameObject.SetActive(false);
                    itemView.SetData(new Item(state.Result.itemUsable));
                    itemNameText.text = GetItemName(state.Result.itemUsable);
                    SubscribeOnBlockIndex(currentBlockIndex);
                    Widget.Find<HeaderMenu>()?.UpdateCombinationNotification();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            _currentBlockIndex = currentBlockIndex;
            UpdateHasNotification(_currentBlockIndex);
            UpdateSlotInformation(currentBlockIndex);
        }

        private void UpdateHasNotification(long currentBlockIndex)
        {
            if (_combinationSlotState?.Result is null)
            {
                HasNotification.Value = false;
                return;
            }

            switch (_combinationSlotState.Result)
            {
                case CombinationConsumable5.ResultModel ccResult:
                    if (ccResult.id == default)
                    {
                        HasNotification.Value = false;
                        return;
                    }

                    break;
                default:
                    HasNotification.Value = false;
                    return;
            }

            var diff = _combinationSlotState.Result.itemUsable.RequiredBlockIndex -
                       currentBlockIndex;
            if (diff <= 0)
            {
                HasNotification.Value = false;
                return;
            }

            var gameConfigState = Game.Game.instance.States.GameConfigState;
            var cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);

            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            var isEnough =
                States.Instance.CurrentAvatarState.inventory.HasFungibleItem(row.ItemId, cost);

            HasNotification.Value = isEnough;
        }

        private void UpdateSlotInformation(long currentBlockIndex)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_combinationSlotState?.Result is null)
            {
                return;
            }

            UpdateRequiredBlockInformation(_currentBlockIndex);

            if (_combinationSlotState.StartBlockIndex >= currentBlockIndex) // prepare
            {
                preparingContainer.gameObject.SetActive(true);
                workingContainer.gameObject.SetActive(false);
                randomNumberRoulette.Stop();
            }
            else // working
            {
                preparingContainer.gameObject.SetActive(false);
                workingContainer.gameObject.SetActive(true);
                randomNumberRoulette.Play();
                UpdateHourglass(_currentBlockIndex);
            }
        }

        private void UpdateRequiredBlockInformation(long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(_combinationSlotState.RequiredBlockIndex, 1);
            var diff = Math.Max(_combinationSlotState.UnlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}.";
        }

        private void UpdateHourglass(long currentBlockIndex)
        {
            var diff = _combinationSlotState.UnlockBlockIndex - currentBlockIndex;
            var cost =
                RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            var count = GetHourglassCount();
            hourglassCountText.text = cost.ToString();
            hourglassCountText.color = count >= cost
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }

        private int GetHourglassCount()
        {
            var count = 0;
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var materials =
                inventory.Items.OrderByDescending(x => x.item.ItemType == ItemType.Material);
            var hourglass = materials.Where(x => x.item.ItemSubType == ItemSubType.Hourglass);
            foreach (var item in hourglass)
            {
                if (item.item is TradableMaterial tradableItem)
                {
                    var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                    if (tradableItem.RequiredBlockIndex > blockIndex)
                    {
                        continue;
                    }
                }

                count += item.count;
            }

            return count;
        }

        private string GetItemName(ItemUsable itemUsable)
        {
            var itemName = itemUsable.GetLocalizedNonColoredName();
            switch (itemUsable)
            {
                case Equipment equipment:
                    if (equipment.level > 0)
                    {
                        return string.Format(L10nManager.Localize("UI_COMBINATION_SLOT_UPGRADE"),
                            itemName,
                            equipment.level);
                    }
                    else
                    {
                        return string.Format(L10nManager.Localize("UI_COMBINATION_SLOT_CRAFT"),
                            itemName);
                    }
                default:
                    return string.Format(L10nManager.Localize("UI_COMBINATION_SLOT_CRAFT"),
                        itemName);
            }
        }

        private void SelectSlot()
        {
            if (_combinationSlotState.Validate(States.Instance.CurrentAvatarState,
                Game.Game.instance.Agent.BlockIndex))
            {
                if (Game.Game.instance.Stage.IsInStage)
                {
                    return;
                }

                Widget.Find<Menu>().CombinationClick(_slotIndex);
            }
            else
            {
                ShowPopup();
            }
        }

        private void ShowPopup()
        {
            if (!(_combinationSlotState?.Result is CombinationConsumable5.ResultModel))
            {
                return;
            }

            if (_combinationSlotState.Result.itemUsable.RequiredBlockIndex >
                Game.Game.instance.Agent.BlockIndex)
            {
                Widget.Find<CombinationSlotPopup>().Pop(_combinationSlotState, _slotIndex);
            }
        }
    }
}
