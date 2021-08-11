using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Extension;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class ItemInformationTooltip : VerticalTooltipWidget<Model.ItemInformationTooltip>
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private SubmitButton submitButton;
        [SerializeField] private Button retrieveButton;
        [SerializeField] private Button reregisterButton;
        [SerializeField] private SubmitWithCostButton buyButton;
        [SerializeField] private GameObject submit;
        [SerializeField] private GameObject buy;
        [SerializeField] private GameObject sell;
        [SerializeField] private BlockTimer buyTimer;
        [SerializeField] private BlockTimer sellTimer;
        [SerializeField] private Button closeButton;

        [SerializeField] private TextMeshProUGUI priceText;


        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private new Model.ItemInformationTooltip Model { get; set; }

        public Module.ItemInformation itemInformation;

        public RectTransform Target => Model.target.Value;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        protected override void Awake()
        {
            base.Awake();

            Model = new Model.ItemInformationTooltip();

            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.OnSubmitClick.OnNext(this);
                Close();
            }).AddTo(gameObject);

            buyButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.OnSubmitClick.OnNext(this);
                Close();
            }).AddTo(gameObject);

            CloseWidget = () =>
            {
                Model.OnCloseClick.OnNext(this);
                Close();
            };

            SubmitWidget = () =>
            {
                if (!submitButton.IsSubmittable)
                    return;

                AudioController.PlayClick();
                Model.OnSubmitClick.OnNext(this);
                Close();
            };

            closeButton.onClick.AddListener(() =>
            {
                Model.OnCloseClick.OnNext(this);
                Close();
            });
        }

        protected override void OnDestroy()
        {
            Model.Dispose();
            Model = null;
            base.OnDestroy();
        }

        public void Show(RectTransform target, CountableItem item, Action<ItemInformationTooltip> onClose = null)
        {
            Show(target, item, null, null, null, onClose);
        }

        public void Show(RectTransform target,
                         CountableItem item,
                         Func<CountableItem, bool> submitEnabledFunc,
                         string submitText,
                         Action<ItemInformationTooltip> onSubmit,
                         Action<ItemInformationTooltip> onClose = null)
        {
            if (item?.ItemBase.Value is null)
            {
                return;
            }

            submit.SetActive(submitEnabledFunc != null);
            sell.SetActive(false);
            buy.SetActive(false);

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;

            Show(Model);
            itemInformation.SetData(Model.ItemInformation);

            Model.TitleText.SubscribeTo(titleText).AddTo(_disposablesForModel);
            Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);
            Model.SubmitButtonText.SubscribeTo(submitButton).AddTo(_disposablesForModel);
            Model.SubmitButtonEnabled.Subscribe(submitButton.SetSubmittable).AddTo(_disposablesForModel);

            Model.SubmitButtonText.SubscribeTo(submitButton).AddTo(_disposablesForModel);
            Model.SubmitButtonEnabled.Subscribe(submitButton.SetSubmittable).AddTo(_disposablesForModel);
            Model.OnSubmitClick.Subscribe(onSubmit).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }
            Model.ItemInformation.item.Subscribe(value => SubscribeTargetItem(Model.target.Value))
                .AddTo(_disposablesForModel);
        }

        public void ShowForSell(RectTransform target,
                                CountableItem item,
                                Func<CountableItem, bool> submitEnabledFunc,
                                string submitText,
                                Action<ItemInformationTooltip> onSell,
                                Action<ItemInformationTooltip> onSellCancellation,
                                Action<ItemInformationTooltip> onClose)
        {
            if (item?.ItemBase.Value is null)
            {
                return;
            }

            submit.SetActive(false);
            buy.SetActive(false);
            sell.SetActive(true);
            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;

            Show(Model);
            itemInformation.SetData(Model.ItemInformation);

            Model.TitleText.SubscribeTo(titleText).AddTo(_disposablesForModel);
            Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);

            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }

            Model.ItemInformation.item
                .Subscribe(value => SubscribeTargetItem(Model.target.Value))
                .AddTo(_disposablesForModel);

            retrieveButton.onClick.RemoveAllListeners();
            retrieveButton.onClick.AddListener(() =>
            {
                onSellCancellation.Invoke(this);
                Model.OnCloseClick.OnNext(this);
                Close();
            });

            reregisterButton.onClick.RemoveAllListeners();
            reregisterButton.onClick.AddListener(() =>
            {
                onSell.Invoke(this);
                Model.OnCloseClick.OnNext(this);
                Close();
            });
            sellTimer.UpdateTimer(Model.ExpiredBlockIndex.Value);
        }

          public void ShowForBuy(RectTransform target,
                                CountableItem item,
                                Func<CountableItem, bool> submitEnabledFunc,
                                string submitText,
                                Action<ItemInformationTooltip> onBuy,
                                Action<ItemInformationTooltip> onClose)
        {
            if (item?.ItemBase.Value is null)
            {
                return;
            }

            submit.SetActive(false);
            sell.SetActive(false);
            buy.SetActive(true);

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;
            Show(Model);
            itemInformation.SetData(Model.ItemInformation);

            Model.TitleText.SubscribeTo(titleText).AddTo(_disposablesForModel);
            Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);
            Model.SubmitButtonText.SubscribeTo(buyButton).AddTo(_disposablesForModel);
            Model.SubmitButtonEnabled.Subscribe(buyButton.SetSubmittable).AddTo(_disposablesForModel);
            Model.Price.Subscribe(price =>
            {
                buyButton.ShowNCG(price, price <= States.Instance.GoldBalanceState.Gold);
            }).AddTo(_disposablesForModel);

            Model.OnSubmitClick.Subscribe(onBuy).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }

            Model.ItemInformation.item
                .Subscribe(value => SubscribeTargetItem(Model.target.Value))
                .AddTo(_disposablesForModel);
            buyTimer.UpdateTimer(Model.ExpiredBlockIndex.Value);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = null;
            Model.ItemInformation.item.Value = null;
            base.Close(ignoreCloseAnimation);
        }

        protected override void SubscribeTarget(RectTransform target)
        {
            // Try not to do anything.
        }

        private void SubscribeTargetItem(RectTransform target)
        {
            panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
            base.SubscribeTarget(target);

            if (!(target is null) && panel.position.x - target.position.x < 0)
            {
                panel.SetAnchorAndPivot(AnchorPresetType.TopRight, PivotPresetType.TopRight);
                panel.MoveToRelatedPosition(target, TargetPivotPresetType.ReverseX(), DefaultOffsetFromTarget.ReverseX());
                UpdateAnchoredPosition();
            }
        }
    }
}
