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

        private Shop.StateType _stateType;
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
                data?.OnRefresh.OnNext(data);
            }).AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion
        
        public void SetState(Shop.StateType stateType)
        {
            _stateType = stateType;
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
            this.data.Products.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            this.data.Products.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            this.data.RegisteredProducts.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            this.data.RegisteredProducts.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            
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
            
            switch (_stateType)
            {
                case Shop.StateType.Buy:
                    UpdateViewWithItems(data.Products);
                    refreshButton.gameObject.SetActive(true);
                    break;
                case Shop.StateType.Sell:
                    UpdateViewWithItems(data.RegisteredProducts);
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
    }
}
