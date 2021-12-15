using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ShopSellItems : MonoBehaviour
    {
        public List<ShopItemView> items;

        [SerializeField]
        private TMP_Dropdown itemSubTypeFilter = null;

        [SerializeField]
        private TMP_Dropdown sortFilter = null;

        [SerializeField]
        private Button previousPageButton = null;

        [SerializeField]
        private InteractableSwitchableSelectable previousPageButtonInteractableSwitch = null;

        [SerializeField]
        private Button nextPageButton = null;

        [SerializeField]
        private InteractableSwitchableSelectable nextPageButtonInteractableSwitch = null;

        [SerializeField]
        private RefreshButton refreshButton = null;

        private int _filteredPageIndex;
        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public Model.ShopSellItems SharedModel { get; private set; }

        #region Mono

        private void Awake()
        {
            SharedModel = new Model.ShopSellItems();
            SharedModel.Items
                .Subscribe(_ => UpdateView())
                .AddTo(gameObject);

            itemSubTypeFilter.AddOptions(new[]
                {
                    ItemSubTypeFilter.All,
                    ItemSubTypeFilter.Weapon,
                    ItemSubTypeFilter.Armor,
                    ItemSubTypeFilter.Belt,
                    ItemSubTypeFilter.Necklace,
                    ItemSubTypeFilter.Ring,
                    ItemSubTypeFilter.Food_HP,
                    ItemSubTypeFilter.Food_ATK,
                    ItemSubTypeFilter.Food_DEF,
                    ItemSubTypeFilter.Food_CRI,
                    ItemSubTypeFilter.Food_HIT,
                    ItemSubTypeFilter.FullCostume,
                    ItemSubTypeFilter.HairCostume,
                    ItemSubTypeFilter.EarCostume,
                    ItemSubTypeFilter.EyeCostume,
                    ItemSubTypeFilter.TailCostume,
                    ItemSubTypeFilter.Title,
                    ItemSubTypeFilter.Materials,
                }
                .Select(type => type.TypeToString(true))
                .ToList());
            itemSubTypeFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ItemSubTypeFilter) index;
                    }
                    catch
                    {
                        return ItemSubTypeFilter.All;
                    }
                })
                .Subscribe(filter =>
                {
                    SharedModel.itemSubTypeFilter = filter;
                    OnItemSubTypeFilterChanged();
                })
                .AddTo(gameObject);

            sortFilter.AddOptions(new[]
                {
                    ShopSortFilter.Class,
                    ShopSortFilter.CP,
                    ShopSortFilter.Price,
                }
                .Select(type => L10nManager.Localize($"UI_{type.ToString().ToUpper()}"))
                .ToList());
            sortFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ShopSortFilter) index;
                    }
                    catch
                    {
                        return ShopSortFilter.Class;
                    }
                })
                .Subscribe(filter =>
                {
                    SharedModel.sortFilter = filter;
                    OnSortFilterChanged();
                })
                .AddTo(gameObject);

            previousPageButton.OnClickAsObservable()
                .Subscribe(OnPreviousPageButtonClick)
                .AddTo(gameObject);
            nextPageButton.OnClickAsObservable()
                .Subscribe(OnNextPageButtonClick)
                .AddTo(gameObject);
        }

        public void Show()
        {
            itemSubTypeFilter.SetValueWithoutNotify(0);
            SharedModel.itemSubTypeFilter = 0;
            sortFilter.SetValueWithoutNotify(0);
            SharedModel.sortFilter = 0;

            ReactiveShopState.SellDigests
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
            foreach (var item in items)
            {
                item.Clear();
            }

            if (SharedModel is null)
            {
                return;
            }

            _filteredPageIndex = 0;
            UpdateViewWithFilteredPageIndex(SharedModel.Items.Value);
            refreshButton.gameObject.SetActive(false);
        }

        private async void UpdateViewWithFilteredPageIndex(IReadOnlyDictionary<int, List<ShopItem>> models)
        {
            var count = models?.Count ?? 0;
            if (count > _filteredPageIndex)
            {
                var items = models[_filteredPageIndex];
                var task = Task.Run(async () =>
                {
                    var itemBases = await Util.GetTradableItems(items);
                    foreach (var itemBase in itemBases)
                    {
                        if (itemBase is Equipment equipment)
                        {
                            var shopItem = items.FirstOrDefault(x => x.TradableId.Value.Equals(equipment.TradableId));
                            if(shopItem != null)
                            {
                                shopItem.ItemBase.Value = itemBase;
                            }
                        }
                    }
                    return true;
                });
                var result = await task;
                if (result)
                {

                    UpdateViewWithItems(items);
                }
            }
            else
            {
                UpdateViewWithItems(new List<ShopItem>());
            }

            if (_filteredPageIndex > 0)
            {
                previousPageButtonInteractableSwitch.SetSwitchOn();
            }
            else
            {
                previousPageButtonInteractableSwitch.SetSwitchOff();
            }

            if (_filteredPageIndex + 1 < count)
            {
                nextPageButtonInteractableSwitch.SetSwitchOn();
            }
            else
            {
                nextPageButtonInteractableSwitch.SetSwitchOff();
            }
        }

        private void UpdateViewWithItems(IEnumerable<ShopItem> viewModels)
        {
            using (var itemViews = items.GetEnumerator())
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

        private void OnPreviousPageButtonClick(Unit unit)
        {
            if (_filteredPageIndex == 0)
            {
                previousPageButtonInteractableSwitch.SetSwitchOff();
                return;
            }

            _filteredPageIndex--;
            nextPageButtonInteractableSwitch.SetSwitchOn();

            if (_filteredPageIndex == 0)
            {
                previousPageButtonInteractableSwitch.SetSwitchOff();
            }

            UpdateViewWithFilteredPageIndex(SharedModel.Items.Value);
        }

        private void OnNextPageButtonClick(Unit unit)
        {
            var count = SharedModel.Items.Value.Count;


            if (_filteredPageIndex + 1 >= count)
            {
                nextPageButtonInteractableSwitch.SetSwitchOff();
                return;
            }

            _filteredPageIndex++;
            previousPageButtonInteractableSwitch.SetSwitchOn();

            if (_filteredPageIndex + 1 == count)
            {
                nextPageButtonInteractableSwitch.SetSwitchOff();
            }

            UpdateViewWithFilteredPageIndex(SharedModel.Items.Value);
        }
    }
}
