using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using Nekoyume.UI.Tween;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using Libplanet.Assets;
    using mixpanel;
    using System.Collections;
    using UniRx;
    using Vector3 = UnityEngine.Vector3;

    public class GrindModule : MonoBehaviour
    {
        [Serializable]
        private struct CrystalAnimationData
        {
            public int maximum;
            public int middle;
            public int minimum;
        }

        [SerializeField]
        private Inventory grindInventory;

        [SerializeField]
        private ConditionalCostButton grindButton;

        [SerializeField]
        private StakingBonus stakingBonus;

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
        private Animator animator;

        [SerializeField]
        private CrystalAnimationData animationData;

        [SerializeField]
        private DigitTextTweener crystalRewardTweener;

        private bool _isInitialized;

        private int _inventoryApStoneCount;

        private FungibleAssetValue _cachedGrindingRewardNCG;

        private FungibleAssetValue _cachedGrindingRewardCrystal;

        private readonly ReactiveCollection<InventoryItem> _selectedItemsForGrind = new();

        private readonly List<IDisposable> _disposables = new();

        private static readonly List<(ItemType type, Predicate<InventoryItem>)>
            DimConditionPredicateList = new()
            {
                (ItemType.Equipment, _ => false),
                (ItemType.Consumable, _ => true),
                (ItemType.Material, _ => true),
                (ItemType.Costume, _ => true),
            };

        private const int LimitGrindingCount = 10;
        private static readonly BigInteger MaximumCrystal = 100_000;
        private static readonly BigInteger MiddleCrystal = 1_000;

        public static readonly Vector3 CrystalMovePositionOffset = new(0.05f, 0.05f);
        private static readonly int FirstRegister = Animator.StringToHash("FirstRegister");
        private static readonly int StartGrind = Animator.StringToHash("StartGrind");
        private static readonly int EmptySlot = Animator.StringToHash("EmptySlot");
        private static readonly int ShowAnimationHash = Animator.StringToHash("Show");

        private bool CanGrind => _selectedItemsForGrind.Any();

        public void Show(bool reverseInventoryOrder = true)
        {
            gameObject.SetActive(true);
            if (animator)
            {
                animator.Play(ShowAnimationHash);
            }

            Initialize();
            Subscribe();

            _selectedItemsForGrind.Clear();
            grindInventory.SetGrinding(OnClickItem,
                OnUpdateInventory,
                DimConditionPredicateList,
                reverseInventoryOrder);
            grindButton.Interactable = false;
            UpdateStakingBonusObject(States.Instance.StakingLevel);
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

            grindButton.SetCost(CostType.ActionPoint, 5);
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
                switch (state)
                {
                    case ConditionalButton.State.Normal:
                        Action(_selectedItemsForGrind.Select(inventoryItem =>
                            (Equipment)inventoryItem.ItemBase).ToList());
                        break;
                    case ConditionalButton.State.Conditional:
                        Action(_selectedItemsForGrind.Select(inventoryItem =>
                            (Equipment)inventoryItem.ItemBase).ToList());
                        break;
                    case ConditionalButton.State.Disabled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }).AddTo(_disposables);

            _selectedItemsForGrind.ObserveAdd().Subscribe(item =>
            {
                item.Value.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrind.Count);
                itemSlots[item.Index].UpdateSlot(item.Value);
                item.Value.GrindingCountEnabled.OnNext(true);

                if (_selectedItemsForGrind.Count == 1 && animator)
                {
                    animator.SetTrigger(FirstRegister);
                }
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

                if (_selectedItemsForGrind.Count == 0)
                {
                    animator.SetTrigger(EmptySlot);
                }
            }).AddTo(_disposables);
            _selectedItemsForGrind.ObserveReset()
                .Subscribe(_ => { itemSlots.ForEach(slot => slot.ResetSlot()); })
                .AddTo(_disposables);
            _selectedItemsForGrind.ObserveCountChanged().Subscribe(count =>
            {
                grindButton.Interactable = CanGrind;
                UpdateCrystalReward();
            }).AddTo(_disposables);

            ReactiveAvatarState.ActionPoint
                .Subscribe(_ => grindButton.Interactable = CanGrind)
                .AddTo(_disposables);

            StakingLevelSubject.Level
                .Subscribe(UpdateStakingBonusObject)
                .AddTo(_disposables);

            itemSlots.ForEach(slot =>
                slot.OnClick.Subscribe(_ => { _selectedItemsForGrind.Remove(slot.AssignedItem); }).AddTo(_disposables));
        }

        private void OnClickItem(InventoryItem model)
        {
            var isRegister = !_selectedItemsForGrind.Contains(model);
            var isEquipment = model.ItemBase.ItemType == ItemType.Equipment;
            var isEquipped = model.Equipped.Value;
            var isValid =
                _selectedItemsForGrind.Count < 10 && isEquipment
                || !isRegister;

            if (isValid)
            {
                if (isEquipped)
                {
                    var confirm = Widget.Find<IconAndButtonSystem>();
                    confirm.ConfirmCallback = () => RegisterToGrindingList(model, isRegister);
                    confirm.ShowWithTwoButton(
                        "UI_CONFIRM",
                        "UI_CONFIRM_EQUIPPED_GRINDING",
                        type: IconAndButtonSystem.SystemType.Information);
                }
                else
                {
                    RegisterToGrindingList(model, isRegister);
                }
            }
            else
            {
                const string blockMessage = "ERROR_NOT_GRINDING_10OVER";
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize(blockMessage),
                    NotificationCell.NotificationType.Alert);
            }

            grindInventory.ClearSelectedItem();
        }

        private void OnUpdateInventory(Inventory inventory, Nekoyume.Model.Item.Inventory inventoryModel)
        {
            var selectedItemList = _selectedItemsForGrind.OrderBy(i => i.GrindingCount.Value).ToList();
            _selectedItemsForGrind.Clear();
            var notExistItemCount = 0;
            var slotIndex = 0;
            foreach (var inventoryItem in selectedItemList)
            {
                if (inventory.TryGetModel(inventoryItem.ItemBase, out var newItem))
                {
                    newItem.GrindingCount.SetValueAndForceNotify(
                        inventoryItem.GrindingCount.Value - notExistItemCount);
                    itemSlots[slotIndex - notExistItemCount].UpdateSlot(inventoryItem);
                    _selectedItemsForGrind.Add(newItem);
                }
                else
                {
                    notExistItemCount++;
                }

                slotIndex++;
            }

            grindButton.Interactable = CanGrind;

            _inventoryApStoneCount = 0;
            foreach (var item in inventoryModel.Items.Where(x => x.item.ItemSubType == ItemSubType.ApStone))
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
            var prevCrystalReward = _cachedGrindingRewardCrystal.MajorUnit;
            _cachedGrindingRewardCrystal = CrystalCalculator.CalculateCrystal(
                _selectedItemsForGrind.Select(item => (Equipment)item.ItemBase),
                false,
                TableSheets.Instance.CrystalEquipmentGrindingSheet,
                TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                States.Instance.StakingLevel);
            if (_cachedGrindingRewardCrystal.MajorUnit > 0)
            {
                if (prevCrystalReward > _cachedGrindingRewardCrystal.MajorUnit)
                {
                    crystalRewardText.text = _cachedGrindingRewardCrystal.GetQuantityString();
                }
                else
                {
                    crystalRewardTweener.Play(
                        (long) prevCrystalReward,
                        (long) _cachedGrindingRewardCrystal.MajorUnit);
                }
            }
            else
            {
                crystalRewardText.text = string.Empty;
            }
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

        private void Action(List<Equipment> equipments)
        {
            if (!equipments.Any() || equipments.Count > LimitGrindingCount)
            {
                Debug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
                return;
            }

            if (CheckSelectedItemsAreStrong(equipments))
            {
                var system = Widget.Find<IconAndButtonSystem>();
                system.ShowWithTwoButton("UI_WARNING",
                    "UI_GRINDING_CONFIRM");
                system.ConfirmCallback = () => { CheckUseApPotionForAction(equipments); };
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
                            L10nManager.Localize("UI_APREFILL_GUIDE_FORMAT", L10nManager.Localize("GRIND_UI_BUTTON"),
                                _inventoryApStoneCount),
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
            StartCoroutine(CoCombineNPCAnimation(_cachedGrindingRewardCrystal.MajorUnit));
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(true);

            ActionManager.Instance
                .Grinding(equipments, chargeAp, (int) _cachedGrindingRewardCrystal.MajorUnit)
                .Subscribe(eval =>
                {
                    Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
                    if (eval.Exception != null)
                    {
                        Debug.LogException(eval.Exception.InnerException);
                        OneLineSystem.Push(
                            MailType.Grinding,
                            L10nManager.Localize("ERROR_UNKNOWN"),
                            NotificationCell.NotificationType.Alert);
                    }
                });
            _selectedItemsForGrind.Clear();
            if (animator)
            {
                animator.SetTrigger(StartGrind);
            }
        }

        private IEnumerator CoCombineNPCAnimation(BigInteger rewardCrystal)
        {
            var loadingScreen = Widget.Find<GrindingLoadingScreen>();
            loadingScreen.OnDisappear = OnNPCDisappear;
            loadingScreen.SetCurrency(
                (long)_cachedGrindingRewardNCG.MajorUnit,
                (long)_cachedGrindingRewardCrystal.MajorUnit);
            loadingScreen.CrystalAnimationCount = GetCrystalMoveAnimationCount(rewardCrystal);
            canvasGroup.interactable = false;

            yield return null;
            yield return new WaitUntil(() =>
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= .5f);
            loadingScreen.Show();
            var quote = L10nManager.Localize("UI_GRIND_NPC_QUOTE");
            loadingScreen.AnimateNPC(quote);
        }

        private void OnNPCDisappear()
        {
            canvasGroup.interactable = true;
        }

        private int GetCrystalMoveAnimationCount(BigInteger crystal)
        {
            if (crystal > MaximumCrystal)
            {
                return animationData.maximum;
            }

            if (crystal > MiddleCrystal)
            {
                return animationData.middle;
            }

            return animationData.minimum;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    StartCoroutine(CoCombineNPCAnimation(MiddleCrystal));
                }
                else if (Input.GetKeyDown(KeyCode.W))
                {
                    StartCoroutine(CoCombineNPCAnimation(MiddleCrystal + 1));
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    StartCoroutine(CoCombineNPCAnimation(MaximumCrystal + 1));
                }
            }
        }
#endif
    }
}
