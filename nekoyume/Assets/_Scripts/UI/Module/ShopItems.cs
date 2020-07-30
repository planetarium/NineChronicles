using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    public class ShopItems : MonoBehaviour
    {
        public List<ShopItemView> items;
        public TouchHandler refreshButtonTouchHandler;
        public RefreshButton refreshButton;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public Model.ShopItems SharedModel { get; private set; }

        #region Mono

        private void Awake()
        {
            SharedModel = new Model.ShopItems();
            SharedModel.State.Subscribe(_ => UpdateView()).AddTo(gameObject);
            SharedModel.OtherProducts.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(gameObject);
            SharedModel.OtherProducts.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(gameObject);
            SharedModel.CurrentAgentsProducts.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(gameObject);
            SharedModel.CurrentAgentsProducts.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(gameObject);

            refreshButtonTouchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel?.ResetOtherProducts();
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            ReactiveShopState.Items.Subscribe(ResetProducts)
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

        public void ResetProducts(IDictionary<Address, List<Nekoyume.Model.Item.ShopItem>> products)
        {
            SharedModel?.ResetProducts(products);
        }

        private void UpdateView()
        {
            if (SharedModel is null)
            {
                foreach (var item in items)
                {
                    item.Clear();
                }

                return;
            }

            switch (SharedModel.State.Value)
            {
                case Shop.StateType.Buy:
                    UpdateViewWithItems(SharedModel.OtherProducts);
                    refreshButton.gameObject.SetActive(true);
                    refreshButton.PlayAnimation(NPCAnimation.Type.Appear);
                    break;
                case Shop.StateType.Sell:
                    UpdateViewWithItems(SharedModel.CurrentAgentsProducts);
                    refreshButton.gameObject.SetActive(false);
                    break;
            }
        }

        private void UpdateViewWithItems(IEnumerable<ShopItem> models)
        {
            using (var itemViews = items.GetEnumerator())
            using (var itemModels = models.GetEnumerator())
            {
                while (itemViews.MoveNext())
                {
                    if (itemViews.Current is null)
                        continue;

                    if (!itemModels.MoveNext())
                    {
                        itemViews.Current.Clear();
                        continue;
                    }

                    itemViews.Current.SetData(itemModels.Current);
                }
            }
        }
    }
}
