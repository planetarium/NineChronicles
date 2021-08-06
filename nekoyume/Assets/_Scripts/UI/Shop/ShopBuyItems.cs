using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ShopBuyItems : MonoBehaviour
    {
        public List<ShopItemView> Items { get; } = new List<ShopItemView>();

        [SerializeField] private List<ToggleDropdown> toggleDropdowns = new List<ToggleDropdown>();
        [SerializeField] private TextMeshProUGUI pageText = null;
        [SerializeField] private Button previousPageButton = null;
        [SerializeField] private Button nextPageButton = null;
        [SerializeField] private Button sortButton = null;
        [SerializeField] private Button sortOrderButton = null;
        [SerializeField] private Button searchButton = null;
        [SerializeField] private Button resetButton = null;
        [SerializeField] private Animator resetAnimator = null;

        [SerializeField] private RectTransform sortOrderIcon = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private Transform inputPlaceholder = null;

        private readonly List<int> _itemIds = new List<int>();
        private TextMeshProUGUI _sortText;
        private ShopSortFilter _sortFilter = ShopSortFilter.Class;

        private readonly int _hashNormal = Animator.StringToHash("Normal");
        private readonly int _hashDisabled = Animator.StringToHash("Disabled");

        private int _filteredPageIndex = 0;
        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();
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

        public Model.ShopBuyItems SharedModel { get; private set; }

        #region Mono

        private void Awake()
        {
            var equipments = Game.Game.instance.TableSheets.EquipmentItemSheet.Values.Select(x => x.Id);
            var consumableItems = Game.Game.instance.TableSheets.ConsumableItemSheet.Values.Select(x => x.Id);
            var costumes = Game.Game.instance.TableSheets.CostumeItemSheet.Values.Select(x => x.Id);
            var materials = Game.Game.instance.TableSheets.MaterialItemSheet.Values.Select(x => x.Id);
            _itemIds.AddRange(equipments);
            _itemIds.AddRange(consumableItems);
            _itemIds.AddRange(costumes);
            _itemIds.AddRange(materials);
            _sortText = sortButton.GetComponentInChildren<TextMeshProUGUI>();
            inputPlaceholder.SetAsLastSibling();

            SharedModel = new Model.ShopBuyItems();
            SharedModel.Items
                .Subscribe(_ => UpdateView())
                .AddTo(gameObject);

            foreach (var toggleDropdown in toggleDropdowns)
            {
                var index = toggleDropdowns.IndexOf(toggleDropdown);
                var toggleType = _toggleTypes[index];
                toggleDropdown.onValueChanged.AddListener((value) =>
                {
                    if (value)
                    {
                        if (_toggleSubTypes[toggleType].Count > 0)
                        {
                            SharedModel.itemSubTypeFilter = _toggleSubTypes[toggleType].First();
                            toggleDropdown.items.First().isOn = true;
                        }
                        else
                        {
                            SharedModel.itemSubTypeFilter = ItemSubTypeFilter.Materials;
                        }
                        OnItemSubTypeFilterChanged();
                    }
                });

                var subItems = toggleDropdown.items;

                foreach (var item in subItems)
                {
                    var subIndex = subItems.IndexOf(item);
                    var subTypes = _toggleSubTypes[toggleType];
                    var subToggleType = subTypes[subIndex];
                    item.onValueChanged.AddListener((value) =>
                    {
                        if (value)
                        {
                            SharedModel.itemSubTypeFilter = subToggleType;
                            OnItemSubTypeFilterChanged();
                        }
                    });
                }
            }

            previousPageButton.OnClickAsObservable().Subscribe(OnClickPreviousPage).AddTo(gameObject);
            nextPageButton.OnClickAsObservable().Subscribe(OnClickNextPage).AddTo(gameObject);
            sortButton.OnClickAsObservable().Subscribe(OnClickSort).AddTo(gameObject);
            sortOrderButton.OnClickAsObservable().Subscribe(OnClickSortOrder).AddTo(gameObject);
            searchButton.OnClickAsObservable().Subscribe(OnSearch).AddTo(gameObject);
            resetButton.OnClickAsObservable().Subscribe(OnClickReset).AddTo(gameObject);
            inputField.onSubmit.AddListener(OnClickSearch);
            inputField.onValueChanged.AddListener(OnInputValueChange);
        }

        public void Reset()
        {
            toggleDropdowns.First().isOn = true;
            toggleDropdowns.First().items.First().isOn = true;
            inputField.text = string.Empty;
            resetButton.interactable = false;
            resetAnimator.Play(_hashDisabled);
            sortOrderIcon.localScale = new Vector3(1, -1, 1);
            SharedModel.itemSubTypeFilter = ItemSubTypeFilter.Weapon;
            SharedModel.isReverseOrder = false;
            SharedModel.searchIds = new List<int>();
            SharedModel.SetMultiplePurchase(false);
            SharedModel.ResetShopItems();
            _sortFilter = ShopSortFilter.Class;
            UpdateSort();
        }

        public void Show()
        {
            Reset();

            ReactiveShopState.BuyDigests
                .Subscribe(SharedModel.ResetItems)
                .AddTo(_disposablesAtOnEnable);
        }

        public void Close()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
        }

        private void OnDestroy()
        {
            SharedModel.Dispose();
            SharedModel = null;
        }
        #endregion

        private void UpdateView()
        {
            foreach (var item in Items)
            {
                item.Clear();
            }

            if (SharedModel is null)
            {
                return;
            }

            _filteredPageIndex = 0;
            UpdateViewWithFilteredPageIndex(SharedModel.Items.Value);
        }

        private void UpdateViewWithFilteredPageIndex(
            IReadOnlyDictionary<int, List<ShopItem>> models)
        {
            var count = models?.Count ?? 0;
            if (count > _filteredPageIndex)
            {
                UpdateViewWithItems(models[_filteredPageIndex]);
            }

            previousPageButton.gameObject.SetActive(_filteredPageIndex > 0);
            nextPageButton.gameObject.SetActive(_filteredPageIndex + 1 < count);
            pageText.text = (_filteredPageIndex + 1).ToString();

        }

        private void UpdateViewWithItems(IEnumerable<ShopItem> viewModels)
        {
            using (var itemViews = Items.GetEnumerator())
            using (var itemModels = viewModels.GetEnumerator())
            {
                while (itemViews.MoveNext())
                {
                    if (itemViews.Current is null)
                    {
                        break;
                    }

                    if (!itemModels.MoveNext())
                    {
                        itemViews.Current.Clear();
                        continue;
                    }

                    itemViews.Current.SetData(itemModels.Current);
                }
            }
        }

        private void OnItemSubTypeFilterChanged()
        {
            SharedModel.ResetShopItems();
        }

        private void OnSortFilterChanged()
        {
            SharedModel.ResetShopItems();
        }

        private void OnClickPreviousPage(Unit unit)
        {
            if (_filteredPageIndex == 0)
            {
                previousPageButton.gameObject.SetActive(false);
                return;
            }

            _filteredPageIndex--;
            nextPageButton.gameObject.SetActive(true);

            if (_filteredPageIndex == 0)
            {
                previousPageButton.gameObject.SetActive(false);
            }

            UpdateViewWithFilteredPageIndex(SharedModel.Items.Value);
        }

        private void OnClickNextPage(Unit unit)
        {
            var count = SharedModel.Items.Value.Count;

            if (_filteredPageIndex + 1 >= count)
            {
                nextPageButton.gameObject.SetActive(false);
                return;
            }

            _filteredPageIndex++;
            previousPageButton.gameObject.SetActive(true);

            if (_filteredPageIndex + 1 == count)
            {
                nextPageButton.gameObject.SetActive(false);
            }

            UpdateViewWithFilteredPageIndex(SharedModel.Items.Value);
        }

        private void OnClickSort(Unit unit)
        {
            int count = Enum.GetNames(typeof(ShopSortFilter)).Length;
            _sortFilter = (int) _sortFilter < count - 1 ? _sortFilter + 1 : 0;
            UpdateSort();
        }

        private void UpdateSort()
        {
            _sortText.text = L10nManager.Localize($"UI_{_sortFilter.ToString().ToUpper()}");
            SharedModel.sortFilter = _sortFilter;
            OnSortFilterChanged();
        }

        private void OnClickSortOrder(Unit unit)
        {
            var scale = sortOrderIcon.localScale;
            scale.y *= -1;
            sortOrderIcon.localScale = scale;

            SharedModel.isReverseOrder = !SharedModel.isReverseOrder;
            OnSortFilterChanged();
        }

        private void OnClickSearch(string value)
        {
            OnSearch(Unit.Default);
        }

        private void OnInputValueChange(string value)
        {
            searchButton.gameObject.SetActive(inputField.text.Length > 0);
        }

        private void OnClickReset(Unit unit)
        {
            inputField.text = string.Empty;
            OnSearch(Unit.Default);
        }

        private void OnSearch(Unit unit)
        {
            resetButton.interactable = inputField.text.Length > 0;
            resetAnimator.Play(inputField.text.Length > 0 ? _hashNormal : _hashDisabled);
            var containItemIds = new List<int>();
            foreach (var id in _itemIds)
            {
                var itemName = L10nManager.LocalizeItemName(id);
                if (Regex.IsMatch(itemName, inputField.text, RegexOptions.IgnoreCase))
                {
                    containItemIds.Add(id);
                }
            }

            SharedModel.searchIds = containItemIds;
            OnSortFilterChanged();
        }
    }
}
