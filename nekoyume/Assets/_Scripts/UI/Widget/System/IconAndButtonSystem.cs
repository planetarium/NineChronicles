using System;
using System.Collections;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class IconAndButtonSystem : SystemWidget
    {
        public enum SystemType : int
        {
            Error = 0,
            BlockChainError,
            Information,
        }

        [Serializable]
        private struct IconAndButton
        {
            public GameObject rootGameObject;

            public TextButton confirmButton;

            public TextButton cancelButton;

            public TextMeshProUGUI contentText;

            public TextMeshProUGUI titleText;

            public Button textGroupButton;
        }

        [SerializeField]
        private IconAndButton[] uiBySystemType;

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
                ui.textGroupButton.onClick.AddListener(() =>
                {
                    ClipboardHelper.CopyToClipboard(ui.contentText.text);
                    NotificationSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_COPIED"),
                        NotificationCell.NotificationType.Information);
                });
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            foreach (var ui in uiBySystemType)
            {
                ui.rootGameObject.SetActive(false);
            }
            base.Close(ignoreCloseAnimation);
        }

        public void Show(
            string title,
            string content,
            string labelYes = "UI_OK",
            bool localize = true,
            SystemType type = SystemType.Error)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(title, content, labelYes, localize, type);
                return;
            }

            Set(title, content, labelYes, "", localize, type);
            Show();
        }

        public void ShowWithTwoButton(
            string title,
            string content,
            string labelYes = "UI_OK",
            string labelNo = "UI_CANCEL",
            bool localize = true,
            SystemType type = SystemType.Error)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                ShowWithTwoButton(title, content, labelYes, labelNo, localize, type);
                return;
            }

            Set(title, content, labelYes, labelNo, localize, type);
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
                UnityEngine.Application.Quit();
#endif
            };
        }

        private IEnumerator CoCheckBlockIndex(long blockIndex)
        {
            yield return new WaitWhile(() => Game.Game.instance.Agent.BlockIndex == blockIndex);
            CancelCallback = null;
            Close();
        }

        private void Set(
            string title,
            string content,
            string labelYes = "UI_OK",
            string labelNo = "UI_CANCEL",
            bool localize = true,
            SystemType type = SystemType.Error)
        {
            SetUIByType(type);

            void Test(string text, Action<bool> setActive, Action<string> setText)
            {
                if (string.IsNullOrEmpty(text))
                {
                    setActive?.Invoke(false);
                }
                else
                {
                    setText?.Invoke(localize
                        ? L10nManager.Localize(text)
                        : text);
                    setActive?.Invoke(true);
                }
            }

            Test(title, _titleText.gameObject.SetActive, text => _titleText.text = text);
            Test(content, _contentText.gameObject.SetActive, text => _contentText.text = text);
            Test(labelYes, _confirmButton.gameObject.SetActive, text => _confirmButton.Text = text);
            Test(labelNo, _cancelButton.gameObject.SetActive, text => _cancelButton.Text = text);
        }

        private void Confirm()
        {
            Close();
            ConfirmCallback?.Invoke();
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
            uiBySystemType[(int) type].rootGameObject.SetActive(true);
        }
    }
}
