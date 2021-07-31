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
        public enum SlotType
        {
            Lock,
            Empty,
            Appraise,
            Working,
        }

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

        private CombinationSlotState _state;
        private const int UnlockStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;

        public SlotType Type { get; private set;  } = SlotType.Empty;
        public bool IsCached { get; set; } = false;

        private void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex).AddTo(gameObject);

            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                OnClickSlot(Type, _state);
            }).AddTo(gameObject);

            itemView.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                OnClickSlot(Type, _state);
            }).AddTo(gameObject);
        }

        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            Type = GetSlotType(_state, currentBlockIndex, IsCached);
            UpdateInformation(Type, currentBlockIndex, _state, IsCached);
        }

        public void SetSlot(long currentBlockIndex, CombinationSlotState state = null)
        {
            _state = state;
            Type = GetSlotType(state, currentBlockIndex, IsCached);
            UpdateInformation(Type, currentBlockIndex, state, IsCached);
        }

        private void UpdateInformation(SlotType type, long currentBlockIndex, CombinationSlotState state, bool isCached)
        {
            switch (type)
            {
                case SlotType.Lock:
                    SetContainer(true, false, false);
                    var text = L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE");
                    lockText.text = string.Format(text, UnlockStage);
                    break;

                case SlotType.Empty:
                    SetContainer(false, false, true);
                    itemView.Clear();
                    break;

                case SlotType.Appraise:
                    SetContainer(false, true, false);
                    preparingContainer.gameObject.SetActive(true);
                    workingContainer.gameObject.SetActive(false);
                    hasNotificationImage.enabled = false;
                    randomNumberRoulette.Stop();
                    break;

                case SlotType.Working:
                    SetContainer(false, true, false);
                    preparingContainer.gameObject.SetActive(false);
                    workingContainer.gameObject.SetActive(true);
                    UpdateItemInformation(state.Result.itemUsable);
                    randomNumberRoulette.Play();
                    UpdateHourglass(state, currentBlockIndex);
                    UpdateRequiredBlockInformation(state, currentBlockIndex);
                    UpdateNotification(state, currentBlockIndex, isCached);
                    break;
            }
        }

        private void SetContainer(bool isLock, bool isWorking, bool isEmpty)
        {
            lockContainer.gameObject.SetActive(isLock);
            baseContainer.gameObject.SetActive(isWorking);
            noneContainer.gameObject.SetActive(isEmpty);
        }

        private static SlotType GetSlotType(CombinationSlotState state, long currentBlockIndex, bool isCached)
        {
            var isLock = !States.Instance.CurrentAvatarState?.worldInformation.IsStageCleared(UnlockStage) ?? true;
            if (isLock)
            {
                return SlotType.Lock;
            }

            if (state?.Result is null)
            {
                return isCached ? SlotType.Appraise:  SlotType.Empty;
            }

            return currentBlockIndex < state.StartBlockIndex + GameConfig.RequiredAppraiseBlock
                ? SlotType.Appraise
                : SlotType.Working;
        }

        private void UpdateRequiredBlockInformation(CombinationSlotState state, long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(state.RequiredBlockIndex, 1);
            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}.";
        }

        private void UpdateNotification(CombinationSlotState state, long currentBlockIndex, bool isCached)
        {
            if (GetSlotType(state, currentBlockIndex, isCached) != SlotType.Working)
            {
                hasNotificationImage.enabled = false;
                return;
            }
            var gameConfigState = Game.Game.instance.States.GameConfigState;
            var diff = state.RequiredBlockIndex - currentBlockIndex;
            var cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            var isEnough = States.Instance.CurrentAvatarState.inventory.HasFungibleItem(row.ItemId, currentBlockIndex, cost);

            hasNotificationImage.enabled = isEnough;
        }

        private void UpdateHourglass(CombinationSlotState state, long currentBlockIndex)
        {
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            var cost =
                RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            var count = GetHourglassCount(currentBlockIndex);
            hourglassCountText.text = cost.ToString();
            hourglassCountText.color = count >= cost
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }

        private void UpdateItemInformation(ItemUsable item)
        {
            itemView.SetData(new Item(item));
            itemNameText.text = GetItemName(item);
        }

        private static int GetHourglassCount(long currentBlockIndex)
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
                    if (tradableItem.RequiredBlockIndex > currentBlockIndex)
                    {
                        continue;
                    }
                }

                count += item.count;
            }

            return count;
        }

        private static string GetItemName(ItemUsable itemUsable)
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

        private static void OnClickSlot(SlotType type, CombinationSlotState state)
        {
            switch (type)
            {
                case SlotType.Empty:
                    if (Game.Game.instance.Stage.IsInStage)
                    {
                        return;
                    }

                    Widget.Find<HeaderMenu>().UpdateAssets(HeaderMenu.AssetVisibleState.Combination);
                    Widget.Find<Craft>().Show();
                    Widget.Find<CombinationSlots>().Close();
                    break;

                case SlotType.Working:
                    Widget.Find<CombinationSlotPopup>().Pop(state);
                    break;
            }
        }
    }
}
