using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;

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
        private List<ToggleDropdown> toggleDropdowns = new List<ToggleDropdown>();

        [SerializeField]
        private Button sortButton;

        [SerializeField]
        private Button sortOrderButton;

        [SerializeField]
        private Button searchButton;

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private Toggle levelLimitToggle;

        [SerializeField]
        private RectTransform sortOrderIcon = null;

        [SerializeField]
        private TMP_InputField inputField = null;

        [SerializeField]
        private Transform inputPlaceholder = null;

        private readonly List<ItemSubTypeFilter> _toggleTypes = new List<ItemSubTypeFilter>()
        {
            ItemSubTypeFilter.Equipment,
            ItemSubTypeFilter.Food,
            ItemSubTypeFilter.Materials,
            ItemSubTypeFilter.Costume,
        };

        private readonly Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>> _toggleSubTypes =
            new Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>>()
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
            };

        private readonly List<ShopItemViewModel> _selectedItems = new List<ShopItemViewModel>();
        private readonly List<int> _itemIds = new List<int>();
        private readonly int _hashNormal = Animator.StringToHash("Normal");
        private readonly int _hashDisabled = Animator.StringToHash("Disabled");
        private const int CartMaxCount = 8;

        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new ReactiveProperty<ItemSubTypeFilter>(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
            new ReactiveProperty<ShopSortFilter>(ShopSortFilter.Class);

        private readonly ReactiveProperty<List<int>> _selectedItemIds =
            new ReactiveProperty<List<int>>(new List<int>());

        private readonly ReactiveProperty<bool> _isAscending = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _levelLimit = new ReactiveProperty<bool>();

        private readonly ReactiveProperty<BuyMode> _mode =
            new ReactiveProperty<BuyMode>(BuyMode.Single);

        private Action<List<ShopItemViewModel>> _onBuyMultiple;
        private Animator _resetAnimator;
        private TextMeshProUGUI _sortText;

        public bool IsFocused => inputField.isFocused;

        public void ClearSelectedItems()
        {
            foreach (var model in _selectedItems)
            {
                model?.Selected.SetValueAndForceNotify(false);
            }

            _selectedItems.Clear();
            cartView.UpdateCart(_selectedItems);
        }

        public void SetAction(Action<List<ShopItemViewModel>> onBuyMultiple)
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
                () => _mode.SetValueAndForceNotify(BuyMode.Multiple),
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

                    if (_toggleSubTypes[toggleType].Count > 0)
                    {
                        _selectedSubTypeFilter.Value = _toggleSubTypes[toggleType].First();
                        toggleDropdown.items.First().isOn = true;
                    }
                    else
                    {
                        _selectedSubTypeFilter.Value = ItemSubTypeFilter.Materials;
                    }

                    UpdateView();
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

                        _selectedSubTypeFilter.Value = subToggleType;
                        UpdateView();
                    });
                    item.onClickToggle.AddListener(AudioController.PlayClick);
                }
            }

            sortButton.onClick.AddListener(() =>
            {
                var count = Enum.GetNames(typeof(ShopSortFilter)).Length;
                _selectedSortFilter.Value = (int)_selectedSortFilter.Value < count - 1
                    ? _selectedSortFilter.Value + 1
                    : 0;
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

        protected override void SubscribeToSearchConditions()
        {
            _selectedSubTypeFilter.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _selectedSortFilter.Subscribe(filter =>
            {
                _sortText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}");
                UpdateView();
            }).AddTo(gameObject);
            _selectedItemIds.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _isAscending.Subscribe(v =>
            {
                sortOrderIcon.localScale = new Vector3(1, v ? 1 : -1, 1);
                UpdateView();
            }).AddTo(gameObject);
            _levelLimit.Subscribe(_ => UpdateView()).AddTo(gameObject);

            _mode.Subscribe(x =>
            {
                cartView.SetMode(x);
                ClearSelectedItems();
            }).AddTo(gameObject);
        }

        protected override void OnClickItem(ShopItemViewModel item)
        {
            switch (_mode.Value)
            {
                case BuyMode.Single:
                    if (_selectedItems.Any())
                    {
                        if (_selectedItems.Exists(x =>
                                x.OrderDigest.OrderId.Equals(item.OrderDigest.OrderId)))
                        {
                            ClearSelectedItems();
                        }
                        else
                        {
                            ClearSelectedItems();
                            item.Selected.SetValueAndForceNotify(true);
                            _selectedItems.Add(item);
                            ClickItemAction?.Invoke(item, item.View); // Show tooltip popup
                        }
                    }
                    else
                    {
                        item.Selected.SetValueAndForceNotify(true);
                        _selectedItems.Add(item);
                        ClickItemAction?.Invoke(item, item.View); // Show tooltip popup
                    }

                    break;

                case BuyMode.Multiple:
                    var selectedItem = _selectedItems.FirstOrDefault(x =>
                        x.OrderDigest.OrderId.Equals(item.OrderDigest.OrderId));
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

                    cartView.UpdateCart(_selectedItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Reset()
        {
            toggleDropdowns.First().isOn = true;
            toggleDropdowns.First().items.First().isOn = true;
            inputField.text = string.Empty;
            resetButton.interactable = false;
            _resetAnimator.Play(_hashDisabled);

            _selectedSubTypeFilter.SetValueAndForceNotify(ItemSubTypeFilter.Weapon);
            _selectedSortFilter.SetValueAndForceNotify(ShopSortFilter.Class);
            _selectedItemIds.Value.Clear();
            _isAscending.SetValueAndForceNotify(false);
            _levelLimit.SetValueAndForceNotify(false);
            _mode.SetValueAndForceNotify(BuyMode.Single);

            ClearSelectedItems();
        }

        protected override IEnumerable<ShopItemViewModel> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>> items)
        {
            var models = items[_selectedSubTypeFilter.Value];
            models = models.Where(x => !x.Expired.Value).ToList();

            if (_selectedItemIds.Value.Any()) // _selectedItemIds
            {
                models = models.Where(x =>
                    _selectedItemIds.Value.Exists(y => x.ItemBase.Id.Equals(y))).ToList();
            }

            if (_levelLimit.Value)
            {
                models = models.Where(x => Util.IsUsableItem(x.ItemBase.Id)).ToList();
            }

            return _selectedSortFilter.Value switch
            {
                ShopSortFilter.CP => _isAscending.Value
                    ? models.OrderBy(x => x.OrderDigest.CombatPoint).ToList()
                    : models.OrderByDescending(x => x.OrderDigest.CombatPoint).ToList(),
                ShopSortFilter.Price => _isAscending.Value
                    ? models.OrderBy(x => x.OrderDigest.Price).ToList()
                    : models.OrderByDescending(x => x.OrderDigest.Price).ToList(),
                ShopSortFilter.Class => _isAscending.Value
                    ? models.OrderBy(x => x.Grade)
                        .ThenByDescending(x => x.ItemBase.ItemType)
                        .ToList()
                    : models.OrderByDescending(x => x.Grade)
                        .ThenByDescending(x => x.ItemBase.ItemType)
                        .ToList(),
                _ => throw new ArgumentOutOfRangeException()
            };
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
                    cartView.UpdateCart(_selectedItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
