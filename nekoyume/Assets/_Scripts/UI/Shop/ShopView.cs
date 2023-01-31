using System;
using System.Linq;
using System.Collections.Generic;
using Lib9c.Model.Order;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public abstract class ShopView : MonoBehaviour, IShopView
    {
        [SerializeField]
        protected Button nextPageButton;

        [SerializeField]
        protected Button previousPageButton;

        [SerializeField]
        protected TextMeshProUGUI pageText;

        [SerializeField]
        private GridLayoutGroup gridLayoutGroup;

        [SerializeField]
        private Transform shelfContainer;

        [SerializeField]
        private GameObject shelfPrefab;

        [SerializeField]
        private GameObject shopPrefab;

        private readonly Dictionary<ItemSubTypeFilter, List<ShopItem>> _items = new();
        private readonly List<ShopItem> _selectedModels = new();
        private readonly List<ShopItemView> _itemViews = new();
        private readonly ReactiveProperty<int> _page = new();
        private readonly List<IDisposable> _disposables = new();

        private Image _nextPageImage;
        private Image _previousPageImage;
        private int _column;
        private int _row;
        private int _pageCount = 1;

        protected Action<ShopItem> ClickItemAction;
        protected abstract void OnAwake();
        protected abstract void InitInteractiveUI();
        protected abstract void SubscribeToSearchConditions();
        protected abstract void OnClickItem(ShopItem item);
        protected abstract void Reset();

        protected abstract IEnumerable<ShopItem> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItem>> items);

        protected virtual void UpdateView(bool resetPage = true, int page = 0)
        {
            _selectedModels.Clear();
            _selectedModels.AddRange(GetSortedModels(_items));
            _pageCount = _selectedModels.Any()
                ? _selectedModels.Count / (_column * _row) + 1
                : 1;
            if (resetPage)
            {
                _page.SetValueAndForceNotify(page);
            }

            UpdateExpired(Game.Game.instance.Agent.BlockIndex);
        }

        public void Show(ReactiveProperty<List<ItemProductModel>> digests,
            Action<ShopItem> clickItem)
        {
            Reset();
            InstantiateItemView();
            SetAction(clickItem);
            Set(digests);
            UpdateView();
        }

        private void Awake()
        {
            OnAwake();

            foreach (var filter in ItemSubTypeFilterExtension.Filters)
            {
                _items.Add(filter, new List<ShopItem>());
            }

            InitInteractiveUI();
            SubscribeToSearchConditions();

            _nextPageImage = nextPageButton.GetComponent<Image>();
            nextPageButton.onClick.AddListener(() =>
            {
                _page.Value = math.min(_pageCount - 1, _page.Value + 1);
            });

            _previousPageImage = previousPageButton.GetComponent<Image>();
            previousPageButton.onClick.AddListener(() =>
            {
                _page.Value = math.max(0, _page.Value - 1);
            });

            _page.ObserveOnMainThread().Subscribe(UpdatePage).AddTo(gameObject);
        }

        private void UpdatePage(int page)
        {
            var index = page * _itemViews.Count;
            foreach (var view in _itemViews)
            {
                if (index < _selectedModels.Count)
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

            pageText.text = $"{page + 1}";
            UpdatePageButtonImage();
        }

        private void UpdatePageButtonImage()
        {
            previousPageButton.gameObject.SetActive(_pageCount > 1);
            nextPageButton.gameObject.SetActive(_pageCount > 1);

            previousPageButton.interactable = _page.Value != 0;
            _previousPageImage.color = previousPageButton.interactable
                ? previousPageButton.colors.normalColor
                : previousPageButton.colors.disabledColor;

            nextPageButton.interactable = _page.Value != _pageCount - 1;
            _nextPageImage.color = nextPageButton.interactable
                ? nextPageButton.colors.normalColor
                : nextPageButton.colors.disabledColor;
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
            _itemViews.Clear();
            var sum = _column * _row;
            for (var i = 0; i < sum; i++)
            {
                var go = Instantiate(shopPrefab, gridLayoutGroup.transform);
                var view = go.GetComponent<ShopItemView>();
                _itemViews.Add(view);
            }

            for (var i = 0; i < _row; i++)
            {
                Instantiate(shelfPrefab, shelfContainer);
            }
        }

        private void SetAction(Action<ShopItem> clickItem)
        {
            ClickItemAction = clickItem;
        }

        private void Set(ReactiveProperty<List<ItemProductModel>> orderDigests)
        {
            _disposables.DisposeAllAndClear();
            orderDigests.ObserveOnMainThread().Subscribe(digests =>
            {
                foreach (var item in _items)
                {
                    item.Value.Clear();
                }

                if (digests is null)
                {
                    return;
                }

                var itemSheet = TableSheets.Instance.ItemSheet;
                foreach (var digest in digests)
                {
                    AddItem(digest, itemSheet);
                }

                UpdateView(page: _page.Value);
            }).AddTo(_disposables);
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateExpired)
                .AddTo(_disposables);
        }

        private void AddItem(ItemProductModel digest, ItemSheet sheet)
        {
            if (!ReactiveShopState.TryGetShopItem(digest, out var itemBase))
            {
                return;
            }

            var model = CreateItem(itemBase, digest, sheet);
            var filters = ItemSubTypeFilterExtension.GetItemSubTypeFilter(digest.ItemId);
            foreach (var filter in filters)
            {
                if (!_items.ContainsKey(filter))
                {
                    _items.Add(filter, new List<ShopItem>());
                }

                _items[filter].Add(model);
                _items[ItemSubTypeFilter.All].Add(model);
            }
        }

        private static void DestroyChildren(Transform parent)
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

        private static ShopItem CreateItem(
            ItemBase item,
            ItemProductModel digest,
            ItemSheet sheet)
        {
            var grade = sheet[digest.ItemId].Grade;
            // var limit = item.ItemType != ItemType.Material &&
            //             !Util.IsUsableItem(item);
            var limit = false;
            return new ShopItem(item, digest, grade, limit);
        }

        private void UpdateExpired(long blockIndex)
        {
            foreach (var model in _selectedModels)
            {
                // var isExpired = model.OrderDigest.ExpiredBlockIndex - blockIndex <= 0;
                var isExpired = false;
                model.Expired.Value = isExpired;
            }
        }

        public void SetLoading(List<ItemProductModel> digests, bool isLoading = true)
        {
            var items = _items[ItemSubTypeFilter.All];
            foreach (var digest in digests)
            {
                var item = items.Find(x => x.OrderDigest.ProductId == digest.ProductId);
                if (item is null)
                {
                    continue;
                }

                item.Loading.Value = isLoading;
            }
        }
    }
}
