using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
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

        private System.Action _onSubmit;
        private System.Action _onClose;
        private System.Action _onBlocked;

        private bool _isPointerOnScrollArea;
        private bool _isClickedButtonArea;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        protected override void Awake()
        {
            base.Awake();
            submitButton.OnSubmitSubject.Subscribe(_ =>
            {
                _onSubmit?.Invoke();
                Close();
            }).AddTo(gameObject);
            submitButton.OnClickDisabledSubject.Subscribe(_ => _onBlocked?.Invoke())
                .AddTo(gameObject);
            CloseWidget = () => Close();
            SubmitWidget = () =>
            {
                if (!submitButton.IsSubmittable)
                    return;

                AudioController.PlayClick();
                _onSubmit?.Invoke();
                Close();
            };
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _onClose?.Invoke();
            _isPointerOnScrollArea = false;
            _isClickedButtonArea = false;
            _disposablesForModel.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        public void Show(RectTransform target,
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            detail.Set(item.ItemBase, item.Count.Value, !Util.IsUsableItem(item.ItemBase.Id));

            submitButton.gameObject.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(submitButton.gameObject));
        }

        public void Show(RectTransform target,
            ShopItem item,
            System.Action onRegister,
            System.Action onSellCancellation,
            System.Action onClose)
        {
            submitButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(true);
            sell.Set(item.OrderDigest.ExpiredBlockIndex,
                () =>
                {
                    onSellCancellation?.Invoke();
                    Close();
                }, () =>
                {
                    onRegister?.Invoke();
                    Close();
                });
            detail.Set(item.ItemBase, item.OrderDigest.ItemCount,
                !Util.IsUsableItem(item.OrderDigest.ItemId));
            _onClose = onClose;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(sell.gameObject));
        }

        public void Show(RectTransform target,
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            submitButton.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            buy.gameObject.SetActive(true);
            buy.Set(item.OrderDigest.ExpiredBlockIndex,
                item.OrderDigest.Price,
                () =>
                {
                    onBuy?.Invoke();
                    Close();
                });

            detail.Set(item.ItemBase, item.OrderDigest.ItemCount,
                !Util.IsUsableItem(item.OrderDigest.ItemId));
            _onClose = onClose;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(buy.gameObject));
        }

        public void Show(RectTransform target,
            EquipmentInventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            detail.Set(item.ItemBase, 1, !Util.IsUsableItem(item.ItemBase.Id));

            submitButton.gameObject.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(submitButton.gameObject));
        }

        private void UpdatePosition(RectTransform target)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)verticalLayoutGroup.transform);
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
    }
}
