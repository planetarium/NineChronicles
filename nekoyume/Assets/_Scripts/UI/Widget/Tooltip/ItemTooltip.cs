using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    using System.Collections;
    using UniRx;
    using UnityEngine.UI;

    public abstract class ItemTooltip : NewVerticalTooltipWidget
    {
        [SerializeField]
        protected ItemTooltipDetail detail;

        [SerializeField]
        protected ConditionalButton submitButton;

        [SerializeField]
        protected ItemTooltipBuy buy;

        [SerializeField]
        protected ItemTooltipSell sell;

        [SerializeField]
        protected Scrollbar scrollbar;

        [SerializeField]
        protected List<AcquisitionPlaceButton> acquisitionPlaceButtons;

        [SerializeField]
        protected Button descriptionButton;

        [SerializeField]
        protected AcquisitionPlaceDescription acquisitionPlaceDescription;

        protected readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        protected RectTransform _descriptionButtonRectTransform;

        protected System.Action _onSubmit;
        protected System.Action _onClose;
        protected System.Action _onBlocked;

        protected bool _isPointerOnScrollArea;
        protected bool _isClickedButtonArea;

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

            if (descriptionButton != null)
            {
                _descriptionButtonRectTransform = descriptionButton.GetComponent<RectTransform>();
                descriptionButton.onClick.AddListener(() =>
                {
                    acquisitionPlaceDescription.Show(panel, _descriptionButtonRectTransform);
                });
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _onClose?.Invoke();
            _isPointerOnScrollArea = false;
            _isClickedButtonArea = false;
            _disposablesForModel.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        public virtual void Show(
            ItemBase item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            int itemCount = 0,
            RectTransform target = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            acquisitionPlaceButtons.ForEach(button => button.gameObject.SetActive(false));
            detail.Set(
                item,
                itemCount,
                !Util.IsUsableItem(item) &&
                (item.ItemType == ItemType.Equipment ||
                 item.ItemType == ItemType.Costume));

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

        public virtual void Show(
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            RectTransform target = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            acquisitionPlaceButtons.ForEach(button => button.gameObject.SetActive(false));
            detail.Set(
                item.ItemBase,
                item.Count.Value,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));

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

        public virtual void Show(
            ShopItem item,
            System.Action onRegister,
            System.Action onSellCancellation,
            System.Action onClose,
            RectTransform target = null)
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
            detail.Set(
                item.ItemBase,
                item.OrderDigest.ItemCount,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));
            _onClose = onClose;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(sell.gameObject));
        }

        public virtual void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose,
            RectTransform target = null)
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

            detail.Set(
                item.ItemBase,
                item.OrderDigest.ItemCount,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));
            _onClose = onClose;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
            StartCoroutine(CoUpdate(buy.gameObject));
        }

        public virtual void Show(
            EnhancementInventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            RectTransform target = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            detail.Set(
                item.ItemBase,
                1,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));

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

        public static ItemTooltip Find(ItemType type)
        {
            return type switch
            {
                ItemType.Consumable => Find<ConsumableTooltip>(),
                ItemType.Costume => Find<CostumeTooltip>(),
                ItemType.Equipment => Find<EquipmentTooltip>(),
                ItemType.Material => Find<MaterialTooltip>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"invalid ItemType : {type}")
            };
        }

        protected void UpdatePosition(RectTransform target)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)verticalLayoutGroup.transform);
            panel.MoveInsideOfParent(MarginFromParent);
            if (target)
            {
                panel.MoveToRelatedPosition(target, TargetPivotPresetType, OffsetFromTarget);
            }
            else
            {
                panel.SetAnchor(AnchorPresetType.MiddleCenter);
                panel.anchoredPosition =
                    new Vector2(-(panel.sizeDelta.x / 2), panel.sizeDelta.y / 2);
            }

            if (!(target is null) && panel.position.x - target.position.x < 0)
            {
                panel.SetAnchorAndPivot(AnchorPresetType.TopRight, PivotPresetType.TopRight);
                panel.MoveToRelatedPosition(target, TargetPivotPresetType.ReverseX(),
                    DefaultOffsetFromTarget.ReverseX());
                UpdateAnchoredPosition(target);
            }
        }

        protected IEnumerator CoUpdate(GameObject target)
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
