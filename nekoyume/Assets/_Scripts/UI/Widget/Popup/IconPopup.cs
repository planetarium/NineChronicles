using System;
using DG.Tweening;
using Libplanet.Types.Assets;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class IconPopup : PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private FungibleAssetValueView fungibleAssetValueView;

        [SerializeField]
        private SimpleCountableItemView itemView;

        [SerializeField]
        private GameObject descriptionScrollViewGO;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

        [SerializeField]
        private TextButton okButton;

        [SerializeField]
        private TextButton cancelButton;

        public string Title => titleText.text;

        public FungibleAssetValue FungibleAssetValue =>
            fungibleAssetValueView.FungibleAssetValue;

        public ItemBase Item => itemView.Model?.ItemBase.Value;

        public string Description => descriptionText.text;

        public event Action<IconPopup> OnOk;

        public event Action<IconPopup> OnCancel;

        private Tweener _showOrCloseTweener;

        protected override void Awake()
        {
            base.Awake();
            okButton.OnClick = () =>
            {
                OnOk?.Invoke(this);
                Close();
            };
            cancelButton.OnClick = () =>
            {
                OnCancel?.Invoke(this);
                Close();
            };
            CloseWidget = () =>
            {
                AudioController.PlayClick();
                if (cancelButton.gameObject.activeSelf)
                {
                    cancelButton.OnClick?.Invoke();
                }
                else if (okButton.gameObject.activeSelf)
                {
                    okButton.OnClick?.Invoke();
                }
                else
                {
                    Close();
                }
            };
        }

        public IconPopup SetTitle(
            bool active = true,
            string text = null,
            string l10nKey = null)
        {
            if (active)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    titleText.text = text;
                }
                else if (!string.IsNullOrEmpty(l10nKey))
                {
                    titleText.text = L10nManager.Localize(l10nKey);
                }
                else
                {
                    titleText.text = string.Empty;
                }

                titleText.gameObject.SetActive(true);
                return this;
            }

            titleText.gameObject.SetActive(false);
            return this;
        }

        public IconPopup SetDescription(
            bool active = true,
            string text = null,
            string l10nKey = null)
        {
            if (active)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    descriptionText.text = text;
                }
                else if (!string.IsNullOrEmpty(l10nKey))
                {
                    descriptionText.text = L10nManager.Localize(l10nKey);
                }
                else
                {
                    descriptionText.text = string.Empty;
                }

                descriptionScrollViewGO.SetActive(true);
                return this;
            }

            descriptionScrollViewGO.SetActive(false);
            return this;
        }

        public IconPopup SetOkButton(
            bool active = true,
            string text = null,
            Action<IconPopup> onClick = null)
        {
            if (active)
            {
                okButton.Text = string.IsNullOrEmpty(text)
                    ? L10nManager.Localize("UI_OK")
                    : text;
                okButton.gameObject.SetActive(true);
                if (onClick is not null)
                {
                    OnOk += onClick;
                }

                return this;
            }

            okButton.gameObject.SetActive(false);
            return this;
        }

        public IconPopup SetCancelButton(
            bool active = true,
            string text = null,
            Action<IconPopup> onClick = null)
        {
            if (active)
            {
                cancelButton.Text = string.IsNullOrEmpty(text)
                    ? L10nManager.Localize("UI_CANCEL")
                    : text;
                cancelButton.gameObject.SetActive(true);
                if (onClick is not null)
                {
                    OnCancel += onClick;
                }

                return this;
            }

            cancelButton.gameObject.SetActive(false);
            return this;
        }

        public IconPopup Show(
            FungibleAssetValue fungibleAssetValue,
            bool ignoreShowAnimation = false)
        {
            itemView.gameObject.SetActive(false);
            fungibleAssetValueView.SetData(fungibleAssetValue);
            fungibleAssetValueView.gameObject.SetActive(true);
            ShowInternal(ignoreShowAnimation);
            return this;
        }

        public IconPopup Show(
            ItemBase item,
            bool ignoreShowAnimation = false)
        {
            fungibleAssetValueView.gameObject.SetActive(false);
            itemView.SetData(item);
            itemView.gameObject.SetActive(true);
            ShowInternal(ignoreShowAnimation);
            return this;
        }

        private void ShowInternal(
            bool ignoreShowAnimation)
        {
            ClearTweener();

            // NOTE: Always set `ignoreShowAnimation` argument to `true` because
            //       `IconPopup` does not use any animation.
            base.Show(ignoreShowAnimation: true);
            if (ignoreShowAnimation)
            {
                transform.localScale = Vector3.one;
                return;
            }

            transform.localScale = Vector3.zero;
            _showOrCloseTweener = transform.DOScale(Vector3.one, .3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    transform.localScale = Vector3.one;
                    _showOrCloseTweener = null;
                });
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            OnOk = null;
            OnCancel = null;
            ClearTweener();
            if (ignoreCloseAnimation)
            {
                // NOTE: Always set `ignoreShowAnimation` argument to `true` because
                //       `IconPopup` does not use any animation.
                base.Close(ignoreCloseAnimation: true);
                return;
            }

            transform.localScale = Vector3.one;
            _showOrCloseTweener = transform.DOScale(Vector3.zero, .3f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    transform.localScale = Vector3.zero;
                    _showOrCloseTweener = null;
                    base.Close(ignoreCloseAnimation: true);
                });
        }

        private void ClearTweener()
        {
            if (_showOrCloseTweener is null)
            {
                return;
            }

            if (!_showOrCloseTweener.IsPlaying())
            {
                _showOrCloseTweener = null;
                return;
            }

            _showOrCloseTweener.Kill();
            _showOrCloseTweener = null;
        }
    }
}
