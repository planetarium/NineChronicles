using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Extension;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.UI
{
    using UniRx;

    public class ItemInformationTooltip : VerticalTooltipWidget<Model.ItemInformationTooltip>
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private SubmitButton submitButton;
        [SerializeField] private SubmitButton sellButton;
        [SerializeField] private SubmitWithCostButton buyButton;
        [SerializeField] private GameObject submit;
        [SerializeField] private GameObject sell;
        [SerializeField] private GameObject buy;
        [SerializeField] private BlockTimer sellTimer;
        [SerializeField] private BlockTimer buyTimer;

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

            sellButton.OnSubmitClick.Subscribe(_ =>
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
        }

        protected override void OnDestroy()
        {
            Model.Dispose();
            Model = null;
            base.OnDestroy();
        }

        public void Show(RectTransform target, CountableItem item, Action<ItemInformationTooltip> onClose = null)
        {
            if (item.ForceDimmed)
            {
                if (item?.ItemBase.Value is ITradableItem tradableItem)
                {
                    var remain = tradableItem.RequiredBlockIndex - Game.Game.instance.Agent.BlockIndex;
                    OneLinePopup.Push(MailType.System, $"This item has not expired. It can be used again after {remain} blocks.");
                }
                return;
            }
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

            if (item.ForceDimmed)
            {
                if (item?.ItemBase.Value is ITradableItem tradableItem)
                {
                    var remain = tradableItem.RequiredBlockIndex - Game.Game.instance.Agent.BlockIndex;
                    OneLinePopup.Push(MailType.System, $"This item has not expired. It can be used again after {remain} blocks.");
                }
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

            StartCoroutine(CoUpdate(submitButton.gameObject));
        }

        public void ShowForShop(RectTransform target,
                                CountableItem item,
                                Func<CountableItem, bool> submitEnabledFunc,
                                string submitText,
                                Action<ItemInformationTooltip> onSubmit,
                                Action<ItemInformationTooltip> onClose,
                                bool isBuy)
        {
            if (item?.ItemBase.Value is null)
            {
                return;
            }

            submit.SetActive(false);
            sell.SetActive(!isBuy);
            buy.SetActive(isBuy);

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;
            sellTimer.UpdateTimer(Model.ExpiredBlockIndex.Value);
            buyTimer.UpdateTimer(Model.ExpiredBlockIndex.Value);

            Show(Model);
            itemInformation.SetData(Model.ItemInformation);

            Model.TitleText.SubscribeTo(titleText).AddTo(_disposablesForModel);
            Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);

            if (isBuy)
            {
                Model.SubmitButtonText.SubscribeTo(buyButton).AddTo(_disposablesForModel);
                Model.SubmitButtonEnabled.Subscribe(buyButton.SetSubmittable).AddTo(_disposablesForModel);
                Model.Price.Subscribe(price =>
                {
                    buyButton.ShowNCG(price, price <= States.Instance.GoldBalanceState.Gold);
                }).AddTo(_disposablesForModel);
            }
            else
            {
                Model.SubmitButtonText.SubscribeTo(sellButton).AddTo(_disposablesForModel);
                Model.SubmitButtonEnabled.Subscribe(sellButton.SetSubmittable).AddTo(_disposablesForModel);
            }

            Model.OnSubmitClick.Subscribe(onSubmit).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }

            Model.ItemInformation.item
                .Subscribe(value => SubscribeTargetItem(Model.target.Value))
                .AddTo(_disposablesForModel);

            StartCoroutine(CoUpdate(isBuy ? buyButton.gameObject : sellButton.gameObject));
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

        private IEnumerator CoUpdate(GameObject target)
        {
            var selectedGameObjectCache = EventSystem.current.currentSelectedGameObject;
            while (selectedGameObjectCache is null)
            {
                selectedGameObjectCache = EventSystem.current.currentSelectedGameObject;
                yield return null;
            }

            var positionCache = selectedGameObjectCache.transform.position;

            while (enabled)
            {
                var current = EventSystem.current.currentSelectedGameObject;
                if (current == selectedGameObjectCache)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        positionCache = selectedGameObjectCache.transform.position;
                        yield return null;
                        continue;
                    }

                    if (!Input.GetMouseButton(0) &&
                        Input.mouseScrollDelta == default)
                    {
                        yield return null;
                        continue;
                    }

                    var position = selectedGameObjectCache.transform.position;
                    if (position != positionCache)
                    {
                        Model.OnCloseClick.OnNext(this);
                        Close();
                        yield break;
                    }
                }
                else
                {
                    if (current == target)
                    {
                        yield break;
                    }

                    Model.OnCloseClick.OnNext(this);
                    Close();
                    yield break;
                }

                yield return null;
            }
        }
    }
}
