using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemCountAndPricePopup : ItemCountPopup<Model.ItemCountAndPricePopup>
    {
        public InputField priceInputField;
        
        private Model.ItemCountAndPricePopup _data;
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        
        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            this.ComponentFieldsNotNullTest();

            priceInputField.onValueChanged.AsObservable()
                .Subscribe(_ =>
                {
                    if (!int.TryParse(_, out var price))
                    {
                        throw new InvalidCastException();
                    }
                    _data.price.Value = price;
                }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }
        
        #endregion

        public void Pop(Model.ItemCountAndPricePopup data)
        {
            base.Pop(data);
            
            if (ReferenceEquals(data, null))
            {
                return;
            }

            SetData(data);
        }

        private void SetData(Model.ItemCountAndPricePopup data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _data = data;
        }
        
        private void Clear()
        {
            _data = null;
        }
    }
}
