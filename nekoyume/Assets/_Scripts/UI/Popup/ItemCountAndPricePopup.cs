using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ItemCountAndPricePopup : ItemCountPopup<Model.ItemCountAndPricePopup>
    {
        [SerializeField]
        private TMP_InputField priceInputField = null;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            priceInputField.onValueChanged.AsObservable()
                .Subscribe(_ =>
                {
                    if (!int.TryParse(priceInputField.text, NumberStyles.Number,
                        new NumberFormatInfo(),
                        out var price))
                    {
                        price = 0;
                    }

                    _data.Price.Value = Math.Max(Model.Shop.MinimumPrice, price);
                }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        protected override void SetData(Model.ItemCountAndPricePopup data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(data);
            _data.Price.Subscribe(value => priceInputField.text = value.ToString("N0"))
                .AddTo(_disposablesForSetData);
            _data.PriceInteractable.Subscribe(value => priceInputField.interactable = value)
                .AddTo(_disposablesForSetData);
            priceInputField.Select();
        }

        protected override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
        }
    }
}
