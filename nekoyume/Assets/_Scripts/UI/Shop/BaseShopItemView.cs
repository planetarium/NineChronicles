using System;
using System.Linq;
using System.Collections.Generic;
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

namespace Nekoyume.UI.Module
{
    using UniRx;

    public abstract class BaseShopItemView : MonoBehaviour
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

        private readonly Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>> _items =
            new Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>>();

        private readonly List<ShopItemViewModel> _selectedModels = new List<ShopItemViewModel>();
        private readonly List<NewShopItemView> _itemViews = new List<NewShopItemView>();
        private readonly ReactiveProperty<int> _page = new ReactiveProperty<int>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private int _column = 0;
        private int _row = 0;
        private int _pageCount = 1;

        protected Action<ShopItemViewModel, RectTransform> ClickItemAction;

        protected abstract void OnAwake();
        protected abstract void OnClickItem(ShopItemViewModel item);
        protected abstract void Reset();

        protected abstract IEnumerable<ShopItemViewModel> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>> items);

        public void Show(ReactiveProperty<List<OrderDigest>> digests,
            Action<ShopItemViewModel, RectTransform> clickItem)
        {
            Reset();
            InstantiateItemView();
            SetAction(clickItem);
            Set(digests);
            UpdateView();
        }

        protected void UpdateView()
        {
            _selectedModels.Clear();
            _selectedModels.AddRange(GetSortedModels(_items));
            _pageCount = _selectedModels.Any()
                ? (_selectedModels.Count() / (_column * _row)) + 1
                : 1;
            _page.SetValueAndForceNotify(0);
            UpdateExpired(Game.Game.instance.Agent.BlockIndex);
        }

        private void Awake()
        {
            foreach (var filter in ItemSubTypeFilterExtension.Filters)
            {
                _items.Add(filter, new List<ShopItemViewModel>());
            }

            OnAwake();

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
                var view = go.GetComponent<NewShopItemView>();
                _itemViews.Add(view);
            }

            for (var i = 0; i < _row; i++)
            {
                Instantiate(shelfPrefab, shelfContainer);
            }
        }

        private void SetAction(Action<ShopItemViewModel, RectTransform> clickItem)
        {
            ClickItemAction = clickItem;
        }

        private void Set(IObservable<List<OrderDigest>> orderDigests)
        {
            _disposables.DisposeAllAndClear();
            orderDigests.Subscribe(digests =>
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

        private static ShopItemViewModel CreateItem(ItemBase item, OrderDigest digest,
            ItemSheet sheet)
        {
            var itemId = digest.ItemId;
            var grade = sheet[itemId].Grade;
            var limit = item.ItemType != ItemType.Material && !Util.IsUsableItem(itemId);
            return new ShopItemViewModel(item, digest, grade, limit);
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
