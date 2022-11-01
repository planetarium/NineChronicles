using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class Rune : Widget
    {
        private const int defaultRuneId = 311001;

        [SerializeField]
        private List<RuneCostItem> costItems;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private TextMeshProUGUI successRateText;

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
        private SweepSlider slider;

        [SerializeField]
        private List<GameObject> activeButtons;

        [SerializeField]
        private List<GameObject> disableButtons;

        [SerializeField]
        private GameObject content;

        [SerializeField]
        private List<GameObject> loadingObjects;

        [SerializeField]
        private GameObject maxLevel;

        [SerializeField]
        private RuneListScroll scroll;

        [SerializeField]
        private Animator animator;

        private static readonly int HashToCombine =
            Animator.StringToHash("Combine");

        private static readonly int HashToLevelUp =
            Animator.StringToHash("LevelUp");

        private static readonly int HashToCombineLoop =
            Animator.StringToHash("CombineLoop");

        private static readonly int HashToLevelUpLoop =
            Animator.StringToHash("LevelUpLoop");

        private static readonly int HashToMaterialUse =
            Animator.StringToHash("MaterialUse");

        private readonly Dictionary<int, List<RuneItem>> _runeItems = new();
        private readonly List<IDisposable> _disposables = new();

        private RuneItem _selectedRuneItem;
        private int _maxTryCount = 1;
        private static readonly ReactiveProperty<bool> IsLoading = new();
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
            plusButton.onClick.AddListener(() => TryCount.Value = math.min(_maxTryCount, TryCount.Value + 1));
            minusButton.onClick.AddListener(() => TryCount.Value = math.max(1, TryCount.Value - 1));
            closeButton.onClick.AddListener(() => Close(true));
            CloseWidget = () => Close(true);
            IsLoading.Subscribe(b => loadingObjects.ForEach(x => x.SetActive(b)))
                     .AddTo(gameObject);
            TryCount.Subscribe(x =>
            {
                slider.ForceMove(x);
                _costItems[RuneCostType.RuneStone].UpdateCount(x);
                _costItems[RuneCostType.Ncg].UpdateCount(x);
                _costItems[RuneCostType.Crystal].UpdateCount(x);
            }).AddTo(gameObject);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            Find<CombinationMain>().Show();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
        }

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<DataLoadingScreen>();
            loading.Show();
            await SetInventory();
            base.Show(ignoreShowAnimation);
            Set(_selectedRuneItem);
            loading.Close();
        }

        public async UniTaskVoid OnActionRender(IRandom random)
        {
            Find<RuneEnhancementResultScreen>().Show(
                _selectedRuneItem,
                States.Instance.GoldBalanceState.Gold,
                States.Instance.CrystalBalance,
                TryCount.Value,
                random);

            var fav = await States.Instance.SetRuneStoneBalance(_selectedRuneItem.Row.Id);
            if (fav != null)
            {
                _selectedRuneItem.RuneStone = (FungibleAssetValue)fav;
            }
            await SetInventory();
            Set(_selectedRuneItem);
            animator.Play(_selectedRuneItem.Level > 1 ? HashToLevelUp : HashToCombine);
            IsLoading.Value = false;

        }

        private async Task SetInventory()
        {
            _disposables.DisposeAllAndClear();
            _runeItems.Clear();

            var runeStates = await GetRuneStatesAsync();
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            foreach (var value in sheet.Values)
            {
                var groupId = RuneFrontHelper.GetGroupId(value.Id);
                if (!_runeItems.ContainsKey(groupId))
                {
                    _runeItems.Add(groupId, new List<RuneItem>());
                }

                var state = runeStates.FirstOrDefault(x => x.RuneId == value.Id);
                var runeItem = new RuneItem(value, state?.Level ?? 0);
                if (_selectedRuneItem == null)
                {
                    if (runeItem.Row.Id == defaultRuneId)
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

                _runeItems[groupId].Add(runeItem);
            }

            var items = _runeItems.Select(p
                => new RuneListItem(RuneFrontHelper.GetGroupName(p.Key), p.Value)).ToList();
            scroll.UpdateData(items);
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
        }

        private async Task<List<RuneState>> GetRuneStatesAsync()
        {
            var listSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var runeIds = listSheet.Values.Select(x => x.Id).ToList();
            var runeAddresses = runeIds.Select(id => RuneState.DeriveAddress(avatarAddress, id)).ToList();
            var stateBulk = await Game.Game.instance.Agent.GetStateBulk(runeAddresses);
            var runeStates = new List<RuneState>();
            foreach (var value in stateBulk.Values)
            {
                if (value is List list)
                {
                    runeStates.Add(new RuneState(list));
                }
            }

            return runeStates;
        }

        private void OnClickItem(RuneItem item)
        {
            _selectedRuneItem = item;
            Set(item);
        }

        private void Enhancement()
        {
            var runeId = _selectedRuneItem.Row.Id;
            Animator.Play(HashToMaterialUse);
            ActionManager.Instance.RuneEnhancement(runeId, TryCount.Value);
            IsLoading.Value = true;
        }

        private void Set(RuneItem item)
        {
            if (item is null)
            {
                return;
            }

            if (!RuneFrontHelper.TryGetRuneIcon(item.Row.Id, out var runeIcon))
            {
                return;
            }

            if (item.Cost is null)
            {
                return;
            }

            if (!RuneFrontHelper.TryGetRuneStoneIcon(item.Cost.RuneStoneId, out var runeStoneIcon))
            {
                return;
            }

            content.SetActive(true);
            UpdateRuneItems(item);
            UpdateButtons(item);
            UpdateCost(item, runeIcon, runeStoneIcon);
            UpdateHeaderMenu(runeStoneIcon, item.RuneStone);
            UpdateSlider(item);
            animator.Play(item.Level > 0 ? HashToLevelUp : HashToCombine);
        }

        private void UpdateRuneItems(RuneItem item)
        {
            var items = new List<RuneItem>();
            foreach (var list in _runeItems.Values)
            {
                items.AddRange(list);
            }

            foreach (var runeItem in items)
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

            foreach (var b in disableButtons)
            {
                b.SetActive(!item.HasNotification);
            }

            maxLevel.SetActive(item.IsMaxLevel);
            combineButton.gameObject.SetActive(item.Level == 0);
            levelUpButton.gameObject.SetActive(item.Level != 0);
        }

        private void UpdateCost(RuneItem item, Sprite runeIcon, Sprite runeStoneIcon)
        {
            runeImage.sprite = runeIcon;
            successRateText.text = $"{item.Cost.LevelUpSuccessRate / 100}%";

            _costItems[RuneCostType.RuneStone].Set(
                item.Cost.RuneStoneQuantity,
                item.EnoughRuneStone,
                () => ShowMaterialNavigatorPopup(RuneCostType.RuneStone, item, runeStoneIcon),
                runeStoneIcon);

            _costItems[RuneCostType.Crystal].Set(
                item.Cost.CrystalQuantity,
                item.EnoughCrystal,
                () => ShowMaterialNavigatorPopup(RuneCostType.Crystal, item, _costItems[RuneCostType.Crystal].Icon));

            _costItems[RuneCostType.Ncg].Set(
                item.Cost.NcgQuantity,
                item.EnoughNcg,
                () => ShowMaterialNavigatorPopup(RuneCostType.Ncg, item, _costItems[RuneCostType.Ncg].Icon));
        }

        private static void UpdateHeaderMenu(Sprite runeStoneIcon, FungibleAssetValue runeStone)
        {
            var headerMenu = Find<HeaderMenuStatic>();
            headerMenu.UpdateAssets(HeaderMenuStatic.AssetVisibleState.RuneStone);
            headerMenu.RuneStone.SetRuneStone(runeStoneIcon, runeStone.GetQuantityString());
        }

        private void UpdateSlider(RuneItem item)
        {
            var maxRuneStone = item.Cost.RuneStoneQuantity > 0
                ? (int)item.RuneStone.MajorUnit / item.Cost.RuneStoneQuantity
                : -1;
            var maxCrystal = item.Cost.CrystalQuantity > 0
                ? (int)States.Instance.CrystalBalance.MajorUnit / item.Cost.CrystalQuantity
                : -1;
            var maxNcg = item.Cost.NcgQuantity > 0
                ? (int)States.Instance.GoldBalanceState.Gold.MajorUnit / item.Cost.NcgQuantity
                : -1;
            var maxValues = new List<int> { maxRuneStone, maxCrystal, maxNcg };
            _maxTryCount = maxValues.Where(x => x >= 0).Min();
            slider.Set(1,
                _maxTryCount > 0 ? _maxTryCount : 1,
                1,
                _maxTryCount > 0 ? _maxTryCount : 1,
                1,
                (x) => TryCount.Value = x,
                _maxTryCount > 0);
            Debug.Log($"rune:{maxRuneStone} / crystal:{maxCrystal} / ncg:{maxNcg}");
        }

        private void ShowMaterialNavigatorPopup(
            RuneCostType costType,
            RuneItem item, Sprite icon)
        {
            var popup = Find<MaterialNavigationPopup>();
            string name, count, content, buttonText;
            System.Action callback;
            switch (costType)
            {
                case RuneCostType.RuneStone:
                    var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                    var runeStoneId = item.Cost.RuneStoneId;
                    var (info, canObtain) = RuneFrontHelper.GetRunStoneInformation(currentBlockIndex, runeStoneId);
                    name = L10nManager.Localize($"ITEM_NAME_{runeStoneId}");
                    count = States.Instance.RuneStoneBalance[runeStoneId].GetQuantityString();
                    content = L10nManager.Localize($"ITEM_DESCRIPTION_{runeStoneId}");
                    buttonText = canObtain
                        ? L10nManager.Localize("UI_MAIN_MENU_WORLDBOSS")
                        : L10nManager.Localize("UI_SHOP");
                    popup.SetInfo((info, canObtain));
                    callback = () =>
                    {
                        base.Close(true);
                        if (canObtain)
                        {
                            Find<WorldBoss>().ShowAsync().Forget();
                        }
                        else
                        {
                            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                            Find<ShopBuy>().Show();
                        }
                    };
                    break;
                case RuneCostType.Crystal:
                    name = L10nManager.Localize("ITEM_NAME_9999998");
                    count = States.Instance.CrystalBalance.GetQuantityString();
                    content = L10nManager.Localize("ITEM_DESCRIPTION_9999998");
                    buttonText = L10nManager.Localize("UI_MAIN_MENU_STAKING");
                    callback = () =>
                    {
                        base.Close(true);
                        Game.Event.OnRoomEnter.Invoke(true);
                        Find<StakingPopup>().Show();
                    };
                    break;
                case RuneCostType.Ncg:
                    name = L10nManager.Localize("ITEM_NAME_9999999");
                    count = States.Instance.GoldBalanceState.Gold.GetQuantityString();
                    content = L10nManager.Localize("ITEM_DESCRIPTION_9999999");
                    buttonText = L10nManager.Localize("UI_SHOP");
                    callback = () =>
                    {
                        base.Close(true);
                        Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        Find<ShopBuy>().Show();
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(costType), costType, null);
            }

            popup.Show(callback, icon, name, count, content, buttonText);
        }
    }
}
