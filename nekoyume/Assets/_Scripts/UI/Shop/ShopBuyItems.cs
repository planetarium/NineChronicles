using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    public class ShopBuyItems : MonoBehaviour
    {
        //todo 스크롤뷰 content 길이 조절해줘야됨
        public List<ShopItemView> Items { get; } = new List<ShopItemView>();

        [SerializeField] private List<NCToggleDropdown> toggleDropdowns = new List<NCToggleDropdown>();
        [SerializeField] private TextMeshProUGUI pageText = null;
        [SerializeField] private Button previousPageButton = null;
        [SerializeField] private Button nextPageButton = null;
        [SerializeField] private Button sortButton = null;
        [SerializeField] private Button sortOrderButton = null;
        [SerializeField] private Button searchButton = null;
        [SerializeField] private RectTransform sortOrderIcon = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private Transform inputPlaceholder = null;

        private readonly List<int> _itemIds = new List<int>();
        private TextMeshProUGUI _sortText;
        private SortFilter _sortFilter = SortFilter.Class;

        // [SerializeField]
        // private TouchHandler refreshButtonTouchHandler = null;
        //
        // [SerializeField]
        // private RefreshButton refreshButton = null;

        private int _filteredPageIndex;
        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();
        private readonly List<ItemSubTypeFilter> _toggleTypes = new List<ItemSubTypeFilter>()
        {
            // ItemSubTypeFilter.All,
            ItemSubTypeFilter.Equipment,
            ItemSubTypeFilter.Food,
            ItemSubTypeFilter.Costume,
        };

        private readonly Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>> _toggleSubTypes =
            new Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>>()
        {
            // {
            //     ItemSubTypeFilter.All, new List<ItemSubTypeFilter>()
            // },
            {
                ItemSubTypeFilter.Equipment, new List<ItemSubTypeFilter>()
                {
                    // ItemSubTypeFilter.Equipment,
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
                    // ItemSubTypeFilter.Food,
                    ItemSubTypeFilter.Food_HP,
                    ItemSubTypeFilter.Food_ATK,
                    ItemSubTypeFilter.Food_DEF,
                    ItemSubTypeFilter.Food_CRI,
                    ItemSubTypeFilter.Food_HIT,
                }
            },
            {
                ItemSubTypeFilter.Costume, new List<ItemSubTypeFilter>()
                {
                    // ItemSubTypeFilter.Costume,
                    ItemSubTypeFilter.FullCostume,
                    ItemSubTypeFilter.HairCostume,
                    ItemSubTypeFilter.EarCostume,
                    ItemSubTypeFilter.EyeCostume,
                    ItemSubTypeFilter.TailCostume,
                    ItemSubTypeFilter.Title,
                }
            },
        };

        public Model.ShopItems SharedModel { get; private set; }

        #region Mono


        private void Awake()
        {
            var equipments = Game.Game.instance.TableSheets.EquipmentItemSheet.Values.Select(x => x.Id);
            var consumableItems = Game.Game.instance.TableSheets.ConsumableItemSheet.Values.Select(x => x.Id);
            var costumes = Game.Game.instance.TableSheets.CostumeItemSheet.Values.Select(x => x.Id);
            _itemIds.AddRange(equipments);
            _itemIds.AddRange(consumableItems);
            _itemIds.AddRange(costumes);
            _sortText = sortButton.GetComponentInChildren<TextMeshProUGUI>();
            inputPlaceholder.SetAsLastSibling();

            SharedModel = new Model.ShopItems();
            SharedModel.State
                .Subscribe(_ => UpdateView())
                .AddTo(gameObject);
            SharedModel.AgentProducts
                .Subscribe(_ => UpdateView())
                .AddTo(gameObject);
            SharedModel.ItemSubTypeProducts
                .Subscribe(_ => UpdateView())
                .AddTo(gameObject);

            foreach (var toggleDropdown in toggleDropdowns)
            {
                var index = toggleDropdowns.IndexOf(toggleDropdown);
                var toggleType = _toggleTypes[index];
                toggleDropdown.SetText(toggleType.TypeToString());
                toggleDropdown.onValueChanged.AddListener((value) =>
                {
                    if (value)
                    {
                        SharedModel.itemSubTypeFilter = _toggleSubTypes[toggleType].First();
                        toggleDropdown.items.First().isOn = true;
                        OnItemSubTypeFilterChanged();
                    }
                });

                var subItems = toggleDropdown.items;
                var removeList = new List<NCToggle>();
                foreach (var item in subItems)
                {
                    var subIndex = subItems.IndexOf(item);
                    var subTypes = _toggleSubTypes[toggleType];

                    if (subIndex < subTypes.Count)
                    {
                        var subToggleType = subTypes[subIndex];
                        item.SetText(subToggleType.TypeToString());
                        item.onValueChanged.AddListener((value) =>
                        {
                            if (value)
                            {
                                SharedModel.itemSubTypeFilter = subToggleType;
                                OnItemSubTypeFilterChanged();
                            }
                        });
                    }
                    else
                    {
                        removeList.Add(item);
                    }
                }
                subItems.RemoveAll(removeList.Contains);
            }

            previousPageButton.OnClickAsObservable().Subscribe(OnClickPreviousPage).AddTo(gameObject);
            nextPageButton.OnClickAsObservable().Subscribe(OnClickNextPage).AddTo(gameObject);


            sortButton.OnClickAsObservable().Subscribe(OnClickSort).AddTo(gameObject);
            sortOrderButton.OnClickAsObservable().Subscribe(OnClickSortOrder).AddTo(gameObject);
            searchButton.OnClickAsObservable().Subscribe(OnSearch).AddTo(gameObject);
            inputField.onSubmit.AddListener(OnClickSearch);

            // refreshButtonTouchHandler.OnClick.Subscribe(_ =>
            // {
            //     AudioController.PlayClick();
            //     // NOTE: 아래 코드를 실행해도 아무런 변화가 없습니다.
            //     // 새로고침을 새로 정의한 후에 수정합니다.
            //     // SharedModel.ResetItemSubTypeProducts();
            // }).AddTo(gameObject);

            ReactiveShopState.AgentProducts
                .Subscribe(SharedModel.ResetAgentProducts)
                .AddTo(_disposablesAtOnEnable);

            ReactiveShopState.ItemSubTypeProducts
                .Subscribe(SharedModel.ResetItemSubTypeProducts)
                .AddTo(_disposablesAtOnEnable);
        }

        public void Show()
        {
            toggleDropdowns.First().isOn = true;
        }

        private void OnEnable()
        {
            inputField.text = string.Empty;
            sortOrderIcon.localScale = Vector3.one;

            SharedModel.itemSubTypeFilter = ItemSubTypeFilter.Weapon;
            SharedModel.sortFilter = SortFilter.Class;
            SharedModel.isReverseOrder = false;
            SharedModel.searchIds = new List<int>();
            SharedModel.SetMultiplePurchase(false);
        }

        private void OnDisable()
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
            UpdateViewWithFilteredPageIndex(SharedModel.ItemSubTypeProducts.Value);
            // refreshButton.gameObject.SetActive(true);
            // refreshButton.PlayAnimation(NPCAnimation.Type.Appear);
        }

        private void UpdateViewWithFilteredPageIndex(
            IReadOnlyDictionary<int, List<ShopItem>> models)
        {
            var count = models?.Count ?? 0;
            UpdateViewWithItems(count > _filteredPageIndex
                ? models[_filteredPageIndex]
                : new List<ShopItem>());

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
            SharedModel.ResetAgentProducts();
            SharedModel.ResetItemSubTypeProducts();
        }

        private void OnSortFilterChanged()
        {
            SharedModel.ResetAgentProducts();
            SharedModel.ResetItemSubTypeProducts();
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

            UpdateViewWithFilteredPageIndex(SharedModel.ItemSubTypeProducts.Value);
        }

        private void OnClickNextPage(Unit unit)
        {
            var count = SharedModel.ItemSubTypeProducts.Value.Count;

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

            UpdateViewWithFilteredPageIndex(SharedModel.ItemSubTypeProducts.Value);
        }

        private void OnClickSort(Unit unit)
        {
            int count = Enum.GetNames(typeof(SortFilter)).Length;
            _sortFilter = (int) _sortFilter < count - 1 ? _sortFilter + 1 : 0;
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

        private void OnSearch(Unit unit)
        {
            var containItemIds = new List<int>();
            foreach (var id in _itemIds)
            {
                var itemName = L10nManager.LocalizeItemName(id);
                if (itemName.Contains(inputField.text))
                {
                    containItemIds.Add(id);
                }
            }

            SharedModel.searchIds = containItemIds;
            OnSortFilterChanged();
        }
    }
}
