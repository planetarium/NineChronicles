using System;
using System.Linq;
using System.Collections.Generic;
using Lib9c.Model.Order;
using Libplanet.Types.Assets;
using MarketService.Response;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
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

        private readonly List<ShopItem> _items = new();
        private readonly List<ShopItem> _selectedModels = new();
        private readonly List<ShopItemView> _itemViews = new();
        protected readonly ReactiveProperty<int> _page = new();
        private readonly List<IDisposable> _disposables = new();

        private Image _nextPageImage;
        private Image _previousPageImage;
        protected int _column;
        protected  int _row;
        private int _pageCount = 1;
        protected bool _isActive;

        protected Action<ShopItem> ClickItemAction;
        protected abstract void OnAwake();
        protected abstract void InitInteractiveUI();
        protected abstract void SubscribeToSearchConditions();
        protected abstract void OnClickItem(ShopItem item);
        protected abstract void Reset();

        protected abstract IEnumerable<ShopItem> GetSortedModels(List<ShopItem> items);

        protected virtual void UpdateView()
        {
            _selectedModels.Clear();
            _selectedModels.AddRange(GetSortedModels(_items));

            if (_column * _row > 0)
            {
                _pageCount = _selectedModels.Any()
                    ? _selectedModels.Count / (_column * _row) + 1
                    : 1;
            }

            UpdateExpired(Game.Game.instance.Agent.BlockIndex);
        }

        public void Show(
            ReactiveProperty<List<ItemProductResponseModel>> itemProducts,
            ReactiveProperty<List<FungibleAssetValueProductResponseModel>> fungibleAssetProducts,
            Action<ShopItem> clickItem)
        {
            InstantiateItemView();
            SetAction(clickItem);
            Set(itemProducts, fungibleAssetProducts);
            UpdateView();
            Reset();
        }

        private void Awake()
        {
            OnAwake();

            _items.Clear();

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

        private void OnEnable()
        {
            _isActive = true;
        }

        private void OnDisable()
        {
            _isActive = false;
        }

        protected virtual void UpdatePage(int page)
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
                go.SetActive(false);
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

        protected void Set(
            ReactiveProperty<List<ItemProductResponseModel>> itemProducts,
            ReactiveProperty<List<FungibleAssetValueProductResponseModel>> fungibleAssetProducts)
        {
            _disposables.DisposeAllAndClear();
            _items.Clear();

            if (itemProducts.Value is not null)
            {
                var itemSheet = TableSheets.Instance.ItemSheet;
                foreach (var product in itemProducts.Value)
                {
                    if (!ReactiveShopState.TryGetItemBase(product, out var itemBase))
                    {
                        continue;
                    }

                    AddItem(product, itemBase, itemSheet);
                }
            }

            if (fungibleAssetProducts.Value is not null)
            {
                foreach (var product in fungibleAssetProducts.Value)
                {
                    AddItem(product);
                }
            }

            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateExpired)
                .AddTo(_disposables);
        }

        private void AddItem(ItemProductResponseModel product, ItemBase itemBase, ItemSheet sheet)
        {
            _items.Add(CreateItem(product, itemBase, sheet));
        }

        private void AddItem(FungibleAssetValueProductResponseModel product)
        {
            var currency = Currency.Legacy(product.Ticker, 0, null);
            var fav = new FungibleAssetValue(currency, (int)product.Quantity, 0);
            _items.Add(CreateItem(product, fav));
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
            ItemProductResponseModel product,
            ItemBase item,
            ItemSheet sheet)
        {
            var grade = sheet[product.ItemId].Grade;
            var limit = item.ItemType != ItemType.Material &&
                        !Util.IsUsableItem(item);
            return new ShopItem(item, product, grade, limit);
        }

        private static ShopItem CreateItem(
            FungibleAssetValueProductResponseModel product,
            FungibleAssetValue fav)
        {
            var grade = Util.GetTickerGrade(product.Ticker);
            return new ShopItem(fav, product, grade, fav.MajorUnit <= 0);
        }

        private void UpdateExpired(long blockIndex)
        {
            foreach (var model in _selectedModels)
            {
                var isExpired = false;
                if (model.Product is not null && model.Product.Legacy)
                {
                    isExpired = model.Product.RegisteredBlockIndex + Order.ExpirationInterval - blockIndex <= 0;
                }

                model.Expired.Value = isExpired;
            }
        }

        protected void UpdateSelected(List<ShopItem> selectedItems)
        {
            foreach (var model in _selectedModels)
            {
                if (model.ItemBase is not null)
                {
                    model.Selected.Value = selectedItems
                        .Where(x => x.Product is not null)
                        .Any(x => x.Product.ProductId == model.Product.ProductId);
                }
                else
                {
                    model.Selected.Value = selectedItems
                        .Where(x => x.FungibleAssetProduct is not null)
                        .Any(x => x.FungibleAssetProduct.ProductId == model.FungibleAssetProduct.ProductId);
                }
            }
        }
    }
}
