using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ShopItems : MonoBehaviour
    {
        public SimpleCountableItemView[] items;
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
            foreach (var item in items)
            { 
                item.Clear();
            }

            _data = null;
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
            {
                return;
            }
            
            //
        }
    }
}
