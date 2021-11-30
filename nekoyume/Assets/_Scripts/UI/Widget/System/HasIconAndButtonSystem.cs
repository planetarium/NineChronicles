using System;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class HasIconAndButtonSystem : SystemWidget
    {
        public enum SystemType : int
        {
            Error = 0,
            BlockChainError,
            Information,
        }

        [SerializeField]
        private IconAndButton[] uiBySystemType;

        private TextButton _confirmButton = null;

        private TextButton _cancelButton = null;

        private TextMeshProUGUI _contentText = null;

        private TextMeshProUGUI _titleText = null;

        public System.Action ConfirmCallback { get; set; }
        public System.Action CancelCallback { get; set; }
        private SystemType _nowType;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = Confirm;
            CloseWidget = Cancel;
            foreach (var ui in uiBySystemType)
            {
                ui.confirmButton.OnClick = Confirm;
                ui.cancelButton.OnClick = Cancel;
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            foreach (var ui in uiBySystemType)
            {
                ui.gameObject.SetActive(false);
            }
            base.Close(ignoreCloseAnimation);
        }

        public void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, bool hasConfirmButton = true, SystemType type = SystemType.Error)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(title, content, labelYes, labelNo, localize, hasConfirmButton, type);
                return;
            }

            Set(title, content, labelYes, labelNo, localize, hasConfirmButton, type);
            Show();
        }

        private void Set(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, bool hasConfirmButton = true, SystemType type = SystemType.Error)
        {
            foreach (var ui in uiBySystemType)
            {
                ui.gameObject.SetActive(false);
            }

            SetUIByType(type);
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                {
                    _titleText.text = L10nManager.Localize(title);
                }
                _contentText.text = L10nManager.Localize(content);
                _confirmButton.Text = L10nManager.Localize(labelYes);
                _cancelButton.Text = L10nManager.Localize(labelNo);
            }
            else
            {
                _titleText.text = title;
                _contentText.text = content;
                _confirmButton.Text = labelYes;
                _cancelButton.Text = labelNo;
            }

            _titleText.gameObject.SetActive(titleExists);
            _confirmButton.gameObject.SetActive(hasConfirmButton);
        }

        private void Confirm()
        {
            ConfirmCallback?.Invoke();
            Close();
            AudioController.PlayClick();
        }

        public void Cancel()
        {
            CancelCallback?.Invoke();
            Close();
            AudioController.PlayClick();
        }

        private void SetUIByType(SystemType type)
        {
            _confirmButton = uiBySystemType[(int) type].confirmButton;
            _cancelButton = uiBySystemType[(int) type].cancelButton;
            _contentText = uiBySystemType[(int) type].contentText;
            _titleText = uiBySystemType[(int) type].titleText;
            uiBySystemType[(int) type].gameObject.SetActive(true);
            _nowType = type;
        }
    }
}
