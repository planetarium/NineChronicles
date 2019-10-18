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
        
        private readonly List<IDisposable> _disposablesAtSetData = new List<IDisposable>();
        
        #region Mono
        
        private void Awake()
        {
            this.ComponentFieldsNotNullTest();

            refreshButtonText.text = LocalizationManager.Localize("UI_REFRESH");

            refreshButton.onClick.AsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                data?.OnRefresh.OnNext(data);
            }).AddTo(gameObject);
        }

        #endregion
        
        public void SetState(Shop.StateType stateType)
        {
            _stateType = stateType;
            UpdateView();
        }
        
        public void SetData(Model.ShopItems data)
        {
            if (data is null)
            {
                Clear();
                return;
            }

            _disposablesAtSetData.DisposeAllAndClear();
            this.data = data;
            this.data.Products.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(_disposablesAtSetData);
            this.data.Products.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(_disposablesAtSetData);
            this.data.RegisteredProducts.ObserveAdd().Subscribe(_ => UpdateView()).AddTo(_disposablesAtSetData);
            this.data.RegisteredProducts.ObserveRemove().Subscribe(_ => UpdateView()).AddTo(_disposablesAtSetData);
            
            UpdateView();
        }

        public void Clear()
        {
            _disposablesAtSetData.DisposeAllAndClear();
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
