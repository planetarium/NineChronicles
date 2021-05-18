using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemCountableAndPricePopup : ItemCountPopup<Model.ItemCountableAndPricePopup>
    {
        [SerializeField] private TMP_InputField priceInputField = null;
        [SerializeField] private TMP_InputField countInputField = null;

        [SerializeField] private Button addCountButton = null;
        [SerializeField] private Button addMaximumCountButton = null;
        [SerializeField] private Button removeCountButton = null;

        [SerializeField] private List<Button> addPriceButton = null;

        [SerializeField] private TextMeshProUGUI totalPrice;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private const int DefaultPrice = 10;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            // price
            priceInputField.onValueChanged.AsObservable().Subscribe(_ =>
            {
                var price = InputFieldValueToField(priceInputField);
                _data.OnChangePrice.OnNext(price);
            }).AddTo(_disposablesForAwake);

            for (int i = 0; i < addPriceButton.Count; i++)
            {
                int digit = i;
                addPriceButton[i].OnClickAsObservable().Subscribe(_ =>
                {
                    var price = InputFieldValueToField(priceInputField) +
                                (int) Mathf.Pow(DefaultPrice, digit + 1);
                    _data.OnChangePrice.OnNext(price);
                }).AddTo(_disposablesForAwake);
            }

            // count
            countInputField.onValueChanged.AsObservable().Subscribe(_ =>
            {
                var maxCount = _data.Item.Value.MaxCount.Value;
                var count = InputFieldValueToField(countInputField);
                _data.OnChangeCount.OnNext(Mathf.Clamp(count, 1, maxCount));
            }).AddTo(_disposablesForAwake);

            addCountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var maxCount = _data.Item.Value.MaxCount.Value;
                var count = InputFieldValueToField(countInputField) + 1;
                _data.OnChangeCount.OnNext(Math.Min(count, maxCount));
            }).AddTo(_disposablesForAwake);

            addMaximumCountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var maxCount = _data.Item.Value.MaxCount.Value;
                _data.OnChangeCount.OnNext(maxCount);
            }).AddTo(_disposablesForAwake);

            removeCountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var count = InputFieldValueToField(countInputField) - 1;
                _data.OnChangeCount.OnNext(Math.Max(count, 1));
            }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        protected override void SetData(Model.ItemCountableAndPricePopup data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(data);

            _data.Count.Value = 1;
            _data.Price.Subscribe(value => priceInputField.text = value.GetQuantityString())
                .AddTo(_disposablesForSetData);

            _data.Count.Subscribe(value => countInputField.text = value.ToString())
                .AddTo(_disposablesForSetData);

            _data.TotalPrice.Subscribe(value =>
                {
                    totalPrice.text = value.GetQuantityString();
                    var price = Convert.ToInt32(_data.Price.Value.GetQuantityString());
                    var count = _data.Count.Value;
                    submitButton.SetSubmittable(!(price < Model.Shop.MinimumPrice || count < 0));
                })
                .AddTo(_disposablesForSetData);

            priceInputField.Select();
            countInputField.Select();
        }

        protected override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
        }

        private int InputFieldValueToField(TMP_InputField inputField)
        {
            if (!int.TryParse(inputField.text, NumberStyles.Number, CultureInfo.CurrentCulture,
                out var price))
            {
                price = 0;
            }

            return price;
        }
    }
}
