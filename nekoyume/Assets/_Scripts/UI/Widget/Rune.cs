using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
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
        [SerializeField]
        private RuneOptionView currentOptions;

        [SerializeField]
        private List<RuneCostItem> costItems;

        [SerializeField]
        private List<RuneEachCostItem> eachCostItems;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private TextMeshProUGUI runeNameText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private TextMeshProUGUI successRateText;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button combineButton;

        [SerializeField]
        private Button levelUpButton;

        [SerializeField]
        private Button plusButton;

        [SerializeField]
        private Button minusButton;

        [SerializeField]
        private Button disableCombineButton;

        [SerializeField]
        private Button disableLevelUpButton;

        [SerializeField]
        private Button informationButton;

        [SerializeField]
        private SweepSlider slider;

        [SerializeField]
        private List<GameObject> activeButtons;

        [SerializeField]
        private GameObject content;

        [SerializeField]
        private GameObject requirement;

        [SerializeField]
        private GameObject successContainer;

        [SerializeField]
        private GameObject costContainer;

        [SerializeField]
        private List<GameObject> loadingObjects;

        [SerializeField]
        private GameObject maxLevel;

        [SerializeField]
        private GameObject sliderContainer;

        [SerializeField]
        private RuneStoneEnhancementInventoryScroll scroll;

        [SerializeField]
        private Animator animator;

        private static readonly int HashToCombine =
            Animator.StringToHash("Combine");

        private static readonly int HashToLevelUp =
            Animator.StringToHash("LevelUp");

        private static readonly int HashToMaterialUse =
            Animator.StringToHash("MaterialUse");

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

            combineButton.onClick.AddListener(Enhancement);
            levelUpButton.onClick.AddListener(Enhancement);
            disableCombineButton.onClick.AddListener(() =>
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_MESSAGE_NOT_ENOUGH_MATERIAL_1"),
                    NotificationCell.NotificationType.Alert);
            });
            disableLevelUpButton.onClick.AddListener(() =>
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_MESSAGE_NOT_ENOUGH_MATERIAL_2"),
                    NotificationCell.NotificationType.Alert);
            });
            informationButton.onClick.AddListener(() =>
            {
                informationButton.gameObject.SetActive(false);
            });
            plusButton.onClick.AddListener(() =>
            {
                if (_maxTryCount <= 0)
                {
                    return;
                }
                TryCount.Value = Math.Min(_maxTryCount, TryCount.Value + 1);
                TryCount.Value = Math.Max(1, TryCount.Value);
            });
            minusButton.onClick.AddListener(() =>
            {
                TryCount.Value = Math.Max(1, TryCount.Value - 1);
            });
            closeButton.onClick.AddListener(() =>
            {
                base.Close(true);
                Find<CombinationMain>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
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
                slider.ForceMove(x);
                _costItems[RuneCostType.RuneStone].UpdateCount(x);
                _costItems[RuneCostType.Ncg].UpdateCount(x);
                _costItems[RuneCostType.Crystal].UpdateCount(x);
            }).AddTo(gameObject);
        }

        public void Show(bool ignoreShowAnimation = false)
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


        public async UniTaskVoid OnActionRender(IRandom random)
        {
            if (_selectedRuneItem.Level == 0)
            {
                if (!RuneFrontHelper.TryGetRuneIcon(_selectedRuneItem.Row.Id, out var runeIcon))
                {
                    return;
                }

                var quote = L10nManager.Localize("UI_RUNE_COMBINE_START");
                Find<RuneCombineResultScreen>().Show(runeIcon, quote);
            }
            else
            {
                Find<RuneEnhancementResultScreen>().Show(
                    _selectedRuneItem,
                    States.Instance.GoldBalanceState.Gold,
                    States.Instance.CrystalBalance,
                    TryCount.Value,
                    random);
            }

            States.Instance.UpdateRuneSlotState();

            var fav = await States.Instance.UpdateRuneStoneBalance(_selectedRuneItem.Row.Id);
            if (fav != null)
            {
                _selectedRuneItem.RuneStone = (FungibleAssetValue)fav;
            }

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
            List<RuneStoneEnhancementInventoryItem> items = new List<RuneStoneEnhancementInventoryItem>();
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
            StartCoroutine(CoShowLoadingScreen());
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
            content.SetActive(true);
            UpdateRuneItems(item);
            UpdateButtons(item);
            UpdateRuneOptions(item);
            UpdateCost(item, runeStoneIcon);
            UpdateHeaderMenu(runeStoneIcon, item.RuneStone);
            UpdateSlider(item);
            animator.Play(item.Level > 0 ? HashToLevelUp : HashToCombine);
            loadingText.text = item.Level > 0
                ? L10nManager.Localize($"UI_RUNE_LEVEL_UP_PROCESSING")
                : L10nManager.Localize($"UI_RUNE_COMBINE_PROCESSING");

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
            foreach (var b in activeButtons)
            {
                b.SetActive(item.HasNotification);
            }

            requirement.SetActive(item.HasNotification);
            maxLevel.SetActive(item.IsMaxLevel);

            disableCombineButton.gameObject.SetActive(!item.HasNotification);
            disableLevelUpButton.gameObject.SetActive(!item.HasNotification);

            if (item.IsMaxLevel)
            {
                combineButton.gameObject.SetActive(false);
                levelUpButton.gameObject.SetActive(false);
            }
            else
            {
                combineButton.gameObject.SetActive(item.Level == 0);
                levelUpButton.gameObject.SetActive(item.Level != 0);
            }
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

                currentOptions.Set(1, statInfo, (RuneUsePlace)item.Row.UsePlace);
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
                    currentOptions.Set(
                    item.Level,
                    nextLevel,
                    statInfo,
                    nextStatInfo,
                    (RuneUsePlace)item.Row.UsePlace);
                }
                else
                {
                    currentOptions.Set(item.Level, statInfo, (RuneUsePlace)item.Row.UsePlace); // max level
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
                sliderContainer.SetActive(false);
                TryCount.Value = 1;
            }
            else
            {
                if (item.Cost is null)
                {
                    sliderContainer.SetActive(false);
                    return;
                }

                sliderContainer.SetActive(true);
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
                slider.Set(1,
                    _maxTryCount > 0 ? _maxTryCount : 1,
                    1,
                    _maxTryCount > 0 ? _maxTryCount : 1,
                    1,
                    (x) =>
                    {
                        TryCount.Value = x;
                        TryCount.Value = Math.Max(1, TryCount.Value);
                    },
                    _maxTryCount > 0,
                    true);
            }
        }

        private IEnumerator CoShowLoadingScreen()
        {
            if (RuneFrontHelper.TryGetRuneIcon(_selectedRuneItem.Row.Id, out var runeIcon))
            {
                var loadingScreen = Find<CombinationLoadingScreen>();
                loadingScreen.Show();
                loadingScreen.SpeechBubbleWithItem.SetRune(runeIcon);
                loadingScreen.SetCloseAction(null);
                yield return new WaitForSeconds(.5f);

                var format = L10nManager.Localize("UI_COST_BLOCK");
                var quote = string.Format(format, 1);
                loadingScreen.AnimateNPC(CombinationLoadingScreen.SpeechBubbleItemType.Rune, quote);
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationRuneCombineButton()
        {
            Enhancement();
        }
    }
}
