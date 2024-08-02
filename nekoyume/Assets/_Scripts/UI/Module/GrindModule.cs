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

        [SerializeField]
        private GrindReward[] grindRewards;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private ConditionalButton removeAllButton;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private CrystalAnimationData animationData;

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
                (ItemType.Costume, _ => true)
            };

        private const int LimitGrindingCount = 50;
        private static readonly BigInteger MaximumCrystal = 100_000;
        private static readonly BigInteger MiddleCrystal = 1_000;

        public static readonly Vector3 CrystalMovePositionOffset = new(0.05f, 0.05f);
        private static readonly int FirstRegister = Animator.StringToHash("FirstRegister");
        private static readonly int StartGrind = Animator.StringToHash("StartGrind");
        private static readonly int EmptySlot = Animator.StringToHash("EmptySlot");

        private bool CanGrind => _selectedItemsForGrind.Any();

        private void Awake()
        {
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
                var message = scroll.DataCount > 0
                    ? "GRIND_UI_SLOTNOTICE"
                    : "ERROR_NOT_GRINDING_EQUIPPED";
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize(message),
                    NotificationCell.NotificationType.Notification);
            }).AddTo(gameObject);
            grindButton.OnClickSubject.Subscribe(state =>
            {
                switch (state)
                {
                    case ConditionalButton.State.Normal:
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
            _selectedItemsForGrind.ObserveRemove()
                .Subscribe(item => item.Value.GrindingCountEnabled.SetValueAndForceNotify(false))
                .AddTo(gameObject);
            _selectedItemsForGrind.ObserveCountChanged().Subscribe(_ =>
            {
                UpdateScroll();

                if (_selectedItemsForGrind.Any())
                {
                    UpdateReward();
                }
                else
                {
                    animator.SetTrigger(EmptySlot);
                }
            }).AddTo(gameObject);

            scroll.OnClick
                .Subscribe(item => _selectedItemsForGrind.Remove(item))
                .AddTo(gameObject);
        }

        public void Show(bool reverseInventoryOrder = false)
        {
            gameObject.SetActive(true);

            Subscribe();

            _selectedItemsForGrind.Clear();
            grindInventory.SetGrinding(
                OnClickItem,
                OnUpdateInventory,
                DimConditionPredicateList,
                reverseInventoryOrder);
            UpdateScroll();
            UpdateStakingBonusObject(States.Instance.StakingLevel);
        }

        private void Subscribe()
        {
            ReactiveAvatarState.ObservableActionPoint
                .Subscribe(_ => grindButton.UpdateObjects())
                .AddTo(_disposables);

            StakingSubject.Level
                .Subscribe(level =>
                {
                    UpdateStakingBonusObject(level);

                    if (_selectedItemsForGrind.Any())
                    {
                        UpdateReward();
                    }
                })
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
            _selectedItemsForGrind.Clear();
        }

        private void OnClickItem(InventoryItem model)
        {
            var isRegister = !_selectedItemsForGrind.Contains(model);
            var inLimit = _selectedItemsForGrind.Count < LimitGrindingCount;
            var isEquipment = model.ItemBase.ItemType == ItemType.Equipment;
            var isEquipped = model.Equipped.Value;
            var isValid = (inLimit && isEquipment) || !isRegister;

            if (isValid)
            {
                if (isEquipped && isRegister)
                {
                    var confirm = Widget.Find<IconAndButtonSystem>();
                    confirm.ConfirmCallback = RegisterForGrind;
                    confirm.ShowWithTwoButton(
                        "UI_CONFIRM",
                        "UI_CONFIRM_EQUIPPED_GRINDING",
                        type: IconAndButtonSystem.SystemType.Information);
                }
                else
                {
                    RegisterForGrind();
                }
            }
            else
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_NOT_GRINDING_OVER", LimitGrindingCount),
                    NotificationCell.NotificationType.Alert);
            }

            grindInventory.ClearSelectedItem();

            void RegisterForGrind()
            {
                if (isRegister)
                {
                    _selectedItemsForGrind.Add(model);
                }
                else
                {
                    _selectedItemsForGrind.Remove(model);
                }
            }
        }

        private void OnUpdateInventory(Inventory inventory, Nekoyume.Model.Item.Inventory inventoryModel)
        {
            var selectedItems = _selectedItemsForGrind.ToList();
            _selectedItemsForGrind.Clear();
            foreach (var selectedItem in selectedItems)
            {
                // Check if the item is still in the inventory.
                if (inventory.TryGetModel(selectedItem.ItemBase, out var item))
                {
                    _selectedItemsForGrind.Add(item);
                }
            }

            grindButton.UpdateObjects();
            _inventoryApStoneCount = inventoryModel.GetUsableItemCount(
                (int)CostType.ApPotion,
                Game.Game.instance.Agent?.BlockIndex ?? -1);
        }

        private void UpdateScroll()
        {
            var count = _selectedItemsForGrind.Count;
            grindButton.UpdateObjects();
            removeAllButton.Interactable = count > 1;
            scroll.UpdateData(_selectedItemsForGrind);
            scroll.RawJumpTo(count - 1);
        }

        private void UpdateStakingBonusObject(int level)
        {
            stakingBonus.OnUpdateStakingLevel(level);
        }

        private void UpdateReward()
        {
            var crystalReward = CrystalCalculator.CalculateCrystal(
                _selectedItemsForGrind.Select(item => (Equipment)item.ItemBase),
                false,
                TableSheets.Instance.CrystalEquipmentGrindingSheet,
                TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                States.Instance.StakingLevel);
            grindRewards[0].SetCrystalReward(crystalReward);
        }

        #region Action

        private void Action(List<Equipment> equipments)
        {
            if (!equipments.Any() || equipments.Count > LimitGrindingCount)
            {
                NcDebug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
                return;
            }

            CheckGrindStrongEquipment(equipments, () =>
                CheckChargeAp(chargeAp =>
                    PushAction(equipments, chargeAp)));
        }

        private void CheckGrindStrongEquipment(List<Equipment> equipments, System.Action callback)
        {
            if (equipments.Exists(IsStrong))
            {
                var system = Widget.Find<IconAndButtonSystem>();
                system.ShowWithTwoButton("UI_WARNING", "UI_GRINDING_CONFIRM");
                system.ConfirmCallback = callback;
            }
            else
            {
                callback();
            }

            // Returns equipment is enhanced or has skills.
            bool IsStrong(Equipment equipment) =>
                equipment.level > 0 || equipment.Skills.Any() || equipment.BuffSkills.Any();
        }

        private void CheckChargeAp(Action<bool> chargeAp)
        {
            switch (grindButton.CurrentState.Value)
            {
                case ConditionalButton.State.Conditional:
                {
                    if (_inventoryApStoneCount > 0)
                    {
                        var confirm = Widget.Find<IconAndButtonSystem>();
                        confirm.ShowWithTwoButton(
                            L10nManager.Localize("UI_CONFIRM"),
                            L10nManager.Localize("UI_APREFILL_GUIDE_FORMAT",
                                L10nManager.Localize("GRIND_UI_BUTTON"), _inventoryApStoneCount),
                            L10nManager.Localize("UI_OK"),
                            L10nManager.Localize("UI_CANCEL"),
                            false, IconAndButtonSystem.SystemType.Information);
                        confirm.ConfirmCallback = () => chargeAp(true);
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
                    chargeAp(false);
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

        #endregion

        #region NPC Animation

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

        #endregion

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
