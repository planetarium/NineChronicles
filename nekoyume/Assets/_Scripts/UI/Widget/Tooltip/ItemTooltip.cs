using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    using System.Collections;
    using UniRx;
    using UnityEngine.UI;

    public class ItemTooltip : NewVerticalTooltipWidget
    {
        [SerializeField]
        private ItemTooltipDetail detail;

        [SerializeField]
        private ConditionalButton submitButton;

        [SerializeField]
        private ItemTooltipBuy buy;

        [SerializeField]
        private ItemTooltipSell sell;

        [SerializeField]
        private Scrollbar scrollbar;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private bool _isPointerOnScrollArea;
        private bool _isClickedButtonArea;
        private bool _isShopItem;

        private Model.ItemInformationTooltip Model { get; set; }

        private System.Action onSubmit;
        private System.Action onClose;
        private System.Action onBlocked;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        protected override void Awake()
        {
            base.Awake();

            submitButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onSubmit?.Invoke();
                Close();
            }).AddTo(gameObject);
            submitButton.OnClickDisabledSubject.Subscribe(_ => onBlocked?.Invoke()).AddTo(gameObject);
            CloseWidget = () => Close();
            SubmitWidget = () =>
            {
                if (!submitButton.IsSubmittable)
                    return;

                AudioController.PlayClick();
                onSubmit?.Invoke();
                Close();
            };
        }

        protected override void OnEnable()
        {
            if (_isShopItem)
            {
                Game.Game.instance.Agent.BlockIndexSubject.Subscribe((long blockIndex) =>
                {
                    var isExpired = Model.ExpiredBlockIndex.Value - blockIndex <= 0;
                    Model.SubmitButtonEnabled.SetValueAndForceNotify(
                        Model.SubmitButtonEnabledFunc.Value.Invoke(Model.ItemInformation.item
                            .Value) && !isExpired);
                }).AddTo(_disposablesForModel);
            }

            base.OnEnable();
        }

        public void Show(RectTransform target, CountableItem item,
            Action<ItemInformationTooltip> onClose = null)
        {
            Show(target, item, null, null, null, onClose);
        }

        public void Show(RectTransform target,
            CountableItem item,
            Func<CountableItem, bool> submitEnabledFunc,
            string submitText,
            Action<ItemInformationTooltip> onSubmit,
            Action<ItemInformationTooltip> onClose = null,
            Action<ItemInformationTooltip> onClickBlocked = null,
            bool isShopItem = false)
        {
            if (item?.ItemBase.Value is null)
            {
                return;
            }

            submitButton.gameObject.SetActive(submitEnabledFunc != null);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;

            // Show(Model);
            // itemInformation.SetData(Model.ItemInformation);

            // Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);
            Model.SubmitButtonText.SubscribeTo(submitButton).AddTo(_disposablesForModel);
            Model.SubmitButtonEnabled.Subscribe(value => submitButton.Interactable = value)
                .AddTo(_disposablesForModel);
            Model.OnSubmitClick.Subscribe(onSubmit).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }

            if (onClickBlocked != null)
            {
                Model.OnClickBlocked.Subscribe(onClickBlocked).AddTo(_disposablesForModel);
            }

            Model.ItemInformation.item.Subscribe(value => UpdatePosition(Model.target.Value))
                .AddTo(_disposablesForModel);

            scrollbar.value = 1f;
            _isShopItem = isShopItem;
            StartCoroutine(CoUpdate(submitButton.gameObject));
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

            submitButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(true);
            sell.Set(Model.ExpiredBlockIndex.Value,
                () =>
                {
                    // onSellCancellation.Invoke(this);
                    // Model.OnCloseClick.OnNext(this);
                    Close();
                }, () =>
                {
                    // onSell.Invoke(this);
                    // Model.OnCloseClick.OnNext(this);
                    Close();
                });
            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;

            // Show(Model);
            // itemInformation.SetData(Model.ItemInformation);

            // Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);

            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }

            Model.ItemInformation.item
                .Subscribe(value => UpdatePosition(Model.target.Value))
                .AddTo(_disposablesForModel);

            scrollbar.value = 1f;
            _isShopItem = true;
            StartCoroutine(CoUpdate(sell.gameObject));
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

            submitButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(true);
            sell.gameObject.SetActive(false);
            buy.Set(Model.ExpiredBlockIndex.Value, Model.Price.Value,
                Model.Price.Value <= States.Instance.GoldBalanceState.Gold,
                () => { }); // todo : 콜백넣어줘야됨

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.SetValueAndForceNotify(submitEnabledFunc);
            Model.SubmitButtonText.Value = submitText;
            // Show(Model);
            // itemInformation.SetData(Model.ItemInformation);

            // Model.Price.SubscribeToPrice(priceText).AddTo(_disposablesForModel);
            // Model.SubmitButtonText.SubscribeTo(buyButton).AddTo(_disposablesForModel);
            // Model.SubmitButtonEnabled.Subscribe(buyButton.SetSubmittable)
            //     .AddTo(_disposablesForModel);
            // Model.Price.Subscribe(price =>
            // {
            //     buyButton.ShowNCG(price, price <= States.Instance.GoldBalanceState.Gold);
            // }).AddTo(_disposablesForModel);

            Model.OnSubmitClick.Subscribe(onBuy).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnCloseClick.Subscribe(onClose).AddTo(_disposablesForModel);
            }

            Model.ItemInformation.item
                .Subscribe(value => UpdatePosition(Model.target.Value))
                .AddTo(_disposablesForModel);

            scrollbar.value = 1f;
            _isShopItem = true;
            StartCoroutine(CoUpdate(buy.gameObject));
            // buyTimer.UpdateTimer(Model.ExpiredBlockIndex.Value);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            onClose?.Invoke();
            _isPointerOnScrollArea = false;
            _isClickedButtonArea = false;
            _disposablesForModel.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private void UpdatePosition(RectTransform target)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)verticalLayoutGroup.transform);
            panel.MoveToRelatedPosition(target, TargetPivotPresetType, OffsetFromTarget);
            panel.MoveInsideOfParent(MarginFromParent);

            if (!(target is null) && panel.position.x - target.position.x < 0)
            {
                panel.SetAnchorAndPivot(AnchorPresetType.TopRight, PivotPresetType.TopRight);
                panel.MoveToRelatedPosition(target, TargetPivotPresetType.ReverseX(),
                    DefaultOffsetFromTarget.ReverseX());
                UpdateAnchoredPosition(target);
            }
        }

        private IEnumerator CoUpdate(GameObject target)
        {
            var selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
            while (selectedGameObjectCache is null)
            {
                selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
                yield return null;
            }

            var positionCache = selectedGameObjectCache.transform.position;

            while (enabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isClickedButtonArea = _isPointerOnScrollArea;
                }

                var current = TouchHandler.currentSelectedGameObject;
                if (current == selectedGameObjectCache)
                {
                    if (!Input.GetMouseButton(0) &&
                        Input.mouseScrollDelta == default)
                    {
                        yield return null;
                        continue;
                    }

                    var position = selectedGameObjectCache.transform.position;
                    if (position != positionCache)
                    {
                        Close();
                        yield break;
                    }

                    if (!_isClickedButtonArea)
                    {
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

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }

                yield return null;
            }
        }

        public void OnEnterButtonArea(bool value)
        {
            _isPointerOnScrollArea = value;
        }

        public void Show(RectTransform target,
            InventoryItemViewModel item,
            string submitText,
            bool interactable,
            System.Action submit,
            System.Action close = null,
            System.Action blocked = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            detail.Set(item.ItemBase, item.Count.Value);

            submitButton.gameObject.SetActive(submit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            onSubmit = submit;
            onClose = close;
            onBlocked = blocked;

            scrollbar.value = 1f;
            _isShopItem = false;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(submitButton.gameObject));
        }
    }
}
