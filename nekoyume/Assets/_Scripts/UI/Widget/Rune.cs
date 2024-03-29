using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class Rune : Widget
    {
        [Serializable]
        private struct TryCountSlider
        {
            public GameObject container;
            public SweepSlider slider;
            public Button minusButton;
            public Button plusButton;
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField] [Header("LeftArea")]
        private RuneStoneEnhancementInventoryScroll scroll;

        [SerializeField] [Header("RightArea")]
        private TextMeshProUGUI runeNameText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private RuneOptionView runeOptionView;

        [SerializeField] [Header("CenterArea")]
        private GameObject requirement;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private GameObject successContainer;

        [SerializeField]
        private TextMeshProUGUI successRateText;

        [SerializeField]
        private List<RuneCostItem> costItems;

        [SerializeField]
        private GameObject costContainer;

        [SerializeField]
        private List<RuneEachCostItem> eachCostItems;

        [SerializeField]
        private TryCountSlider tryCountSlider;

        [SerializeField]
        private ConditionalButton levelUpButton;

        [SerializeField]
        private GameObject maxLevel;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [SerializeField]
        private List<GameObject> loadingObjects;

        [SerializeField]
        private Animator animator;

        private static readonly int HashToCombine = Animator.StringToHash("Combine");
        private static readonly int HashToLevelUp = Animator.StringToHash("LevelUp");
        private static readonly int HashToMaterialUse = Animator.StringToHash("MaterialUse");

        private readonly List<RuneItem> _runeItems = new();
        private readonly List<IDisposable> _disposables = new();

        private RuneItem _selectedRuneItem;
        private int _maxTryCount = 1;
        private int _currentRuneId = RuneFrontHelper.DefaultRuneId;

        private static readonly ReactiveProperty<int> TryCount = new();
        private readonly Dictionary<RuneCostType, RuneCostItem> _costItems = new();

        protected override void Awake()
        {
            base.Awake();
            foreach (var costItem in costItems)
            {
                _costItems.Add(costItem.CostType, costItem);
            }

            levelUpButton.OnSubmitSubject.Subscribe(_ => Enhancement()).AddTo(gameObject);
            levelUpButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                var message = _selectedRuneItem.Level > 0
                    ? L10nManager.Localize("UI_MESSAGE_NOT_ENOUGH_MATERIAL_2")  // Level Up
                    : L10nManager.Localize("UI_MESSAGE_NOT_ENOUGH_MATERIAL_1");  // Combine

                NotificationSystem.Push(MailType.System,
                    message,
                    NotificationCell.NotificationType.Alert);
            }).AddTo(gameObject);

            tryCountSlider.plusButton.onClick.AddListener(() =>
            {
                if (_maxTryCount <= 0)
                {
                    return;
                }
                TryCount.Value = Math.Min(_maxTryCount, TryCount.Value + 1);
                TryCount.Value = Math.Max(1, TryCount.Value);
            });
            tryCountSlider.minusButton.onClick.AddListener(() =>
            {
                TryCount.Value = Math.Max(1, TryCount.Value - 1);
            });
            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                base.Close(true);
                Find<CombinationMain>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            };
            LoadingHelper.RuneEnhancement
                         .Subscribe(b => loadingObjects.ForEach(x => x.SetActive(b)))
                         .AddTo(gameObject);
            TryCount.Subscribe(x =>
            {
                tryCountSlider.slider.ForceMove(x);
                _costItems[RuneCostType.RuneStone].UpdateCount(x);
                _costItems[RuneCostType.Ncg].UpdateCount(x);
                _costItems[RuneCostType.Crystal].UpdateCount(x);
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            SetInventory();
            base.Show(ignoreShowAnimation);
            Set(_selectedRuneItem);
        }

        public void Show(int runeId, bool ignoreShowAnimation = false)
        {
            _currentRuneId = runeId;
            _selectedRuneItem = null;

            SetInventory();
            base.Show(ignoreShowAnimation);
            Set(_selectedRuneItem);
        }

        public void OnActionRender(IRandom random, FungibleAssetValue fav)
        {
            Find<RuneEnhancementResultScreen>().Show(
                _selectedRuneItem,
                States.Instance.GoldBalanceState.Gold,
                States.Instance.CrystalBalance,
                TryCount.Value,
                random);

            States.Instance.UpdateRuneSlotState();
            _selectedRuneItem.RuneStone = fav;
            SetInventory();
            Set(_selectedRuneItem);
            animator.Play(_selectedRuneItem.Level > 1 ? HashToLevelUp : HashToCombine);
            LoadingHelper.RuneEnhancement.Value = false;
        }

        private void SetInventory()
        {
            _disposables.DisposeAllAndClear();
            _runeItems.Clear();

            var runeStates = States.Instance.RuneStates;
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            var items = new List<RuneStoneEnhancementInventoryItem>();
            foreach (var value in sheet.Values)
            {
                var state = runeStates.FirstOrDefault(x => x.RuneId == value.Id);
                var runeItem = new RuneItem(value, state?.Level ?? 0);
                if (_selectedRuneItem == null)
                {
                    if (runeItem.Row.Id == _currentRuneId)
                    {
                        _selectedRuneItem = runeItem;
                    }
                }
                else
                {
                    if (_selectedRuneItem.Row.Id == runeItem.Row.Id)
                    {
                        _selectedRuneItem = runeItem;
                    }
                }
                _runeItems.Add(runeItem);
                items.Add(new RuneStoneEnhancementInventoryItem(state, value, runeItem));
            }

            scroll.UpdateData(items.OrderBy(x => x.item.SortingOrder));
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
        }

        private void OnClickItem(RuneStoneEnhancementInventoryItem item)
        {
            _selectedRuneItem = null;
            Set(item.item);
        }

        private void Enhancement()
        {
            var runeId = _selectedRuneItem.Row.Id;
            Animator.Play(HashToMaterialUse);
            ActionManager.Instance.RuneEnhancement(runeId, TryCount.Value);
            LoadingHelper.RuneEnhancement.Value = true;
            if (RuneFrontHelper.TryGetRuneIcon(_selectedRuneItem.Row.Id, out var runeIcon))
            {
                var quote = L10nManager.Localize("UI_RUNE_COMBINE_START");
                Find<RuneCombineResultScreen>().Show(runeIcon, quote);
            }
        }

        private void Set(RuneItem item)
        {
            if (item is null)
            {
                return;
            }

            if (!RuneFrontHelper.TryGetRuneStoneIcon(item.Row.Id, out var runeStoneIcon))
            {
                return;
            }

            if (item.Cost is null && !item.IsMaxLevel)
            {
                return;
            }

            _selectedRuneItem = item;
            UpdateRuneItems(item);
            UpdateButtons(item);
            UpdateRuneOptions(item);
            UpdateCost(item, runeStoneIcon);
            UpdateHeaderMenu(runeStoneIcon, item.RuneStone);
            UpdateSlider(item);
            animator.Play(item.Level > 0 ? HashToLevelUp : HashToCombine);
            loadingText.text = item.Level > 0
                ? L10nManager.Localize("UI_RUNE_LEVEL_UP_PROCESSING")  // Level Up Processing
                : L10nManager.Localize("UI_RUNE_COMBINE_PROCESSING");  // Combine Processing

            successContainer.SetActive(!item.IsMaxLevel);
            costContainer.SetActive(!item.IsMaxLevel);
            TryCount.SetValueAndForceNotify(TryCount.Value);
        }

        private void UpdateRuneItems(RuneItem item)
        {
            if (!RuneFrontHelper.TryGetRuneIcon(item.Row.Id, out var runeIcon))
            {
                return;
            }

            runeImage.sprite = runeIcon;

            foreach (var runeItem in _runeItems)
            {
                runeItem.IsSelected.Value = runeItem.Row.Id == item.Row.Id;
            }
        }

        private void UpdateButtons(RuneItem item)
        {
            requirement.SetActive(item.HasNotification);
            maxLevel.SetActive(item.IsMaxLevel);

            levelUpButton.gameObject.SetActive(!item.IsMaxLevel);
            levelUpButton.Text = item.Level > 0
                ? L10nManager.Localize("UI_UPGRADE_EQUIPMENT")  // Level Up
                : L10nManager.Localize("UI_COMBINATION_ITEM");  // Combine
            levelUpButton.Interactable = item.HasNotification;
        }

        private void UpdateRuneOptions(RuneItem item)
        {
            runeNameText.text = L10nManager.Localize($"RUNE_NAME_{item.Row.Id}");
            gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{item.Row.Grade}");

            if (item.Level == 0)
            {
                if (item.OptionRow is null || !item.OptionRow.LevelOptionMap.TryGetValue(1, out var statInfo))
                {
                    return;
                }

                runeOptionView.Set(1, statInfo, (RuneUsePlace)item.Row.UsePlace);
            }
            else
            {
                if (!item.OptionRow.LevelOptionMap.TryGetValue(item.Level, out var statInfo))
                {
                    return;
                }

                var nextLevel = item.Level + 1;
                if (item.OptionRow.LevelOptionMap.TryGetValue(nextLevel, out var nextStatInfo))
                {
                    runeOptionView.Set(
                    item.Level,
                    nextLevel,
                    statInfo,
                    nextStatInfo,
                    (RuneUsePlace)item.Row.UsePlace);
                }
                else
                {
                    runeOptionView.Set(item.Level, statInfo, (RuneUsePlace)item.Row.UsePlace); // max level
                }
            }
        }

        private void UpdateCost(RuneItem item, Sprite runeStoneIcon)
        {
            if (item.Cost is null)
            {
                successRateText.text = String.Empty;
                eachCostItems.ForEach(x=> x.Set(0));
                _costItems[RuneCostType.RuneStone].Set(0, false, null);
                _costItems[RuneCostType.Crystal].Set(0, false, null);
                _costItems[RuneCostType.Ncg].Set(0, false, null);
                return;
            }

            successRateText.text = $"{item.Cost.LevelUpSuccessRate / 100}%";

            var popup = Find<MaterialNavigationPopup>();

            _costItems[RuneCostType.RuneStone].Set(
                item.Cost.RuneStoneQuantity,
                item.EnoughRuneStone,
                () => popup.ShowRuneStone(item.Row.Id),
                runeStoneIcon);

            _costItems[RuneCostType.Crystal].Set(
                item.Cost.CrystalQuantity,
                item.EnoughCrystal,
                () => popup.ShowCurrency(CostType.Crystal));

            _costItems[RuneCostType.Ncg].Set(
                item.Cost.NcgQuantity,
                item.EnoughNcg,
                () => popup.ShowCurrency(CostType.NCG));

            foreach (var costItem in eachCostItems)
            {
                switch(costItem.CostType)
                {
                    case RuneCostType.RuneStone:
                        costItem.Set(item.Cost.RuneStoneQuantity);
                        break;
                    case RuneCostType.Crystal:
                        costItem.Set(item.Cost.CrystalQuantity);
                        break;
                    case RuneCostType.Ncg:
                        costItem.Set(item.Cost.NcgQuantity);
                        break;
                }
            }
        }

        private void UpdateHeaderMenu(Sprite runeStoneIcon, FungibleAssetValue runeStone)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            var headerMenu = Find<HeaderMenuStatic>();
            headerMenu.UpdateAssets(HeaderMenuStatic.AssetVisibleState.RuneStone);
            headerMenu.RuneStone.SetRuneStone(
                runeStoneIcon, runeStone.GetQuantityString(), runeStone.Currency.Ticker);
        }

        private void UpdateSlider(RuneItem item)
        {
            if (item.Level == 0)
            {
                tryCountSlider.container.SetActive(false);
                TryCount.Value = 1;
            }
            else
            {
                if (item.Cost is null)
                {
                    tryCountSlider.container.SetActive(false);
                    return;
                }

                tryCountSlider.container.SetActive(true);
                var maxRuneStone = item.Cost.RuneStoneQuantity > 0
                    ? item.RuneStone.MajorUnit / item.Cost.RuneStoneQuantity
                    : -1;
                var maxCrystal = item.Cost.CrystalQuantity > 0
                    ? States.Instance.CrystalBalance.MajorUnit / item.Cost.CrystalQuantity
                    : -1;
                var maxNcg = item.Cost.NcgQuantity > 0
                    ? States.Instance.GoldBalanceState.Gold.MajorUnit / item.Cost.NcgQuantity
                    : -1;
                var maxValues = new List<BigInteger> { maxRuneStone, maxCrystal, maxNcg };
                var count = (int)maxValues.Where(x => x >= 0).Min();
                _maxTryCount = Math.Min(100, count);
                tryCountSlider.slider.Set(1,
                    _maxTryCount > 0 ? _maxTryCount : 1,
                    1,
                    _maxTryCount > 0 ? _maxTryCount : 1,
                    1,
                    x =>
                    {
                        TryCount.Value = x;
                        TryCount.Value = Math.Max(1, TryCount.Value);
                    },
                    _maxTryCount > 0,
                    true);
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationRuneCombineButton()
        {
            Enhancement();
        }
    }
}
