using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    public class ShopBuyItems : MonoBehaviour
    {
        public List<ShopItemView> Items { get; set; } = new List<ShopItemView>();

        [SerializeField] List<NCToggleDropdown> toggleDropdowns = new List<NCToggleDropdown>();
        [SerializeField] private Button previousPageButton = null;
        [SerializeField] private Button nextPageButton = null;
        [SerializeField] private TextMeshProUGUI pageText = null;


        // [SerializeField]
        // private TouchHandler refreshButtonTouchHandler = null;
        //
        // [SerializeField]
        // private RefreshButton refreshButton = null;

        private int _filteredPageIndex;
        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();
        private readonly List<ItemSubTypeFilter> _toggleTypes = new List<ItemSubTypeFilter>()
        {
            ItemSubTypeFilter.Equipment,
            ItemSubTypeFilter.Food,
            ItemSubTypeFilter.Costume,
        };

        private readonly Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>> _toggleSubTypes =
            new Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>>()
        {
            {
                ItemSubTypeFilter.Equipment, new List<ItemSubTypeFilter>()
                {
                    ItemSubTypeFilter.Equipment,
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
                    ItemSubTypeFilter.Food,
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
                    ItemSubTypeFilter.Costume,
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
                toggleDropdown.SetText(FilterSubTypeToString(toggleType));
                toggleDropdown.onValueChanged.AddListener((value) =>
                {
                    if (value)
                    {
                        SharedModel.itemSubTypeFilter = toggleType;
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
                        item.SetText(FilterSubTypeToString(subToggleType));
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

            //
            // itemSubTypeFilter.AddOptions(new[]
            //     {
            //         ItemSubTypeFilter.All,
            //         ItemSubTypeFilter.Weapon,
            //         ItemSubTypeFilter.Armor,
            //         ItemSubTypeFilter.Belt,
            //         ItemSubTypeFilter.Necklace,
            //         ItemSubTypeFilter.Ring,
            //         ItemSubTypeFilter.Food,
            //         ItemSubTypeFilter.FullCostume,
            //         ItemSubTypeFilter.HairCostume,
            //         ItemSubTypeFilter.EarCostume,
            //         ItemSubTypeFilter.EyeCostume,
            //         ItemSubTypeFilter.TailCostume,
            //         ItemSubTypeFilter.Title,
            //     }
            //     .Select(type => type == ItemSubTypeFilter.All
            //         ? L10nManager.Localize("ALL")
            //         : ((ItemSubType) Enum.Parse(typeof(ItemSubType), type.ToString()))
            //         .GetLocalizedString())
            //     .ToList());
            //
            // itemSubTypeFilter.onValueChanged.AsObservable()
            //     .Select(index =>
            //     {
            //         try
            //         {
            //             return (ItemSubTypeFilter) index;
            //         }
            //         catch
            //         {
            //             return ItemSubTypeFilter.All;
            //         }
            //     })
            //     .Subscribe(filter =>
            //     {
            //         SharedModel.itemSubTypeFilter = filter;
            //         OnItemSubTypeFilterChanged();
            //     })
            //     .AddTo(gameObject);
            //
            // sortFilter.AddOptions(new[]
            //     {
            //         SortFilter.Class,
            //         SortFilter.CP,
            //         SortFilter.Price,
            //     }
            //     .Select(type => L10nManager.Localize($"UI_{type.ToString().ToUpper()}"))
            //     .ToList());
            // sortFilter.onValueChanged.AsObservable()
            //     .Select(index =>
            //     {
            //         try
            //         {
            //             return (SortFilter) index;
            //         }
            //         catch
            //         {
            //             return SortFilter.Class;
            //         }
            //     })
            //     .Subscribe(filter =>
            //     {
            //         SharedModel.sortFilter = filter;
            //         OnSortFilterChanged();
            //     })
            //     .AddTo(gameObject);
            //
            previousPageButton.OnClickAsObservable()
                .Subscribe(OnPreviousPageButtonClick)
                .AddTo(gameObject);
            nextPageButton.OnClickAsObservable()
                .Subscribe(OnNextPageButtonClick)
                .AddTo(gameObject);
            //
            // refreshButtonTouchHandler.OnClick.Subscribe(_ =>
            // {
            //     AudioController.PlayClick();
            //     // NOTE: 아래 코드를 실행해도 아무런 변화가 없습니다.
            //     // 새로고침을 새로 정의한 후에 수정합니다.
            //     // SharedModel.ResetItemSubTypeProducts();
            // }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            toggleDropdowns.First().isOn = false;
            toggleDropdowns.First().isOn = true;

            // itemSubTypeFilter.SetValueWithoutNotify(0);
            SharedModel.itemSubTypeFilter = 0;
            // sortFilter.SetValueWithoutNotify(0);
            SharedModel.sortFilter = 0;

            ReactiveShopState.AgentProducts
                .Subscribe(SharedModel.ResetAgentProducts)
                .AddTo(_disposablesAtOnEnable);

            ReactiveShopState.ItemSubTypeProducts
                .Subscribe(SharedModel.ResetItemSubTypeProducts)
                .AddTo(_disposablesAtOnEnable);
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

        private void OnPreviousPageButtonClick(Unit unit)
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

        private void OnNextPageButtonClick(Unit unit)
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

        private string FilterSubTypeToString(ItemSubTypeFilter type)
        {
            switch (type)
            {
                case ItemSubTypeFilter.All:
                case ItemSubTypeFilter.Food:
                case ItemSubTypeFilter.Equipment:
                case ItemSubTypeFilter.Costume:
                    return L10nManager.Localize("ALL");

                case ItemSubTypeFilter.Food_HP:
                    return StatType.HP.ToString();
                case ItemSubTypeFilter.Food_ATK:
                    return StatType.ATK.ToString();
                case ItemSubTypeFilter.Food_DEF:
                    return StatType.DEF.ToString();
                case ItemSubTypeFilter.Food_CRI:
                    return StatType.CRI.ToString();
                case ItemSubTypeFilter.Food_HIT:
                    return StatType.HIT.ToString();

                default:
                    return ((ItemSubType) Enum.Parse(typeof(ItemSubType), type.ToString()))
                        .GetLocalizedString();
            }
        }
    }
}
