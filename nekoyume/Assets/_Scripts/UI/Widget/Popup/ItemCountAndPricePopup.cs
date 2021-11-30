using System;
using System.Collections.Generic;
using System.Globalization;
using Libplanet.Assets;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Module;
    using UniRx;

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
                        CultureInfo.CurrentCulture,
                        out var price))
                    {
                        price = 0;
                    }

                    var isBelowMinimumPrice = price < Model.Shop.MinimumPrice;
                    submitButton.SetState(isBelowMinimumPrice ?
                        ConditionalButton.State.Conditional : ConditionalButton.State.Normal);

                    _data.Price.Value =
                        new FungibleAssetValue(_data.Price.Value.Currency, price, 0);
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
            _data.Price.Subscribe(value => priceInputField.text = value.GetQuantityString())
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
