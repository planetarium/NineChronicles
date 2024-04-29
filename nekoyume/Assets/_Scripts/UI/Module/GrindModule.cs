using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Types.Assets;
using Nekoyume.Blockchain;
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
        private GrindingItemSlotScroll scroll;

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
        private ConditionalButton removeAllButton;

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

        private const int LimitGrindingCount = 50;
        private static readonly BigInteger MaximumCrystal = 100_000;
        private static readonly BigInteger MiddleCrystal = 1_000;

        public static readonly Vector3 CrystalMovePositionOffset = new(0.05f, 0.05f);
        private static readonly int FirstRegister = Animator.StringToHash("FirstRegister");
        private static readonly int StartGrind = Animator.StringToHash("StartGrind");
        private static readonly int EmptySlot = Animator.StringToHash("EmptySlot");

        private bool CanGrind => _selectedItemsForGrind.Any();

        public void Show(bool reverseInventoryOrder = false)
        {
            gameObject.SetActive(true);

            Initialize();
            Subscribe();

            _selectedItemsForGrind.Clear();
            grindInventory.SetGrinding(OnClickItem,
                OnUpdateInventory,
                DimConditionPredicateList,
                reverseInventoryOrder);
            UpdateScroll();
            UpdateStakingBonusObject(States.Instance.StakingLevel);
            crystalRewardText.text = string.Empty;
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
            _selectedItemsForGrind.Clear();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            grindButton.SetCost(CostType.ActionPoint, 5);
            grindButton.SetCondition(() => CanGrind);
            removeAllButton.OnSubmitSubject.Subscribe(_ =>
            {
                foreach (var item in _selectedItemsForGrind.ToList())
                {
                    item.GrindingCountEnabled.SetValueAndForceNotify(false);
                }

                _selectedItemsForGrind.Clear();
            }).AddTo(gameObject);
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

            grindButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                var l10nkey = scroll.DataCount > 0
                    ? "GRIND_UI_SLOTNOTICE"
                    : "ERROR_NOT_GRINDING_EQUIPPED";
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize(l10nkey),
                    NotificationCell.NotificationType.Notification);
            }).AddTo(gameObject);
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
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveAdd().Subscribe(item =>
            {
                item.Value.GrindingCountEnabled.SetValueAndForceNotify(true);

                if (_selectedItemsForGrind.Count == 1 && animator)
                {
                    animator.SetTrigger(FirstRegister);
                }
            }).AddTo(gameObject);
            _selectedItemsForGrind.ObserveRemove().Subscribe(item =>
            {
                item.Value.GrindingCountEnabled.SetValueAndForceNotify(false);
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveCountChanged().Subscribe(_ =>
            {
                UpdateScroll();
                if (_selectedItemsForGrind.Count == 0)
                {
                    animator.SetTrigger(EmptySlot);
                }
            }).AddTo(gameObject);

            scroll.OnClick
                .Subscribe(item => _selectedItemsForGrind.Remove(item))
                .AddTo(gameObject);

            _isInitialized = true;
        }

        private void Subscribe()
        {
            ReactiveAvatarState.ObservableActionPoint
                .Subscribe(_ => grindButton.Interactable = CanGrind)
                .AddTo(_disposables);

            StakingSubject.Level
                .Subscribe(UpdateStakingBonusObject)
                .AddTo(_disposables);
        }

        private void OnClickItem(InventoryItem model)
        {
            var isRegister = !_selectedItemsForGrind.Contains(model);
            var inLimit = _selectedItemsForGrind.Count < LimitGrindingCount;
            var isEquipment = model.ItemBase.ItemType == ItemType.Equipment;
            var isEquipped = model.Equipped.Value;
            var isValid = inLimit && isEquipment || !isRegister;

            if (isValid)
            {
                if (isEquipped && isRegister)
                {
                    var confirm = Widget.Find<IconAndButtonSystem>();
                    confirm.ConfirmCallback = () => RegisterToGrindingList(model, true);
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
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_NOT_GRINDING_OVER", LimitGrindingCount),
                    NotificationCell.NotificationType.Alert);
            }

            grindInventory.ClearSelectedItem();
        }

        private void OnUpdateInventory(Inventory inventory, Nekoyume.Model.Item.Inventory inventoryModel)
        {
            var selectedItemList = _selectedItemsForGrind.ToList();
            _selectedItemsForGrind.Clear();
            foreach (var inventoryItem in selectedItemList)
            {
                if (inventory.TryGetModel(inventoryItem.ItemBase, out var newItem))
                {
                    _selectedItemsForGrind.Add(newItem);
                }
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

        private void UpdateScroll()
        {
            var count = _selectedItemsForGrind.Count;
            grindButton.Interactable = CanGrind;
            removeAllButton.Interactable = count > 1;
            scroll.UpdateData(_selectedItemsForGrind);
            scroll.RawJumpTo(count - 1);

            if (_selectedItemsForGrind.Any())
            {
                UpdateCrystalReward();
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
                crystalRewardTweener.Play(
                        (long) prevCrystalReward,
                        (long) _cachedGrindingRewardCrystal.MajorUnit);
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
                NcDebug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
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
                .Grinding(equipments, chargeAp, (long)_cachedGrindingRewardCrystal.MajorUnit)
                .Subscribe(eval =>
                {
                    Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
                    if (eval.Exception != null)
                    {
                        NcDebug.LogException(eval.Exception.InnerException);
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
