using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Order;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    using UniRx;

    public class SellItemView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown itemSubTypeFilter = null;

        [SerializeField]
        private TMP_Dropdown sortFilter = null;

        [SerializeField]
        private Button nextPageButton;

        [SerializeField]
        private Button previousPageButton;

        [SerializeField]
        private TextMeshProUGUI pageText;

        [SerializeField]
        private GridLayoutGroup gridLayoutGroup;

        [SerializeField]
        private Transform shelfContainer;

        [SerializeField]
        private GameObject shelfPrefab;

        [SerializeField]
        private GameObject shopPrefab;

        private readonly Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>> _items =
            new Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>>();
        private readonly List<NewShopItemView> _itemViews = new List<NewShopItemView>();
        private readonly List<ShopItemViewModel> _selectedModels = new List<ShopItemViewModel>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private Action<ShopItemViewModel, RectTransform> _onClickItem;
        private ItemSubTypeFilter _activeSubTypeFilter = ItemSubTypeFilter.All;
        private ShopSortFilter _activeSortFilter = ShopSortFilter.Class;
        private readonly ReactiveProperty<int> _page = new ReactiveProperty<int>();
        private ShopItemViewModel _selectedModel;

        private int _column = 0;
        private int _row = 0;
        private int _pageCount = 1;

        private void Awake()
        {
            var filters = ItemSubTypeFilterExtension.ItemSubTypeFilters;
            foreach (var filter in filters)
            {
                _items.Add(filter, new List<ShopItemViewModel>());
            }

            itemSubTypeFilter.AddOptions(filters
                .Select(type => type.TypeToString(true))
                .ToList());
            itemSubTypeFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ItemSubTypeFilter)index;
                    }
                    catch
                    {
                        return ItemSubTypeFilter.All;
                    }
                })
                .Subscribe(filter =>
                {
                    _activeSubTypeFilter = filter;
                    UpdateView();
                })
                .AddTo(gameObject);

            sortFilter.AddOptions(ShopSortFilterExtension.ShopSortFilters
                .Select(type => L10nManager.Localize($"UI_{type.ToString().ToUpper()}"))
                .ToList());
            sortFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ShopSortFilter)index;
                    }
                    catch
                    {
                        return ShopSortFilter.Class;
                    }
                })
                .Subscribe(filter =>
                {
                    _activeSortFilter = filter;
                    UpdateView();
                })
                .AddTo(gameObject);

            nextPageButton.onClick.AddListener(() =>
            {
                _page.Value = math.min(_pageCount - 1, _page.Value + 1);
            });

            previousPageButton.onClick.AddListener(() =>
            {
                _page.Value = math.max(0, _page.Value - 1);
            });

            _page.Subscribe(UpdatePage).AddTo(gameObject);
        }

        private void InstantiateItemView()
        {
            var cell = gridLayoutGroup.cellSize;
            var spacing = gridLayoutGroup.spacing;
            var rect = gridLayoutGroup.GetComponent<RectTransform>().rect;
            var column = Util.GetGridItemCount(cell.x, spacing.x, rect.width);
            var row = Util.GetGridItemCount(cell.y, spacing.y, rect.height);
            if (column == _column && row == _row)
            {
                return;
            }

            DestroyChildren(shelfContainer);
            DestroyChildren(gridLayoutGroup.transform);

            _column = column;
            _row = row;
            var sum = _column * _row;

            _itemViews.Clear();
            for (var i = 0; i < sum; i++)
            {
                var go = Instantiate(shopPrefab, gridLayoutGroup.transform);
                var view = go.GetComponent<NewShopItemView>();
                _itemViews.Add(view);
            }

            for (var i = 0; i < _row; i++)
            {
                Instantiate(shelfPrefab, shelfContainer);
            }
        }


        private void UpdateView()
        {
            _selectedModels.Clear();
            _selectedModels.AddRange(GetSortedModels());
            _pageCount = _selectedModels.Any()
                ? (_selectedModels.Count() / (_column * _row)) + 1
                : 1;
            _page.SetValueAndForceNotify(0);
            UpdateExpired(Game.Game.instance.Agent.BlockIndex);
        }

        private void UpdateExpired(long blockIndex)
        {
            foreach (var model in _selectedModels)
            {
                var isExpired = model.OrderDigest.ExpiredBlockIndex - blockIndex <= 0;
                model.Expired.Value = isExpired;
            }
        }

        private void UpdatePage(int page)
        {
            var index = page * _itemViews.Count();
            foreach (var view in _itemViews)
            {
                if (index < _selectedModels.Count())
                {
                    view.gameObject.SetActive(true);
                    view.Set(_selectedModels[index], OnClickItem);
                }
                else
                {
                    view.gameObject.SetActive(false);
                }

                index++;
            }

            pageText.text = $"{page + 1} / {_pageCount}";
        }

        private void OnClickItem(ShopItemViewModel item)
        {
            if (_selectedModel == null)
            {
                _selectedModel = item;
                _selectedModel.Selected.SetValueAndForceNotify(true);
                _onClickItem?.Invoke(_selectedModel, _selectedModel.View); // Show tooltip popup
            }
            else
            {
                if (_selectedModel.Equals(item))
                {
                    _selectedModel.Selected.SetValueAndForceNotify(false);
                    _selectedModel = null;
                }
                else
                {
                    _selectedModel.Selected.SetValueAndForceNotify(false);
                    _selectedModel = item;
                    _selectedModel.Selected.SetValueAndForceNotify(true);
                    _onClickItem?.Invoke(_selectedModel, _selectedModel.View); // Show tooltip popup
                }
            }
        }

        private void DestroyChildren(Transform parent)
        {
            var children = new List<GameObject>();
            for (var i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i).gameObject);
            }

            foreach (var child in children)
            {
                DestroyImmediate(child);
            }
        }

        private void SetAction(Action<ShopItemViewModel, RectTransform> clickItem)
        {
            _onClickItem = clickItem;
        }

        private void Set()
        {
            _disposables.DisposeAllAndClear();
            ReactiveShopState.SellDigest.Subscribe(digests =>
            {
                foreach (var item in _items)
                {
                    item.Value.Clear();
                }

                if (digests is null)
                {
                    return;
                }

                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var digest in digests)
                {
                    AddItem(digest, itemSheet);
                }

                UpdateView();
            }).AddTo(_disposables);
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateExpired)
                .AddTo(_disposables);
        }

        private IEnumerable<ShopItemViewModel> GetSortedModels()
        {
            var items = _items[_activeSubTypeFilter];
            return _activeSortFilter switch
            {
                ShopSortFilter.CP => items.OrderByDescending(x => x.OrderDigest.CombatPoint)
                    .ToList(),
                ShopSortFilter.Price => items.OrderByDescending(x => x.OrderDigest.Price).ToList(),
                ShopSortFilter.Class => items.OrderByDescending(x => x.Grade)
                    .ThenByDescending(x => x.ItemBase.ItemType).ToList(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void AddItem(OrderDigest digest, ItemSheet sheet)
        {
            if (!ReactiveShopState.TryGetShopItem(digest, out var itemBase))
            {
                return;
            }

            var filter = ItemSubTypeFilterExtension.GetItemSubTypeFilter(sheet, digest.ItemId);
            var model = CreateItem(itemBase, digest, sheet);
            _items[filter].Add(model);
            _items[ItemSubTypeFilter.All].Add(model);
        }

        private static ShopItemViewModel CreateItem(ItemBase item, OrderDigest digest,
            ItemSheet sheet)
        {
            var itemId = digest.ItemId;
            var grade = sheet[itemId].Grade;
            var limit = item.ItemType != ItemType.Material && !Util.IsUsableItem(itemId);
            return new ShopItemViewModel(item, digest, grade, limit);
        }

        public void SetSell(Action<ShopItemViewModel, RectTransform> clickItem)
        {
            InstantiateItemView();
            SetAction(clickItem);
            Set();
            UpdateView();
        }

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
        }
    }
}
