using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
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
        private GameObject loading;

        [field: SerializeField]
        public Button ItemFilterButton { get; private set; }

        private int _loadingCount;

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
                        ItemSubTypeFilter.Food_CRI,
                        ItemSubTypeFilter.Food_DEF,
                        ItemSubTypeFilter.Food_SPD,
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

        private readonly Dictionary<ItemSubTypeFilter, List<ShopSortFilter>> _toggleSortFilters =
            new()
            {
                {
                    ItemSubTypeFilter.Equipment, new List<ShopSortFilter>()
                    {
                        ShopSortFilter.CP,
                        ShopSortFilter.Price,
                        ShopSortFilter.Class,
                        ShopSortFilter.Crystal,
                        ShopSortFilter.CrystalPerPrice,
                        ShopSortFilter.EquipmentLevel,
                        ShopSortFilter.OptionCount,
                    }
                },
                {
                    ItemSubTypeFilter.Food, new List<ShopSortFilter>()
                    {
                        ShopSortFilter.CP,
                        ShopSortFilter.Price,
                        ShopSortFilter.Class,
                        ShopSortFilter.Crystal,
                        ShopSortFilter.CrystalPerPrice,
                        ShopSortFilter.EquipmentLevel,
                        ShopSortFilter.OptionCount,
                    }
                },
                {
                    ItemSubTypeFilter.Materials, new List<ShopSortFilter>()
                    {
                        ShopSortFilter.Price,
                        ShopSortFilter.UnitPrice,
                    }
                },
                {
                    ItemSubTypeFilter.Costume, new List<ShopSortFilter>()
                    {
                        ShopSortFilter.CP,
                        ShopSortFilter.Price,
                        ShopSortFilter.Class,
                        ShopSortFilter.Crystal,
                        ShopSortFilter.CrystalPerPrice,
                        ShopSortFilter.EquipmentLevel,
                        ShopSortFilter.OptionCount,
                    }
                },
                {
                    ItemSubTypeFilter.Stones, new List<ShopSortFilter>()
                    {
                        ShopSortFilter.Price,
                        ShopSortFilter.UnitPrice
                    }
                },
            };

        private readonly Dictionary<ItemSubTypeFilter, int> _toggleSortFilterIndex = new()
        {
            { ItemSubTypeFilter.Equipment, 0 },
            { ItemSubTypeFilter.Food, 0 },
            { ItemSubTypeFilter.Materials, 0 },
            { ItemSubTypeFilter.Costume, 0 },
            { ItemSubTypeFilter.Stones, 0 },
        };

        private readonly List<ShopItem> _selectedItems = new();
        private readonly int _hashNormal = Animator.StringToHash("Normal");
        private readonly int _hashDisabled = Animator.StringToHash("Disabled");
        private const int CartMaxCount = 20;

        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
            new(ShopSortFilter.CP);

        private readonly ReactiveProperty<bool> _isAscending = new();
        private readonly ReactiveProperty<bool> _levelLimit = new();

        private readonly ReactiveProperty<BuyMode> _mode = new(BuyMode.Single);

        private Action<List<ShopItem>> _onBuyMultiple;

        private Animator _resetAnimator;
        private TextMeshProUGUI _sortText;

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

            ReactiveShopState.Initialize();

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
            foreach (var toggleDropdown in toggleDropdowns)
            {
                var index = toggleDropdowns.IndexOf(toggleDropdown);
                var toggleType = _toggleTypes[index];
                toggleDropdown.onClickToggle.AddListener(() =>
                {
                    _selectedSubTypeFilter.Value = _toggleSubTypes[toggleType].First();

                    var itemTypeFilter = _selectedSubTypeFilter.Value.ToItemTypeFilter();
                    _selectedSortFilter.Value = _toggleSortFilters[itemTypeFilter].First();
                    _toggleSortFilterIndex[itemTypeFilter] = 0;

                    toggleDropdown.items.First().isOn = true;
                    ResetPage();
                });
                toggleDropdown.onClickToggle.AddListener(AudioController.PlayClick);

                var subItems = toggleDropdown.items;

                foreach (var item in subItems)
                {
                    var subIndex = subItems.IndexOf(item);
                    var subTypes = _toggleSubTypes[toggleType];
                    var subToggleType = subTypes[subIndex];
                    item.onClickToggle.AddListener(() =>
                    {
                        _selectedSubTypeFilter.Value = subToggleType;

                        var itemTypeFilter = _selectedSubTypeFilter.Value.ToItemTypeFilter();
                        _selectedSortFilter.Value = _toggleSortFilters[itemTypeFilter].First();
                        _toggleSortFilterIndex[itemTypeFilter] = 0;

                        ResetPage();
                    });
                    item.onClickToggle.AddListener(AudioController.PlayClick);
                }
            }

            sortButton.onClick.AddListener(() =>
            {
                var itemTypeFilter = _selectedSubTypeFilter.Value.ToItemTypeFilter();
                var sortFilters = _toggleSortFilters[itemTypeFilter];
                var index = _toggleSortFilterIndex[itemTypeFilter];

                var nextIndex = (index + 1) % sortFilters.Count;
                _toggleSortFilterIndex[itemTypeFilter] = nextIndex;
                _selectedSortFilter.Value = sortFilters[nextIndex];

                ResetPage();
            });

            resetButton.onClick.AddListener(() =>
            {
                ReactiveShopState.ResetItemFilter();
                Widget.Find<ItemFilterPopup>().ItemFilterOptions = ReactiveShopState.ItemFilterOptions;
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

        private async Task SetItems(bool reset = false)
        {
            if (!_isActive)
            {
                return;
            }

            _loadingCount++;
            loading.SetActive(_loadingCount > 0);
            await ReactiveShopState.RefreshItemsAsync(
                _selectedSubTypeFilter.Value,
                _selectedSortFilter.Value.ToMarketOrderType(_isAscending.Value),
                _column * _row,
                _levelLimit.Value,
                reset);
            _loadingCount--;
            loading.SetActive(_loadingCount > 0);

            Set(ReactiveShopState.BuyItemProducts, ReactiveShopState.BuyFungibleAssetProducts);
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSortFilter.Subscribe(filter =>
            {
                _sortText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}");
            }).AddTo(gameObject);
            _isAscending.Subscribe(isAscending =>
            {
                sortOrderIcon.localScale = new Vector3(1, isAscending ? 1 : -1, 1);
            }).AddTo(gameObject);

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
            _loadingCount = 0;
            loading.SetActive(_loadingCount > 0);

            cartView.gameObject.SetActive(false);
            toggleDropdowns.First().isOn = true;
            toggleDropdowns.First().items.First().isOn = true;
            resetButton.interactable = false;
            levelLimitToggle.isOn = false;
            if (_resetAnimator.isActiveAndEnabled)
            {
                _resetAnimator.Play(_hashDisabled);
            }

            foreach (var key in _toggleTypes)
            {
                _toggleSortFilterIndex[key] = 0;
            }

            _page.SetValueAndForceNotify(0);
            _selectedSubTypeFilter.SetValueAndForceNotify(ItemSubTypeFilter.Weapon);
            _selectedSortFilter.SetValueAndForceNotify(ShopSortFilter.CP);
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

            SetResetButton();

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

        public void SetItemFilterOption(ItemFilterOptions type)
        {
            ReactiveShopState.SetItemFilterOption(type);
            ResetPage();

            SetResetButton();
        }

        private void SetResetButton()
        {
            resetButton.interactable = ReactiveShopState.IsNeedSearch;
            _resetAnimator.Play(resetButton.interactable ? _hashNormal : _hashDisabled);
        }
    }
}
