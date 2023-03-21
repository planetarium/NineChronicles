using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;
using Toggle = UnityEngine.UI.Toggle;
using Vector3 = UnityEngine.Vector3;

namespace Nekoyume
{
    using UniRx;

    public class BuyView : ShopView
    {
        public enum BuyMode
        {
            Single,
            Multiple,
        }

        [SerializeField]
        private CartView cartView;

        [SerializeField]
        private List<ToggleDropdown> toggleDropdowns = new();

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
        private Toggle levelLimitToggle;

        [SerializeField]
        private RectTransform sortOrderIcon = null;

        [SerializeField]
        private TMP_InputField inputField = null;

        [SerializeField]
        private Transform inputPlaceholder = null;

        [SerializeField]
        private GameObject loading;

        private readonly List<ItemSubTypeFilter> _toggleTypes = new()
        {
            ItemSubTypeFilter.Equipment,
            ItemSubTypeFilter.Food,
            ItemSubTypeFilter.Materials,
            ItemSubTypeFilter.Costume,
            ItemSubTypeFilter.Stones,
        };

        private readonly Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>> _toggleSubTypes =
            new()
            {
                {
                    ItemSubTypeFilter.Equipment, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.Weapon,
                        ItemSubTypeFilter.Armor,
                        ItemSubTypeFilter.Belt,
                        ItemSubTypeFilter.Necklace,
                        ItemSubTypeFilter.Ring,
                    }
                },
                {
                    ItemSubTypeFilter.Food, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.Food_HP,
                        ItemSubTypeFilter.Food_ATK,
                        ItemSubTypeFilter.Food_DEF,
                        ItemSubTypeFilter.Food_CRI,
                        ItemSubTypeFilter.Food_HIT,
                    }
                },
                {
                    ItemSubTypeFilter.Materials, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.Hourglass,
                        ItemSubTypeFilter.ApStone,
                    }
                },
                {
                    ItemSubTypeFilter.Costume, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.FullCostume,
                        ItemSubTypeFilter.HairCostume,
                        ItemSubTypeFilter.EarCostume,
                        ItemSubTypeFilter.EyeCostume,
                        ItemSubTypeFilter.TailCostume,
                        ItemSubTypeFilter.Title,
                    }
                },
                {
                    ItemSubTypeFilter.Stones, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.RuneStone,
                        ItemSubTypeFilter.PetSoulStone,
                    }
                },
            };

        private readonly List<ShopItem> _selectedItems = new();
        private readonly List<int> _itemIds = new List<int>();
        private readonly int _hashNormal = Animator.StringToHash("Normal");
        private readonly int _hashDisabled = Animator.StringToHash("Disabled");
        private const int CartMaxCount = 20;

        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
            new(ShopSortFilter.CP);

        private readonly ReactiveProperty<List<int>> _selectedItemIds = new(new List<int>());

        private readonly ReactiveProperty<bool> _isAscending = new();
        private readonly ReactiveProperty<bool> _levelLimit = new();

        private readonly ReactiveProperty<BuyMode> _mode = new(BuyMode.Single);

        private Action<List<ShopItem>> _onBuyMultiple;

        private Animator _sortAnimator;
        private Animator _sortOrderAnimator;
        private Animator _levelLimitAnimator;
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
            _sortAnimator = sortButton.GetComponent<Animator>();
            _sortOrderAnimator = sortOrderButton.GetComponent<Animator>();
            _levelLimitAnimator = levelLimitToggle.GetComponent<Animator>();
            _resetAnimator = resetButton.GetComponent<Animator>();
            loading.SetActive(false);

            _sortText = sortButton.GetComponentInChildren<TextMeshProUGUI>();
            var tableSheets = Game.Game.instance.TableSheets;
            _itemIds.AddRange(tableSheets.EquipmentItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.ConsumableItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.CostumeItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.MaterialItemSheet.Values.Select(x => x.Id));

            historyButton.onClick.AddListener(() =>
            {
                Widget.Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE",
                    "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
            });

            showCartButton.onClick.AddListener(() =>
            {
                _mode.SetValueAndForceNotify(BuyMode.Multiple);
            });

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

            foreach (var toggleDropdown in toggleDropdowns)
            {
                var index = toggleDropdowns.IndexOf(toggleDropdown);
                var toggleType = _toggleTypes[index];
                toggleDropdown.onValueChanged.AddListener((value) =>
                {
                    if (!value)
                    {
                        return;
                    }

                    _selectedSubTypeFilter.Value = _toggleSubTypes[toggleType].First();
                    _selectedSortFilter.Value = _selectedSubTypeFilter.Value
                        is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone
                        ? ShopSortFilter.Price : ShopSortFilter.CP;
                    toggleDropdown.items.First().isOn = true;
                });
                toggleDropdown.onClickToggle.AddListener(AudioController.PlayClick);

                var subItems = toggleDropdown.items;

                foreach (var item in subItems)
                {
                    var subIndex = subItems.IndexOf(item);
                    var subTypes = _toggleSubTypes[toggleType];
                    var subToggleType = subTypes[subIndex];
                    item.onValueChanged.AddListener((value) =>
                    {
                        if (!value)
                        {
                            return;
                        }

                        _selectedSortFilter.Value = subToggleType
                            is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone
                            ? ShopSortFilter.Price : ShopSortFilter.CP;
                        _selectedSubTypeFilter.Value = subToggleType;
                    });
                    item.onClickToggle.AddListener(AudioController.PlayClick);
                }
            }

            sortButton.onClick.AddListener(() =>
            {
                var count = Enum.GetNames(typeof(ShopSortFilter)).Length;
                switch (_selectedSubTypeFilter.Value)
                {
                    case ItemSubTypeFilter.RuneStone:
                    case ItemSubTypeFilter.PetSoulStone:
                        _selectedSortFilter.Value = ShopSortFilter.Price;
                        break;

                    default:
                        _selectedSortFilter.Value = (int)_selectedSortFilter.Value < count - 1 ? _selectedSortFilter.Value + 1 : 0;
                        break;
                }

            });
            sortOrderButton.onClick.AddListener(() =>
            {
                _isAscending.Value = !_isAscending.Value;
            });
            searchButton.onClick.AddListener(OnSearch);
            resetButton.onClick.AddListener(() =>
            {
                inputField.text = string.Empty;
                OnSearch();
            });
            inputField.onSubmit.AddListener(_ => OnSearch());
            inputField.onValueChanged.AddListener(_ =>
                searchButton.gameObject.SetActive(inputField.text.Length > 0));
            levelLimitToggle.onValueChanged.AddListener(value => _levelLimit.Value = value);
        }

        public async void OnBuyProductAction()
        {
            _page.SetValueAndForceNotify(_page.Value);
            ClearSelectedItems();
        }

        private async void OnUpdateSubTypeFilter(ItemSubTypeFilter filter)
        {
            _page.SetValueAndForceNotify(0);
        }

        private async void OnUpdateSortTypeFilter(ShopSortFilter filter)
        {
            _page.SetValueAndForceNotify(0);
            _sortText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}");
        }

        private async void OnUpdateAscending(bool isAscending)
        {
            _page.SetValueAndForceNotify(0);
            sortOrderIcon.localScale = new Vector3(1, isAscending ? 1 : -1, 1);
        }

        protected override async void UpdatePage(int page)
        {
            await CheckItem(_selectedSubTypeFilter.Value);
            base.UpdatePage(page);
            UpdateView();
        }

        private async Task CheckItem(ItemSubTypeFilter filter)
        {
            if (!_isActive)
            {
                return;
            }

            var orderType = filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone
                ? _isAscending.Value ? MarketOrderType.price : MarketOrderType.price_desc
                : _selectedSortFilter.Value.ToMarketOrderType(_isAscending.Value);
            var count = ReactiveShopState.GetCachedBuyItemCount(orderType, filter);
            var limit = _column * _row;
            if (count < (_page.Value + 1) * limit)
            {
                loading.SetActive(true);
                if (filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone)
                {
                    await ReactiveShopState.RequestBuyFungibleAssetsAsync(filter, orderType, limit * 5);
                }
                else
                {
                    await ReactiveShopState.RequestBuyProductsAsync(filter, orderType, limit * 15);
                }
                loading.SetActive(false);
            }

            if (filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone)
            {
                ReactiveShopState.SetBuyFungibleAssets(orderType);
            }
            else
            {
                ReactiveShopState.SetBuyProducts(orderType);
            }

            Set(ReactiveShopState.BuyItemProducts, ReactiveShopState.BuyFungibleAssetProducts);
            UpdateView();
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSubTypeFilter.Subscribe(OnUpdateSubTypeFilter).AddTo(gameObject);
            _selectedSortFilter.Subscribe(OnUpdateSortTypeFilter).AddTo(gameObject);
            _selectedItemIds.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _isAscending.Subscribe(OnUpdateAscending).AddTo(gameObject);
            _levelLimit.Subscribe(_ => UpdateView()).AddTo(gameObject);

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
                        if (item.ItemBase is not null)
                        {
                            if (_selectedItems.Exists(x =>
                                    x.Product.ProductId.Equals(item.Product.ProductId)))
                            {
                                ClearSelectedItems();
                            }
                            else
                            {
                                ClearSelectedItems();
                                item.Selected.SetValueAndForceNotify(true);
                                _selectedItems.Add(item);
                                ClickItemAction?.Invoke(item); // Show tooltip popup
                            }
                        }
                        else
                        {
                            if (_selectedItems.Exists(x =>
                                    x.FungibleAssetProduct.ProductId.Equals(item.FungibleAssetProduct.ProductId)))
                            {
                                ClearSelectedItems();
                            }
                            else
                            {
                                ClearSelectedItems();
                                item.Selected.SetValueAndForceNotify(true);
                                _selectedItems.Add(item);
                                ClickItemAction?.Invoke(item); // Show tooltip popup
                            }
                        }
                    }
                    else
                    {
                        item.Selected.SetValueAndForceNotify(true);
                        _selectedItems.Add(item);
                        ClickItemAction?.Invoke(item); // Show tooltip popup
                    }

                    break;

                case BuyMode.Multiple:
                    cartView.gameObject.SetActive(true);

                    ShopItem selectedItem = null;
                    if (item.ItemBase is not null)
                    {
                        foreach (var shopItem in _selectedItems.Where(x => x.Product is not null))
                        {
                            if (item.Product.ProductId == shopItem.Product.ProductId)
                            {
                                selectedItem = shopItem;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var shopItem in _selectedItems.Where(x => x.FungibleAssetProduct is not null))
                        {
                            if (item.FungibleAssetProduct.ProductId == shopItem.FungibleAssetProduct.ProductId)
                            {
                                selectedItem = shopItem;
                                break;
                            }
                        }
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

                        if (_selectedItems.Count() < CartMaxCount)
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
            toggleDropdowns.First().isOn = false;
            cartView.gameObject.SetActive(false);
            toggleDropdowns.First().isOn = true;
            toggleDropdowns.First().items.First().isOn = true;
            inputField.text = string.Empty;
            resetButton.interactable = false;
            if (_resetAnimator.isActiveAndEnabled)
            {
                _resetAnimator.Play(_hashDisabled);
            }

            _page.SetValueAndForceNotify(0);
            _selectedSubTypeFilter.SetValueAndForceNotify(ItemSubTypeFilter.Weapon);
            _selectedSortFilter.SetValueAndForceNotify(ShopSortFilter.CP);
            _selectedItemIds.Value.Clear();
            _isAscending.SetValueAndForceNotify(false);
            _levelLimit.SetValueAndForceNotify(levelLimitToggle.isOn);
            _mode.SetValueAndForceNotify(BuyMode.Single);

            ClearSelectedItems();
        }

        protected override IEnumerable<ShopItem> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItem>> items)
        {
            return items[_selectedSubTypeFilter.Value];
        }

        protected override void UpdateView()
        {
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
            base.UpdateView();
        }

        private void OnSearch()
        {
            resetButton.interactable = inputField.text.Length > 0;
            _resetAnimator.Play(inputField.text.Length > 0 ? _hashNormal : _hashDisabled);

            var containItemIds = new List<int>();
            foreach (var id in _itemIds)
            {
                var itemName = L10nManager.LocalizeItemName(id);
                if (Regex.IsMatch(itemName, inputField.text, RegexOptions.IgnoreCase))
                {
                    containItemIds.Add(id);
                }
            }

            _selectedItemIds.Value = containItemIds;
        }
    }
}
