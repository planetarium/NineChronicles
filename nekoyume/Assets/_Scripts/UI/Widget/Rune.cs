using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
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
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class Rune : Widget
    {
        [SerializeField]
        private List<RuneCostItem> costItems;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private TextMeshProUGUI successRateText;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button openButton;

        [SerializeField]
        private Button onceButton;

        [SerializeField]
        private Button repeatButton;

        [SerializeField]
        private List<GameObject> activeButtons;

        [SerializeField]
        private List<GameObject> disableButtons;

        [SerializeField]
        private GameObject content;

        [SerializeField]
        private GameObject guide;

        [SerializeField]
        private GameObject loading;

        [SerializeField]
        private GameObject maxLevel;


        [SerializeField]
        private RuneListScroll scroll;

        [SerializeField]
        private Animator animator;

        private static readonly int HashToOpen =
            Animator.StringToHash("Open");

        private static readonly int HashToEnhancement =
            Animator.StringToHash("Enhancement");

        private static readonly int HashToIdle =
            Animator.StringToHash("Idle");

        private readonly Dictionary<int, List<RuneItem>> _runeItems = new();
        private readonly List<IDisposable> _disposables = new();

        private RuneItem _selectedRuneItem;
        private static readonly ReactiveProperty<bool> IsLoading = new();
        private readonly Dictionary<RuneCostType, RuneCostItem> _costItems = new();

        protected override void Awake()
        {
            base.Awake();
            openButton.onClick.AddListener(() => Enhancement(_selectedRuneItem.Row.Id, true, HashToOpen));
            onceButton.onClick.AddListener(() => Enhancement(_selectedRuneItem.Row.Id, true, HashToEnhancement));
            repeatButton.onClick.AddListener(() =>
            {
                var t = L10nManager.Localize("UI_REPEAT");
                var c = L10nManager.Localize("UI_REPEAT_LEVEL_UP_INFO");
                Find<TwoButtonSystem>().Show(
                    $"<size=140%>{t}</size>\n\n{c}",
                    L10nManager.Localize("UI_CONFIRM"),
                    L10nManager.Localize("UI_CANCEL"),
                    (() => Enhancement(_selectedRuneItem.Row.Id, false, HashToEnhancement)));
            });

            closeButton.onClick.AddListener(() => Close(true));
            CloseWidget = () => Close(true);
            IsLoading.Subscribe(b => loading.SetActive(b)).AddTo(gameObject);

            foreach (var costItem in costItems)
            {
                _costItems.Add(costItem.CostType, costItem);
            }
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

            if (!IsLoading.Value)
            {
                Reset();
            }

            await SetInventory();
            loading.Close();
            base.Show(ignoreShowAnimation);
        }

        public async UniTaskVoid OnActionRender()
        {
            var fav = await States.Instance.SetRuneStoneBalance(_selectedRuneItem.Row.Id);
            if (fav != null)
            {
                _selectedRuneItem.RuneStone = (FungibleAssetValue)fav;
            }
            await SetInventory();
            Set(_selectedRuneItem);
            animator.Play(HashToIdle);
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
                if (_selectedRuneItem != null && _selectedRuneItem.Row.Id == runeItem.Row.Id)
                {
                    _selectedRuneItem = runeItem;
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

        private void Enhancement(int runeId, bool once, int animationHash)
        {
            Animator.Play(animationHash);
            ActionManager.Instance.RuneEnhancement(runeId, once);
            IsLoading.Value = true;
        }

        private void Reset()
        {
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.CurrencyOnly);
            guide.SetActive(true);
            content.SetActive(false);
            runeImage.sprite = null;
            successRateText.text = string.Empty;
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

            guide.SetActive(false);
            content.SetActive(true);
            UpdateRuneItems(item);
            UpdateButtons(item);
            UpdateCost(item, runeIcon, runeStoneIcon);
            UpdateHeaderMenu(runeStoneIcon, item.RuneStone);
            Debug.Log($"[Rune.UpdateInfo] DONE");
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
            openButton.gameObject.SetActive(item.Level == 0);
            onceButton.gameObject.SetActive(item.Level != 0);
            repeatButton.gameObject.SetActive(item.Level != 0);
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
