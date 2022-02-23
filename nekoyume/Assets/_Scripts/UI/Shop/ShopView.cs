using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Lib9c.Model.Order;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
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

        private readonly Dictionary<ItemSubTypeFilter, List<ShopItem>> _items =
            new Dictionary<ItemSubTypeFilter, List<ShopItem>>();

        private readonly List<ShopItem> _selectedModels = new List<ShopItem>();
        private readonly List<ShopItemView> _itemViews = new List<ShopItemView>();
        private readonly ReactiveProperty<int> _page = new ReactiveProperty<int>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private int _column = 0;
        private int _row = 0;
        private int _pageCount = 1;

        protected Action<ShopItem, RectTransform> ClickItemAction;
        protected abstract void OnAwake();
        protected abstract void InitInteractiveUI();
        protected abstract void SubscribeToSearchConditions();
        protected abstract void OnClickItem(ShopItem item);
        protected abstract void Reset();

        protected abstract IEnumerable<ShopItem> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItem>> items);

        protected virtual void UpdateView()
        {
            _selectedModels.Clear();
            _selectedModels.AddRange(GetSortedModels(_items));
            _pageCount = _selectedModels.Any()
                ? (_selectedModels.Count() / (_column * _row)) + 1
                : 1;
            _page.SetValueAndForceNotify(0);
            UpdateExpired(Game.Game.instance.Agent.BlockIndex);
        }

        public void Show(ReactiveProperty<List<OrderDigest>> digests,
            Action<ShopItem, RectTransform> clickItem)
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

            nextPageButton.onClick.AddListener(() =>
            {
                _page.Value = math.min(_pageCount - 1, _page.Value + 1);
            });

            previousPageButton.onClick.AddListener(() =>
            {
                _page.Value = math.max(0, _page.Value - 1);
            });

            _page.ObserveOnMainThread().Subscribe(UpdatePage).AddTo(gameObject);
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
                var view = go.GetComponent<ShopItemView>();
                _itemViews.Add(view);
            }

            for (var i = 0; i < _row; i++)
            {
                Instantiate(shelfPrefab, shelfContainer);
            }
        }

        private void SetAction(Action<ShopItem, RectTransform> clickItem)
        {
            ClickItemAction = clickItem;
        }

        private void Set(IObservable<List<OrderDigest>> orderDigests)
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

                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var digest in digests)
                {
                    AddItem(digest, itemSheet);
                }

                UpdateView();
            }).AddTo(_disposables);
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateExpired).AddTo(_disposables);
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

        private static ShopItem CreateItem(ItemBase item, OrderDigest digest,
            ItemSheet sheet)
        {
            var itemId = digest.ItemId;
            var grade = sheet[itemId].Grade;
            var limit = item.ItemType != ItemType.Material && !Util.IsUsableItem(itemId);
            return new ShopItem(item, digest, grade, limit);
        }

        private void UpdateExpired(long blockIndex)
        {
            foreach (var model in _selectedModels)
            {
                var isExpired = model.OrderDigest.ExpiredBlockIndex - blockIndex <= 0;
                model.Expired.Value = isExpired;
            }
        }
    }
}
