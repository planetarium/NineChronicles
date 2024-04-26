using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        [Serializable]
        private struct RuneLevelBonus
        {
            public TextMeshProUGUI bonusText;
            public RuneLevelBonusDiff reward;
            public Button infoButton;
        }

        [Serializable]
        private struct RuneLevelBonusDiff
        {
            public GameObject arrow;
            public TextMeshProUGUI currentText;
            public TextMeshProUGUI nextText;

            private int _runeId;
            private int _startLevel;

            public void Set(bool canEnhancement, int runeId, int startLevel)
            {
                _runeId = runeId;
                _startLevel = startLevel;

                arrow.SetActive(canEnhancement);
                nextText.gameObject.SetActive(canEnhancement);
            }

            public void UpdateTryCount(int tryCount)
            {
                var allRuneState = States.Instance.AllRuneState;
                if (allRuneState is null)
                {
                    return;
                }

                var nextReward = RuneFrontHelper.CalculateRuneLevelBonusReward(
                    allRuneState,
                    Game.Game.instance.TableSheets.RuneListSheet,
                    Game.Game.instance.TableSheets.RuneLevelBonusSheet,
                    (_runeId, _startLevel + tryCount));

                nextText.text = $"+{nextReward / 1000m:0.###}%";
            }
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField] [Header("LeftArea")]
        private RuneLevelBonus runeLevelBonus;

        [SerializeField]
        private RuneStoneEnhancementInventoryScroll scroll;

        [SerializeField] [Header("RightArea")]
        private TextMeshProUGUI runeNameText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private TextMeshProUGUI levelBonusCoef;

        [SerializeField]
        private RuneOptionView runeOptionView;

        [SerializeField] [Header("CenterArea")]
        private GameObject requirement;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private List<RuneCostItem> costItems;

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
        private decimal _runeLevelBonus;
        private int _maxTryCount = 1;
        private int _currentRuneId = RuneFrontHelper.DefaultRuneId;

        private static readonly ReactiveProperty<int> TryCount = new();
        private readonly Dictionary<RuneCostType, RuneCostItem> _costItems = new();

        private static string TutorialCheckKey =>
            $"Tutorial_Check_Rune_{Game.Game.instance.States.CurrentAvatarKey}";

        protected override void Awake()
        {
            base.Awake();
            foreach (var costItem in costItems)
            {
                _costItems.Add(costItem.CostType, costItem);
            }

            runeLevelBonus.infoButton.onClick.AddListener(() =>
                Find<RuneLevelBonusEffectPopup>().Show(_runeLevelBonus));
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
                if (_maxTryCount < 1)
                {
                    return;
                }

                TryCount.Value = Math.Min(TryCount.Value + 1, _maxTryCount);
            });
            tryCountSlider.minusButton.onClick.AddListener(() =>
            {
                TryCount.Value = Math.Max(TryCount.Value - 1, 1);
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
                runeOptionView.UpdateTryCount(x);
                runeLevelBonus.reward.UpdateTryCount(x);
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            SetRuneLevelBonus();
            SetInventory();
            base.Show(ignoreShowAnimation);
            Set(_selectedRuneItem);

            if (_runeLevelBonus > 0 &&
                PlayerPrefs.GetInt(TutorialCheckKey, 0) == 0)
            {
                // Play Tutorial - Rune Level Bonus (for old user)
                Game.Game.instance.Stage.TutorialController.Play(231000);
                PlayerPrefs.SetInt(TutorialCheckKey, 1);
            }
        }

        public void Show(int runeId, bool ignoreShowAnimation = false)
        {
            _currentRuneId = runeId;
            _selectedRuneItem = null;

            Show(ignoreShowAnimation);
        }

        public void OnActionRender(
            IRandom random,
            FungibleAssetValue fav,
            (int previousCp, int currentCp) cp)
        {
            Find<RuneEnhancementResultScreen>().Show(
                _selectedRuneItem,
                TryCount.Value,
                random,
                cp);

            States.Instance.UpdateRuneSlotState();
            _selectedRuneItem.RuneStone = fav;
            SetRuneLevelBonus();
            SetInventory();
            Set(_selectedRuneItem);
            animator.Play(_selectedRuneItem.Level > 1 ? HashToLevelUp : HashToCombine);
            LoadingHelper.RuneEnhancement.Value = false;
        }

        private void SetInventory()
        {
            _disposables.DisposeAllAndClear();
            _runeItems.Clear();

            var allRuneState = States.Instance.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var items = new List<RuneStoneEnhancementInventoryItem>();
            foreach (var runeRow in runeListSheet)
            {
                var runeLevel = allRuneState.TryGetRuneState(runeRow.Id, out var runeState)
                    ? runeState.Level
                    : 0;

                var runeItem = new RuneItem(runeRow, runeLevel);
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
                items.Add(new RuneStoneEnhancementInventoryItem(runeState, runeRow, runeItem));
            }

            scroll.UpdateData(items.OrderBy(x => x.item.SortingOrder));
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
        }

        private void OnClickItem(RuneStoneEnhancementInventoryItem item)
        {
            _selectedRuneItem = null;
            Set(item.item);
        }

        private void SetRuneLevelBonus()
        {
            var bonus = RuneFrontHelper.CalculateRuneLevelBonus(
                States.Instance.AllRuneState,
                Game.Game.instance.TableSheets.RuneListSheet);
            var reward = RuneFrontHelper.CalculateRuneLevelBonusReward(
                bonus,
                Game.Game.instance.TableSheets.RuneLevelBonusSheet);
            runeLevelBonus.bonusText.text = bonus.ToString();
            runeLevelBonus.reward.currentText.text = $"+{reward / 1000m:0.###}%";

            _runeLevelBonus = bonus;
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

            if (item.CostRow is null && !item.IsMaxLevel)
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
            levelBonusCoef.text = item.Row.BonusCoef.ToString();

            runeOptionView.Set(item.OptionRow, item.Level, (RuneUsePlace)item.Row.UsePlace);
            runeLevelBonus.reward.Set(
                item.Level < item.CostRow.Cost.Count,
                _selectedRuneItem.Row.Id, _selectedRuneItem.Level);
        }

        private void UpdateCost(RuneItem item, Sprite runeStoneIcon)
        {
            if (item.CostRow is null)
            {
                _costItems[RuneCostType.RuneStone].Set(null, 0, false, null);
                _costItems[RuneCostType.Crystal].Set(null, 0, false, null);
                _costItems[RuneCostType.Ncg].Set(null, 0, false, null);
                return;
            }

            var popup = Find<MaterialNavigationPopup>();

            _costItems[RuneCostType.RuneStone].Set(
                item.CostRow,
                item.Level,
                item.EnoughRuneStone,
                () => popup.ShowRuneStone(item.Row.Id),
                runeStoneIcon);

            _costItems[RuneCostType.Crystal].Set(
                item.CostRow,
                item.Level,
                item.EnoughCrystal,
                () => popup.ShowCurrency(CostType.Crystal));

            _costItems[RuneCostType.Ncg].Set(
                item.CostRow,
                item.Level,
                item.EnoughNcg,
                () => popup.ShowCurrency(CostType.NCG));
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
                if (item.CostRow is null)
                {
                    tryCountSlider.container.SetActive(false);
                    return;
                }

                tryCountSlider.container.SetActive(true);

                _maxTryCount = item.CostRow.GetMaxTryCount(item.Level, (
                    States.Instance.GoldBalanceState.Gold,
                    States.Instance.CrystalBalance,
                    item.RuneStone), 30);

                var sliderMaxValue = _maxTryCount > 0 ? _maxTryCount : 1;
                tryCountSlider.slider.Set(
                    1, sliderMaxValue,
                    1, sliderMaxValue,
                    1, x => TryCount.Value = Math.Clamp(x, 1, sliderMaxValue),
                    _maxTryCount > 0, true);
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationRuneCombineButton()
        {
            Enhancement();
            // Rune Level Bonus (for new user)
            PlayerPrefs.SetInt(TutorialCheckKey, 1);
        }
    }
}
