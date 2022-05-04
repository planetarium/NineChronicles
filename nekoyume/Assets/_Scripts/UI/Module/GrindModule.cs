using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using Libplanet.Assets;
    using System.Collections;
    using UniRx;
    public class GrindModule : MonoBehaviour
    {
        [SerializeField]
        private Inventory grindInventory;

        [SerializeField]
        private ConditionalCostButton grindButton;

        [SerializeField]
        private StakingBonus stakingBonus;

        [SerializeField]
        private GameObject stakingBonusDisabledObject;

        [SerializeField]
        private List<GrindingItemSlot> itemSlots;

        // TODO: It is used when NCG can be obtained through grinding later.
        [SerializeField]
        private GameObject ncgRewardObject;

        [SerializeField]
        private TMP_Text ncgRewardText;

        [SerializeField]
        private TMP_Text crystalRewardText;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private ItemMoveAnimation crystalMoveAnimation;

        private bool _isInitialized;

        private int _inventoryApStoneCount;

        private FungibleAssetValue _cachedGrindingRewardNCG;

        private FungibleAssetValue _cachedGrindingRewardCrystal;

        private readonly ReactiveCollection<InventoryItem> _selectedItemsForGrind =
            new ReactiveCollection<InventoryItem>();

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private static readonly List<(ItemType type, Predicate<InventoryItem>)>
            DimConditionPredicateList
                = new List<(ItemType type, Predicate<InventoryItem>)>
                {
                    (ItemType.Equipment, InventoryHelper.CheckCanNotGrinding),
                    (ItemType.Consumable, _ => false),
                    (ItemType.Material, _ => false),
                    (ItemType.Costume, _ => false),
                };

        private const int LimitGrindingCount = 10;

        private static readonly Vector3 CrystalMovePositionOffset = new Vector3(0.05f, 0.05f);

        private bool CanGrind => _selectedItemsForGrind.Any() &&
                                 _selectedItemsForGrind.All(item => !item.Equipped.Value);

        public void Show(bool reverseInventoryOrder = true)
        {
            gameObject.SetActive(true);

            Initialize();
            Subscribe();

            _selectedItemsForGrind.Clear();
            grindInventory.SetGrinding(ShowItemTooltip,
                OnUpdateInventory,
                DimConditionPredicateList,
                reverseInventoryOrder);
            grindButton.Interactable = false;
            UpdateStakingBonusObject(States.Instance.MonsterCollectionState?.Level ?? 0);
            crystalRewardText.text = string.Empty;
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            grindButton.SetCost(ConditionalCostButton.CostType.ActionPoint, 5);
            grindButton.SetCondition(() => CanGrind);
            stakingBonus.SetBonusTextFunc(level =>
            {
                if (level > 0 &&
                    TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet.TryGetValue(level,
                        out var row))
                {
                    return $"+{row.Multiplier}%";
                }

                return "+0%";
            });

            _isInitialized = true;
        }

        private void Subscribe()
        {
            grindButton.OnSubmitSubject.Subscribe(_ =>
            {
                Action(_selectedItemsForGrind.Select(inventoryItem =>
                    (Equipment) inventoryItem.ItemBase).ToList(), false);
            }).AddTo(_disposables);
            grindButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                var l10nkey = _selectedItemsForGrind.Any()
                    ? "ERROR_NOT_GRINDING_EQUIPPED"
                    : "GRIND_UI_SLOTNOTICE";
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize(l10nkey),
                    NotificationCell.NotificationType.Notification);
            }).AddTo(_disposables);
            grindButton.OnClickSubject.Subscribe(state =>
            {
                if (state == ConditionalButton.State.Conditional)
                {
                    Action(_selectedItemsForGrind.Select(inventoryItem =>
                        (Equipment) inventoryItem.ItemBase).ToList(), true);
                }
            }).AddTo(_disposables);

            _selectedItemsForGrind.ObserveAdd().Subscribe(item =>
            {
                item.Value.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrind.Count);
                itemSlots[item.Index].UpdateSlot(item.Value);
                item.Value.GrindingCountEnabled.OnNext(true);
            }).AddTo(_disposables);
            _selectedItemsForGrind.ObserveRemove().Subscribe(item =>
            {
                var listSize = _selectedItemsForGrind.Count;
                for (int i = item.Index; i < LimitGrindingCount; i++)
                {
                    if (i < listSize)
                    {
                        _selectedItemsForGrind[i].GrindingCount.SetValueAndForceNotify(i + 1);
                        itemSlots[i].UpdateSlot(_selectedItemsForGrind[i]);
                    }
                    else
                    {
                        itemSlots[i].UpdateSlot();
                    }
                }
                item.Value.GrindingCount.SetValueAndForceNotify(0);
            }).AddTo(_disposables);
            _selectedItemsForGrind.ObserveReset().Subscribe(_ =>
            {
                itemSlots.ForEach(slot => slot.UpdateSlot());
            }).AddTo(_disposables);
            _selectedItemsForGrind.ObserveCountChanged().Subscribe(count =>
            {
                grindButton.Interactable = CanGrind;
                UpdateCrystalReward();
            }).AddTo(_disposables);

            ReactiveAvatarState.ActionPoint
                .Subscribe(_ => grindButton.Interactable = CanGrind)
                .AddTo(_disposables);

            MonsterCollectionStateSubject.Level
                .Subscribe(UpdateStakingBonusObject)
                .AddTo(_disposables);

            itemSlots.ForEach(slot => slot.OnClick.Subscribe(_ =>
            {
                _selectedItemsForGrind.Remove(slot.AssignedItem);
            }).AddTo(_disposables));
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            var isRegister = !_selectedItemsForGrind.Contains(model);
            var isEquipment = model.ItemBase.ItemType == ItemType.Equipment;
            var interactable =
                isEquipment && _selectedItemsForGrind.Count < 10 && !InventoryHelper.CheckCanNotGrinding(model)
                || !isRegister;
            var onSubmit = isEquipment
                ? new System.Action(() => RegisterToGrindingList(model, isRegister))
                : null;
            var blockMessage = model.Equipped.Value ? "ERROR_NOT_GRINDING_EQUIPPED" : "ERROR_NOT_GRINDING_10OVER";
            var onBlock = new System.Action(() =>
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize(blockMessage),
                    NotificationCell.NotificationType.Alert));
            tooltip.Show(
                model,
                isRegister
                    ? L10nManager.Localize("UI_COMBINATION_REGISTER_MATERIAL")
                    : L10nManager.Localize("UI_COMBINATION_UNREGISTER_MATERIAL"),
                interactable,
                onSubmit,
                grindInventory.ClearSelectedItem,
                onBlock,
                target);
        }

        private void OnUpdateInventory(Inventory inventory, Nekoyume.Model.Item.Inventory inventoryModel)
        {
            var selectedItemCount = _selectedItemsForGrind.Count;
            for (int i = 0; i < selectedItemCount; i++)
            {
                if (inventory.TryGetModel(_selectedItemsForGrind[i].ItemBase, out var inventoryItem))
                {
                    inventoryItem.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrind[i].GrindingCount.Value);
                    _selectedItemsForGrind[i] = inventoryItem;
                    itemSlots[i].UpdateSlot(_selectedItemsForGrind[i]);
                }
            }
            grindButton.Interactable = CanGrind;

            _inventoryApStoneCount = 0;
            foreach (var item in inventoryModel.Items.Where(x=> x.item.ItemSubType == ItemSubType.ApStone))
            {
                if (item.Locked)
                {
                    continue;
                }

                if (item.item is ITradableItem tradableItem)
                {
                    var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                    if (tradableItem.RequiredBlockIndex > blockIndex)
                    {
                        continue;
                    }

                    _inventoryApStoneCount += item.count;
                }
                else
                {
                    _inventoryApStoneCount += item.count;
                }
            }
        }

        private void RegisterToGrindingList(InventoryItem item, bool isRegister)
        {
            if (isRegister)
            {
                _selectedItemsForGrind.Add(item);
            }
            else
            {
                _selectedItemsForGrind.Remove(item);
            }
        }

        private void UpdateStakingBonusObject(int level)
        {
            stakingBonus.OnUpdateStakingLevel(level);

            if (_selectedItemsForGrind.Any())
            {
                UpdateCrystalReward();
            }
        }

        private void UpdateCrystalReward()
        {
            _cachedGrindingRewardCrystal = Grinding.CalculateCrystal(
                    _selectedItemsForGrind.Select(item => (Equipment)item.ItemBase),
                    TableSheets.Instance.CrystalEquipmentGrindingSheet,
                    States.Instance.MonsterCollectionState?.Level ?? 0,
                    TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet);
            crystalRewardText.text = _cachedGrindingRewardCrystal.MajorUnit > 0 ?
                _cachedGrindingRewardCrystal.GetQuantityString() :
                string.Empty;
        }

        /// <summary>
        /// Returns true if any of the selected equipment has enhanced equipment or has skills.
        /// </summary>
        /// <param name="equipments"></param>
        /// <returns></returns>
        private static bool CheckSelectedItemsAreStrong(List<Equipment> equipments)
        {
            return equipments.Exists(item =>
                item.level > 0 || item.Skills.Any() || item.BuffSkills.Any());
        }

        private void Action(List<Equipment> equipments, bool chargeAp)
        {
            if (!equipments.Any() || equipments.Count > LimitGrindingCount)
            {
                Debug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
                return;
            }

            if (CheckSelectedItemsAreStrong(equipments))
            {
                var system = Widget.Find<IconAndButtonSystem>();
                system.ShowWithTwoButton("UI_CONFIRM",
                    "UI_GRINDING_CONFIRM",
                    "UI_OK",
                    "UI_CANCEL",
                    true,
                    IconAndButtonSystem.SystemType.Information);
                system.ConfirmCallback = () =>
                {
                    if (chargeAp)
                    {
                        CheckUseApPotionForAction(equipments);
                    }
                    else
                    {
                        PushAction(equipments, false);
                    }
                };
            }
            else
            {
                CheckUseApPotionForAction(equipments);
            }
        }

        private void CheckUseApPotionForAction(List<Equipment> equipments)
        {
            switch (grindButton.CurrentState.Value)
            {
                case ConditionalButton.State.Conditional:
                {
                    if (_inventoryApStoneCount > 0)
                    {
                        var confirm = Widget.Find<IconAndButtonSystem>();
                        confirm.ShowWithTwoButton(L10nManager.Localize("UI_CONFIRM"),
                            L10nManager.Localize("UI_APREFILL_GUIDE_FORMAT", L10nManager.Localize("GRIND_UI_BUTTON"), _inventoryApStoneCount),
                            L10nManager.Localize("UI_OK"),
                            L10nManager.Localize("UI_CANCEL"),
                            false, IconAndButtonSystem.SystemType.Information);
                        confirm.ConfirmCallback = () =>
                            PushAction(equipments, true);
                        confirm.CancelCallback = () => confirm.Close();
                    }
                    else
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("ERROR_ACTION_POINT"),
                            NotificationCell.NotificationType.Alert);
                    }

                    break;
                }
                case ConditionalButton.State.Normal:
                    PushAction(equipments, false);
                    break;
                case ConditionalButton.State.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PushAction(List<Equipment> equipments, bool chargeAp)
        {
            StartCoroutine(CoCombineNPCAnimation());
            ActionManager.Instance.Grinding(equipments, chargeAp).Subscribe();
            _selectedItemsForGrind.Clear();
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(true);
        }

        private IEnumerator CoCombineNPCAnimation()
        {
            var loadingScreen = Widget.Find<CombinationLoadingScreen>();
            loadingScreen.OnDisappear = OnNPCDisappear;
            loadingScreen.Show();
            canvasGroup.interactable = false;
            loadingScreen.SetCurrency(
                (int)_cachedGrindingRewardNCG.MajorUnit,
                (int)_cachedGrindingRewardCrystal.MajorUnit);
            yield return new WaitForSeconds(.5f);

            var quote = L10nManager.Localize("UI_GRIND_NPC_QUOTE");
            loadingScreen.AnimateNPC(ItemType.Equipment, quote);
            loadingScreen.SetCloseAction(() =>
            {
                StartCoroutine(ItemMoveAnimationFactory.CoItemMoveAnimation(
                    ItemMoveAnimationFactory.AnimationItemType.Crystal,
                    crystalRewardText.transform.position,
                    Widget.Find<HeaderMenuStatic>().Crystal.IconPosition +
                    CrystalMovePositionOffset,
                    30));
            });
        }

        private void OnNPCDisappear()
        {
            canvasGroup.interactable = true;
        }
    }
}
