using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;
using Vector3 = UnityEngine.Vector3;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class BuyView : ShopView
    {
        public enum BuyMode
        {
            Single,
            Multiple
        }

        [Serializable]
        public class ItemTypeFilter
        {
            public ItemSubTypeFilter typeFilter;  // for identifying
            public ToggleDropdown toggleDropdown;
            public ItemSubTypeFilter[] subTypeFilters;
            public List<ShopSortFilter> sortFilters;
        }

        [SerializeField]
        private CartView cartView;

        [SerializeField]
        private List<ItemTypeFilter> itemTypeFilters;

        [SerializeField]
        private Button sortButton;

        [SerializeField]
        private Button sortOrderButton;

        [SerializeField]
        private Button searchButton;

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private Button historyButton;

        [SerializeField]
        private Button showCartButton;

        [SerializeField]
        private UnityEngine.UI.Toggle levelLimitToggle;

        [SerializeField]
        private RectTransform sortOrderIcon;

        [SerializeField]
        private TMP_InputField inputField;

        [SerializeField]
        private Transform inputPlaceholder;

        [SerializeField]
        private GameObject loading;

        private int _loadingCount;

        private readonly List<ShopItem> _selectedItems = new();
        private readonly List<int> _itemIds = new();
        private readonly List<int> _runeIds = new();
        private readonly List<int> _petIds = new();
        private readonly int _hashNormal = Animator.StringToHash("Normal");
        private readonly int _hashDisabled = Animator.StringToHash("Disabled");
        private const int CartMaxCount = 20;

        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
            new(ShopSortFilter.CP);

        private readonly ReactiveProperty<bool> _useSearch = new();
        private readonly ReactiveProperty<bool> _isAscending = new();
        private readonly ReactiveProperty<bool> _levelLimit = new();

        private readonly ReactiveProperty<BuyMode> _mode = new(BuyMode.Single);

        private Action<List<ShopItem>> _onBuyMultiple;

        private Animator _resetAnimator;
        private TextMeshProUGUI _sortText;

        public bool IsFocused => inputField.isFocused;
        public bool IsCartEmpty => !_selectedItems.Any();

        public void ClearSelectedItems()
        {
            foreach (var model in _selectedItems)
            {
                model?.Selected.SetValueAndForceNotify(false);
            }

            _selectedItems.Clear();
            cartView.UpdateCart(_selectedItems, () => UpdateSelected(_selectedItems));
        }

        public void SetAction(Action<List<ShopItem>> onBuyMultiple)
        {
            _onBuyMultiple = onBuyMultiple;
        }

        protected override void OnAwake()
        {
            _resetAnimator = resetButton.GetComponent<Animator>();

            _sortText = sortButton.GetComponentInChildren<TextMeshProUGUI>();
            var tableSheets = Game.Game.instance.TableSheets;
            _itemIds.AddRange(tableSheets.EquipmentItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.ConsumableItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.CostumeItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.MaterialItemSheet.Values.Select(x => x.Id));
            _runeIds.AddRange(tableSheets.RuneListSheet.Values.Select(x => x.Id));
            _petIds.AddRange(tableSheets.PetSheet.Values.Select(x => x.Id));

            historyButton.onClick.AddListener(() =>
            {
                Widget.Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE",
                    "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
            });

            showCartButton.onClick.AddListener(() => { _mode.SetValueAndForceNotify(BuyMode.Multiple); });

            cartView.Set(() =>
                {
                    if (_selectedItems.Exists(x => x.Expired.Value))
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize("UI_SALE_PERIOD_HAS_EXPIRED"),
                            NotificationCell.NotificationType.Alert);
                    }
                    else
                    {
                        _onBuyMultiple?.Invoke(_selectedItems);
                    }
                },
                () =>
                {
                    if (_selectedItems.Any())
                    {
                        Widget.Find<TwoButtonSystem>().Show(
                            L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                            L10nManager.Localize("UI_YES"),
                            L10nManager.Localize("UI_NO"),
                            () => _mode.SetValueAndForceNotify(BuyMode.Single));
                    }
                    else
                    {
                        _mode.SetValueAndForceNotify(BuyMode.Single);
                    }
                });

            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(_ => UpdateView())
                .AddTo(gameObject);
        }

        protected override void InitInteractiveUI()
        {
            inputPlaceholder.SetAsLastSibling();

            foreach (var filter in itemTypeFilters)
            {
                var toggleDropdown = filter.toggleDropdown;
                var toggles = toggleDropdown.items;
                toggleDropdown.onClickToggle.AddListener(() =>
                {
                    _selectedSubTypeFilter.Value = filter.subTypeFilters.First();
                    _selectedSortFilter.Value = filter.sortFilters.First();

                    toggles.First().isOn = true;
                    ResetPage();
                });
                toggleDropdown.onClickToggle.AddListener(AudioController.PlayClick);

                foreach (var toggle in toggles)
                {
                    toggle.onClickToggle.AddListener(() =>
                    {
                        var toggleIndex = toggles.IndexOf(toggle);
                        _selectedSubTypeFilter.Value = filter.subTypeFilters[toggleIndex];
                        _selectedSortFilter.Value = filter.sortFilters.First();

                        ResetPage();
                    });
                    toggle.onClickToggle.AddListener(AudioController.PlayClick);
                }
            }

            sortButton.onClick.AddListener(() =>
            {
                var filter = itemTypeFilters.First(filter =>
                    filter.subTypeFilters.Contains(_selectedSubTypeFilter.Value));
                var sortFilters = filter.sortFilters;
                var index = sortFilters.IndexOf(_selectedSortFilter.Value);

                var nextIndex = (index + 1) % sortFilters.Count;
                _selectedSortFilter.Value = sortFilters[nextIndex];

                ResetPage();
            });

            inputField.onValueChanged.AddListener(_ =>
                searchButton.gameObject.SetActive(inputField.text.Length > 0));
            inputField.onSubmit.AddListener(_ =>
            {
                _useSearch.SetValueAndForceNotify(inputField.text.Length > 0);
                ResetPage();
            });
            searchButton.onClick.AddListener(() =>
            {
                _useSearch.SetValueAndForceNotify(inputField.text.Length > 0);
                ResetPage();
            });
            resetButton.onClick.AddListener(() =>
            {
                _useSearch.Value = !_useSearch.Value;
                ResetPage();
            });
            sortOrderButton.onClick.AddListener(() =>
            {
                _isAscending.Value = !_isAscending.Value;
                ResetPage();
            });
            levelLimitToggle.onValueChanged.AddListener(value =>
            {
                _levelLimit.Value = value;
                ResetPage();
            });
        }

        public void OnBuyProductAction()
        {
            ClearSelectedItems();
        }

        private async void ResetPage()
        {
            await SetItems(true);
            UpdateView();
            _page.SetValueAndForceNotify(0);
        }

        protected override async void UpdatePage(int page)
        {
            var limit = _column * _row;
            var cachedCount = ReactiveShopState.GetCachedBuyItemCount(_selectedSubTypeFilter.Value);
            if (limit * (page + 1) >= cachedCount)
            {
                await SetItems();
            }

            UpdateView();
            base.UpdatePage(page);
        }

        private int[] GetFilteredItemIds(ItemSubTypeFilter filter)
        {
            var avatarLevel = Game.Game.instance.States.CurrentAvatarState.level;
            var requirementSheet = Game.Game.instance.TableSheets.ItemRequirementSheet;

            if ((!_useSearch.Value && !_levelLimit.Value) ||
                filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone)
            {
                return Array.Empty<int>();
            }

            bool IsValid(int id)
            {
                var inSearch = !_useSearch.Value ||
                    Regex.IsMatch(L10nManager.LocalizeItemName(id), inputField.text, RegexOptions.IgnoreCase);
                var inLevelLimit = !_levelLimit.Value ||
                    (requirementSheet.TryGetValue(id, out var requirementRow) &&
                        avatarLevel >= requirementRow.Level);
                return inSearch && inLevelLimit;
            }

            return _itemIds.Where(IsValid).ToArray();
        }

        private string[] GetFilteredTicker(ItemSubTypeFilter filter)
        {
            if (!_useSearch.Value)
            {
                return new[] { filter == ItemSubTypeFilter.RuneStone ? "RUNE" : "SOULSTONE" };
            }

            var itemName = inputField.text;
            switch (filter)
            {
                case ItemSubTypeFilter.RuneStone:
                    var filteredRuneList = _runeIds.Where(id => Regex.IsMatch(L10nManager.LocalizeRuneName(id), itemName, RegexOptions.IgnoreCase)).ToList();
                    var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                    return filteredRuneList.Any()
                        ? filteredRuneList.Select(id => runeSheet[id].Ticker).ToArray()
                        : new[] { "RUNE" };

                case ItemSubTypeFilter.PetSoulStone:
                    var filteredPetList = _petIds.Where(id => Regex.IsMatch(L10nManager.LocalizePetName(id), itemName, RegexOptions.IgnoreCase)).ToList();
                    var petSheet = Game.Game.instance.TableSheets.PetSheet;
                    return filteredPetList.Any()
                        ? filteredPetList.Select(id => petSheet[id].SoulStoneTicker.ToUpper()).ToArray()
                        : new[] { "SOULSTONE" };
                default:
                    return new[] { "RUNE" };
            }
        }

        private async Task SetItems(bool reset = false)
        {
            if (!_isActive)
            {
                return;
            }

            var filter = _selectedSubTypeFilter.Value;
            var limit = _column * _row;
            var orderType = _selectedSortFilter.Value.ToMarketOrderType(_isAscending.Value);

            _loadingCount++;
            loading.SetActive(_loadingCount > 0);
            if (filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone)
            {
                var tickers = GetFilteredTicker(filter);
                await ReactiveShopState.RequestBuyFungibleAssetsAsync(
                    tickers, orderType, limit * 15, reset);
            }
            else
            {
                var filteredItemIds = GetFilteredItemIds(filter);
                await ReactiveShopState.RequestBuyProductsAsync(
                    filter, orderType, limit * 15, reset, filteredItemIds);
            }

            _loadingCount--;
            loading.SetActive(_loadingCount > 0);

            Set(ReactiveShopState.BuyItemProducts, ReactiveShopState.BuyFungibleAssetProducts);
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSortFilter.Subscribe(filter => { _sortText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}"); }).AddTo(gameObject);
            _useSearch.Subscribe(useSearch =>
            {
                resetButton.interactable = useSearch;
                _resetAnimator.Play(useSearch ? _hashNormal : _hashDisabled);
                if (!useSearch)
                {
                    inputField.text = string.Empty;
                }
            }).AddTo(gameObject);
            _isAscending.Subscribe(isAscending => { sortOrderIcon.localScale = new Vector3(1, isAscending ? 1 : -1, 1); }).AddTo(gameObject);

            _mode.Subscribe(x =>
            {
                ClearSelectedItems();
                switch (_mode.Value)
                {
                    case BuyMode.Single:
                        cartView.gameObject.SetActive(false);
                        break;
                    case BuyMode.Multiple:
                        cartView.gameObject.SetActive(true);
                        break;
                }
            }).AddTo(gameObject);
        }

        protected override void OnClickItem(ShopItem item)
        {
            switch (_mode.Value)
            {
                case BuyMode.Single:
                    if (_selectedItems.Any())
                    {
                        ClearSelectedItems();
                        if (item.ItemBase is not null)
                        {
                            if (_selectedItems.Exists(x =>
                                x.Product.ProductId.Equals(item.Product.ProductId)))
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (_selectedItems.Exists(x =>
                                x.FungibleAssetProduct.ProductId.Equals(item.FungibleAssetProduct.ProductId)))
                            {
                                break;
                            }
                        }
                    }

                    item.Selected.SetValueAndForceNotify(true);
                    _selectedItems.Add(item);
                    ClickItemAction?.Invoke(item); // Show tooltip popup

                    break;
                case BuyMode.Multiple:
                    cartView.gameObject.SetActive(true);

                    ShopItem selectedItem;
                    if (item.ItemBase is not null)
                    {
                        selectedItem = _selectedItems.FirstOrDefault(x =>
                            x.Product.ProductId.Equals(item.Product.ProductId));
                    }
                    else
                    {
                        selectedItem = _selectedItems.FirstOrDefault(x =>
                            item.FungibleAssetProduct.ProductId.Equals(x.FungibleAssetProduct.ProductId));
                    }

                    if (selectedItem == null)
                    {
                        if (item.Expired.Value)
                        {
                            OneLineSystem.Push(MailType.System,
                                L10nManager.Localize("UI_SALE_PERIOD_HAS_EXPIRED"),
                                NotificationCell.NotificationType.Alert);
                            return;
                        }

                        if (_selectedItems.Count < CartMaxCount)
                        {
                            item.Selected.SetValueAndForceNotify(true);
                            _selectedItems.Add(item);
                        }
                        else
                        {
                            OneLineSystem.Push(MailType.System,
                                L10nManager.Localize("NOTIFICATION_BUY_WISHLIST_FULL"),
                                NotificationCell.NotificationType.Alert);
                        }
                    }
                    else
                    {
                        selectedItem.Selected.SetValueAndForceNotify(false);
                        _selectedItems.Remove(selectedItem);
                    }

                    cartView.UpdateCart(_selectedItems, () => UpdateSelected(_selectedItems));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateSelected(_selectedItems);
        }

        protected override void Reset()
        {
            _loadingCount = 0;
            loading.SetActive(_loadingCount > 0);

            var firstFilter = itemTypeFilters.First();
            cartView.gameObject.SetActive(false);
            firstFilter.toggleDropdown.isOn = true;
            firstFilter.toggleDropdown.items.First().isOn = true;
            inputField.text = string.Empty;
            resetButton.interactable = false;
            levelLimitToggle.isOn = false;
            if (_resetAnimator.isActiveAndEnabled)
            {
                _resetAnimator.Play(_hashDisabled);
            }

            _page.SetValueAndForceNotify(0);
            _selectedSubTypeFilter.SetValueAndForceNotify(ItemSubTypeFilter.Weapon);
            _selectedSortFilter.SetValueAndForceNotify(ShopSortFilter.CP);
            _useSearch.SetValueAndForceNotify(false);
            _isAscending.SetValueAndForceNotify(false);
            _levelLimit.SetValueAndForceNotify(false);
            _mode.SetValueAndForceNotify(BuyMode.Single);

            ClearSelectedItems();
        }

        protected override IEnumerable<ShopItem> GetSortedModels(List<ShopItem> items)
        {
            // to check mimir equipment level
            return _levelLimit.Value
                ? items.Where(item => Util.IsUsableItem(item.ItemBase))
                : items;
        }

        protected override void UpdateView()
        {
            base.UpdateView();

            var expiredItems = _selectedItems.Where(x => x.Expired.Value).ToList();
            foreach (var item in expiredItems)
            {
                _selectedItems.Remove(item);
            }

            switch (_mode.Value)
            {
                case BuyMode.Single:
                    break;
                case BuyMode.Multiple:
                    cartView.UpdateCart(_selectedItems, () => UpdateSelected(_selectedItems));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateSelected(_selectedItems);
        }
    }
}
