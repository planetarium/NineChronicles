using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Model.Order;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
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
        private Button enhancementButton;

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
        protected GameObject submitButtonContainer;

        [SerializeField]
        protected AcquisitionPlaceDescription acquisitionPlaceDescription;

        private readonly List<IDisposable> _disposablesForModel = new();

        private RectTransform _descriptionButtonRectTransform;

        private System.Action _onSubmit;
        private System.Action _onClose;
        private System.Action _onBlocked;
        private System.Action _onEnhancement;

        public bool _isPointerOnScrollArea;
        public bool _isClickedButtonArea;

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
            enhancementButton.onClick.AddListener(() =>
            {
                const int requiredStage = Game.LiveAsset.GameConfig.RequiredStage.Enhancement;
                if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_REQUIRE_CLEAR_STAGE", requiredStage),
                        NotificationCell.NotificationType.UnlockCondition);
                    return;
                }

                _onEnhancement?.Invoke();
                Close(true);
            });
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
            int itemCount = 0)
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

            submitButtonContainer.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(submitButtonContainer.gameObject));
        }

        public virtual void Show(
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            System.Action onEnhancement = null)
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

            submitButtonContainer.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;
            _onEnhancement = onEnhancement;
            enhancementButton.gameObject.SetActive(onEnhancement != null);

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(submitButtonContainer.gameObject));
        }

        public virtual void Show(
            ShopItem item,
            int apStoneCount,
            Action<ConditionalButton.State> onRegister,
            Action<ConditionalButton.State> onSellCancellation,
            System.Action onClose)
        {
            submitButtonContainer.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(true);
            if (item.Product.Legacy)
            {
                sell.Set(
                    item.Product.RegisteredBlockIndex + Order.ExpirationInterval,
                    apStoneCount,
                    state =>
                    {
                        onSellCancellation?.Invoke(state);
                        Close();
                    }, state =>
                    {
                        onRegister?.Invoke(state);
                        Close();
                    });
            }
            else
            {
                sell.Set(
                    apStoneCount,
                    state =>
                    {
                        onSellCancellation?.Invoke(state);
                        Close();
                    }, state =>
                    {
                        onRegister?.Invoke(state);
                        Close();
                    });
            }

            detail.Set(
                item.ItemBase,
                (int) item.Product.Quantity,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));
            _onClose = onClose;

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(sell.gameObject));
        }

        public virtual void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            submitButtonContainer.SetActive(false);
            sell.gameObject.SetActive(false);
            buy.gameObject.SetActive(true);
            if (item.Product.Legacy)
            {
                buy.Set(item.Product.RegisteredBlockIndex + Order.ExpirationInterval,
                    (BigInteger)item.Product.Price * States.Instance.GoldBalanceState.Gold.Currency,
                    () =>
                    {
                        onBuy?.Invoke();
                        Close();
                    });
            }
            else
            {
                buy.Set((BigInteger)item.Product.Price * States.Instance.GoldBalanceState.Gold.Currency,
                    () =>
                    {
                        onBuy?.Invoke();
                        Close();
                    });
            }

            detail.Set(
                item.ItemBase,
                (int)item.Product.Quantity,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume),
                // it isn't implemented to get equipment.exp in the MarketService.
                // so, it doesn't show the expText in the shop buy UI.
                false);
            _onClose = onClose;

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(buy.gameObject));
        }

        public virtual void Show(
            EnhancementInventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null)
        {
            enhancementButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            detail.Set(
                item.ItemBase,
                1,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));

            submitButtonContainer.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(submitButtonContainer.gameObject));
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

        public void TutorialActionClickItemInformationTooltipSubmitButton()
        {
            _onSubmit?.Invoke();
            Close();
        }
    }
}
