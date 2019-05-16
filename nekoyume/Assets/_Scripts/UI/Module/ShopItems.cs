using System;
using System.Collections.Generic;
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

        private Model.Shop.State _state;
        private Model.ShopItems _data;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        
        #region Mono
        
        private void Awake()
        {
            this.ComponentFieldsNotNullTest();

            refreshButton.onClick.AsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                _data?.onClickRefresh.OnNext(_data);
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

            _data = data;
            
            UpdateView();
        }

        public void Clear()
        {
            _data = null;
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
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
                    UpdateViewWithItems(_data.buyItems);
                    refreshButton.gameObject.SetActive(true);
                    break;
                case Model.Shop.State.Sell:
                    UpdateViewWithItems(_data.sellItems);
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
