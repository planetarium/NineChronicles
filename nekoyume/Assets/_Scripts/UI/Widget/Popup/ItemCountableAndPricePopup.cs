using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class ItemCountableAndPricePopup : ItemCountPopup<Model.ItemCountableAndPricePopup>
    {
        [SerializeField] private ConditionalCostButton reregisterButton = null;
        [SerializeField] private Button overrideCancelButton;

        [SerializeField] private TMP_InputField priceInputField = null;
        [SerializeField] private TMP_InputField countInputField = null;

        [SerializeField] private Button addCountButton = null;
        [SerializeField] private Button addMaximumCountButton = null;
        [SerializeField] private Button removeCountButton = null;
        [SerializeField] private Button resetPriceButton = null;
        [SerializeField] private Button notificationButton = null;
        [SerializeField] private List<Button> addPriceButton = null;

        [SerializeField] private TextMeshProUGUI unitPrice;
        [SerializeField] private GameObject unitPriceInvalidText;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private const int DefaultPrice = 10;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (overrideCancelButton != null)
            {
                overrideCancelButton.onClick.AddListener(() =>
                {
                    _data?.OnClickCancel.OnNext(_data);
                });
                CloseWidget = overrideCancelButton.onClick.Invoke;
            }

            countInputField.onEndEdit.AsObservable().Subscribe(_ =>
            {
                if (countInputField.text.Equals(string.Empty) ||
                    countInputField.text.Equals("0"))
                {
                    countInputField.text = string.Empty;
                    _data.OnChangeCount.OnNext(0);
                }
                else
                {
                    var maxCount = 1;
                    if (_data.Item is { Value: { } })
                    {
                        maxCount = _data.Item.Value.MaxCount.Value;
                    }
                    var count = InputFieldValueToValue<int>(countInputField);
                    var result = Mathf.Clamp(count, 1, maxCount);
                    countInputField.text = result.ToString();
                    _data.OnChangeCount.OnNext(result);
                }

            }).AddTo(_disposablesForAwake);

            addCountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var maxCount = _data.Item.Value.MaxCount.Value;
                var count = InputFieldValueToValue<int>(countInputField) + 1;
                _data.OnChangeCount.OnNext(Math.Min(count, maxCount));
            }).AddTo(_disposablesForAwake);

            addMaximumCountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var maxCount = _data.Item.Value.MaxCount.Value;
                _data.OnChangeCount.OnNext(maxCount);
            }).AddTo(_disposablesForAwake);

            removeCountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var count = InputFieldValueToValue<int>(countInputField) - 1;
                _data.OnChangeCount.OnNext(Math.Max(count, 1));
            }).AddTo(_disposablesForAwake);

            // price
            priceInputField.onEndEdit.AsObservable().Subscribe(_ =>
            {
                if (priceInputField.text.Length > 0 &&
                    priceInputField.text.Length == priceInputField.caretPosition &&
                    priceInputField.text[priceInputField.caretPosition - 1].Equals('.'))
                {
                    return;
                }

                if (priceInputField.text.Equals(string.Empty) ||
                    priceInputField.text.Equals("0"))
                {
                    priceInputField.text = string.Empty;
                    _data.OnChangePrice.OnNext(0);
                }
                else
                {
                    var price = InputFieldValueToValue<decimal>(priceInputField);
                    _data.OnChangePrice.OnNext(price);
                }

            }).AddTo(_disposablesForAwake);

            resetPriceButton.OnClickAsObservable()
                .Subscribe(_ => _data.OnChangePrice.OnNext(Model.Shop.MinimumPrice));

            for (int i = 0; i < addPriceButton.Count; i++)
            {
                int digit = i;
                addPriceButton[i].OnClickAsObservable().Subscribe(_ =>
                {
                    var price = InputFieldValueToValue<decimal>(priceInputField) +
                                (int) Mathf.Pow(DefaultPrice, digit);
                    _data.OnChangePrice.OnNext(price);
                }).AddTo(_disposablesForAwake);
            }

            reregisterButton.Text = L10nManager.Localize("UI_REREGISTER");
            reregisterButton.OnClickSubject
                .Subscribe(_ =>
                {
                    _data?.OnClickReregister.OnNext(_data);
                })
                .AddTo(_disposablesForAwake);

            notificationButton.OnClickAsObservable().Subscribe(_ =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_QUANTITY_CANNOT_CHANGED"),
                    NotificationCell.NotificationType.Information);
            }).AddTo(_disposablesForAwake);

            CloseWidget = () =>
            {
                if (countInputField.isFocused || priceInputField.isFocused)
                {
                    return;
                }

                _data?.OnClickCancel.OnNext(_data);
            };

            L10nManager.OnLanguageChange.Subscribe(_ =>
            {
                reregisterButton.Text = L10nManager.Localize("UI_REREGISTER");
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

            priceInputField.text = data.Count.Value.ToString();
            countInputField.text = data.Count.Value.ToString();
            _data.ProductId.Value = data.ProductId.Value;
            _data.Price.Value = data.Price.Value;
            _data.UnitPrice.Value = data.UnitPrice.Value;

            _data.Count.Subscribe(value => countInputField.text = value.ToString())
                .AddTo(_disposablesForSetData);

            _data.Price.Subscribe(value =>
                {
                    if (value.MinorUnit == 0 && priceInputField.text.Contains(".0"))
                    {
                        priceInputField.text = $"{value.MajorUnit}.{value.MinorUnit}";
                    }
                    else if (value.MajorUnit == 0 && value.MinorUnit == 0)
                    {
                        priceInputField.text = string.Empty;
                    }
                    else
                    {
                        priceInputField.text = value.GetQuantityString();
                    }
                })
                .AddTo(_disposablesForSetData);

            _data.UnitPrice.Subscribe(value =>
                {
                    unitPrice.text = $"/{value.GetQuantityString()}";

                    var isPriceValid = IsPriceValid();
                    var isUnitPriceValid = IsUnitPriceValid();
                    submitButton.Interactable = isPriceValid && isUnitPriceValid;
                    reregisterButton.Interactable = isPriceValid && isUnitPriceValid;
                    unitPriceInvalidText.SetActive(!isUnitPriceValid);
                })
                .AddTo(_disposablesForSetData);

            priceInputField.Select();
            countInputField.Select();
        }

        private bool IsPriceValid()
        {
            if (!decimal.TryParse(_data.Price.Value.GetQuantityString(),
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
            {
                return false;
            }

            if (price - (int)price > 0)
            {
                return false;
            }

            var count = _data.Count.Value;
            return !(price < Model.Shop.MinimumPrice || count < 0);
        }

        // Note: Consumable(Food) 아이템의 경우 개당 가격 (Unit Price) 값에 소수점을 허용하지 않는다.
        private bool IsUnitPriceValid()
        {
            if (_data.Item.Value?.ItemBase.Value?.ItemType != ItemType.Consumable)
            {
                return true;
            }

            if (!decimal.TryParse(_data.UnitPrice.Value.GetQuantityString(),
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var outUnitPrice))
            {
                return false;
            }

            return outUnitPrice - (int)outUnitPrice <= 0;
        }

        protected override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();
        }

        private T InputFieldValueToValue<T>(TMP_InputField inputField)
        {
            var result = 0;

            if (typeof(T) == typeof(decimal))
            {
                if (!decimal.TryParse(inputField.text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var price))
                {
                    price = 0;
                }
                return (T)Convert.ChangeType(price, typeof(T));
            }

            if (typeof(T) == typeof(int))
            {
                if (!int.TryParse(inputField.text, NumberStyles.Number, CultureInfo.InvariantCulture,
                    out var price))
                {
                    price = 0;
                }
                return (T)Convert.ChangeType(price, typeof(T));
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public void Show(Model.ItemCountableAndPricePopup data, bool isSell)
        {
            if (isSell)
            {
                SubmitWidget = () => submitButton.OnSubmitSubject.OnNext(default);
            }
            else
            {
                SubmitWidget = () => reregisterButton.OnClickSubject.OnNext(default);
            }

            var isCountableItem = data.Item.Value.MaxCount.Value > 1;

            countInputField.textComponent.color =
                ColorHelper.HexToColorRGB(isSell && isCountableItem ? "ebceb1" : "292520");
            countInputField.enabled = isSell && isCountableItem;
            addCountButton.gameObject.SetActive(isSell && isCountableItem);
            addMaximumCountButton.gameObject.SetActive(isSell && isCountableItem);
            removeCountButton.gameObject.SetActive(isSell && isCountableItem);
            submitButton.gameObject.SetActive(isSell);
            reregisterButton.gameObject.SetActive(!isSell);
            notificationButton.gameObject.SetActive(!isSell);

            if (isSell)
            {
                var condition = ConditionalCostButton.CheckCostOfType(CostType.ActionPoint, 5);
                var inventoryItems = States.Instance.CurrentAvatarState.inventory.Items;
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                var apStoneCount = inventoryItems.Where(x =>
                        x.item.ItemSubType == ItemSubType.ApStone &&
                        !x.Locked &&
                        !(x.item is ITradableItem tradableItem &&
                          tradableItem.RequiredBlockIndex > blockIndex))
                    .Sum(item => item.count);

                submitButton.SetCost(CostType.ActionPoint, 5);
                submitButton.Interactable = condition || apStoneCount > 0;
            }
            else
            {
                reregisterButton.SetCost(CostType.ActionPoint, 5);
            }

            Pop(data);
        }
    }
}
