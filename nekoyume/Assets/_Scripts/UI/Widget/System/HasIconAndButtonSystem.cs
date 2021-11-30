using System;
using System.Collections;
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

        [SerializeField]
        private Blur blur;

        private TextButton _confirmButton = null;

        private TextButton _cancelButton = null;

        private TextMeshProUGUI _contentText = null;

        private TextMeshProUGUI _titleText = null;

        public System.Action ConfirmCallback { get; set; }
        public System.Action CancelCallback { get; set; }

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
            if (blur)
            {
                blur.Close();
            }
            base.Close(ignoreCloseAnimation);
        }

        public void Show(string title, string content, string labelYes = "UI_OK",
            bool localize = true, SystemType type = SystemType.Error)
        {
            if (blur)
            {
                blur.Show();
            }

            if (gameObject.activeSelf)
            {
                Close(true);
                ShowWithTwoButton(title, content, labelYes, "", localize, false, type);
                return;
            }

            Set(title, content, labelYes, "", localize, false, type);
        }

        public void ShowWithTwoButton(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, bool hasConfirmButton = true, SystemType type = SystemType.Error)
        {
            if (blur)
            {
                blur.Show();
            }

            if (gameObject.activeSelf)
            {
                Close(true);
                ShowWithTwoButton(title, content, labelYes, labelNo, localize, hasConfirmButton, type);
                return;
            }

            Set(title, content, labelYes, labelNo, localize, hasConfirmButton, type);
            Show();
        }

        public void ShowByBlockDownloadFail(long index)
        {
            var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_FORMAT"),
                L10nManager.Localize("BLOCK_DOWNLOAD"));

            Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                L10nManager.Localize("UI_OK"), false, SystemType.BlockChainError);
            StartCoroutine(CoCheckBlockIndex(index));
#if UNITY_EDITOR
            CancelCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CancelCallback = () => Application.Quit(21);
#endif
        }

        public void SetCancelCallbackToExit()
        {
            CancelCallback = () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                CloseCallback = UnityEngine.Application.Quit;
#endif
            };
        }

        private IEnumerator CoCheckBlockIndex(long blockIndex)
        {
            yield return new WaitWhile(() => Game.Game.instance.Agent.BlockIndex == blockIndex);
            CancelCallback = null;
            Close();
        }

        private void Set(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, bool hasConfirmButton = true, SystemType type = SystemType.Error)
        {
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
        }
    }
}
