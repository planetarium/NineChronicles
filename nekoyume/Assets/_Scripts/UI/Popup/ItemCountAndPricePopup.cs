using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class ItemCountAndPricePopup : ItemCountPopup<Model.ItemCountAndPricePopup>
    {
        public TMP_InputField priceInputField;

        private Model.ItemCountAndPricePopup _data;
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            priceInputField.onValueChanged.AsObservable()
                .Subscribe(_ =>
                {
                    var priceString = _.Replace(",", "");
                    if (!int.TryParse(priceString, out var price) || price < 0)
                    {
                        price = 0;
                    }
                    _data.Price.Value = price;
                }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        public override void Pop(Model.ItemCountAndPricePopup data)
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

            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.PriceInteractable.Subscribe(interactable => priceInputField.interactable = interactable)
                .AddTo(_disposablesForSetData);

            UpdateView();
        }

        private void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = null;

            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
            {
                return;
            }

            priceInputField.text = _data.Price.Value.ToString("N0");
            priceInputField.interactable = _data.PriceInteractable.Value;
            priceInputField.Select();
        }

        public void OnValueChanged()
        {
            int.TryParse(priceInputField.text, NumberStyles.Number, new NumberFormatInfo(), out var price);
            priceInputField.text = price.ToString("N0");
        }
    }
}
