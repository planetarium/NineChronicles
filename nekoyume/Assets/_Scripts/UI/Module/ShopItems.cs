using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ShopItems : MonoBehaviour
    {
        public List<ShopItemView> items;
        public Button refreshButton;
        public Text refreshButtonText;

        private Model.Shop.State _state;
        public Model.ShopItems data;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();
        
        #region Mono
        
        private void Awake()
        {
            this.ComponentFieldsNotNullTest();

            refreshButtonText.text = LocalizationManager.Localize("UI_REFRESH");

            refreshButton.onClick.AsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                data?.onRefresh.OnNext(data);
            }).AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion
        
        public void SetState(Model.Shop.State state)
        {
            _state = state;
            UpdateView();
        }
        
        public void SetData(Model.ShopItems data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            this.data = data;
            this.data.products.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            this.data.products.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            this.data.registeredProducts.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            this.data.registeredProducts.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            
            UpdateView();
        }

        public void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            data = null;
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(data, null))
            {
                foreach (var item in items)
                {
                    item.Clear();
                }
                
                return;
            }
            
            switch (_state)
            {
                case Model.Shop.State.Buy:
                    UpdateViewWithItems(data.products);
                    refreshButton.gameObject.SetActive(true);
                    break;
                case Model.Shop.State.Sell:
                    UpdateViewWithItems(data.registeredProducts);
                    refreshButton.gameObject.SetActive(false);
                    break;
            }
        }

        private void UpdateViewWithItems(IEnumerable<ShopItem> data)
        {
            using (var uiItems = items.GetEnumerator())
            using (var dataItems = data.GetEnumerator())
            {
                while (uiItems.MoveNext())
                {
                    if (ReferenceEquals(uiItems.Current, null))
                    {
                        continue;
                    }
                    
                    if (!dataItems.MoveNext())
                    {
                        uiItems.Current.Clear();
                        continue;
                    }
                        
                    uiItems.Current.SetData(dataItems.Current);
                }
            }
        }

        public ShopItemView GetByProductId(Guid id)
        {
            foreach (var shopItemView in items)
            {
                if (shopItemView.Model == null)
                {
                    break;
                }
                
                if (shopItemView.Model.productId.Value == id)
                {
                    return shopItemView;
                }
            }
            
            return null;
        }
    }
}
