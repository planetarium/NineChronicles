using System;
using System.Collections;
using Nekoyume.Blockchain;
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

        private SystemType _type;

        public System.Action ConfirmCallback { get; set; }
        public System.Action CancelCallback { get; set; }

        public TextMeshProUGUI ContentText => _contentText;

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
                    if (_type != SystemType.Information)
                    {
                        ClipboardHelper.CopyToClipboard(ui.contentText.text);
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_COPIED"),
                            NotificationCell.NotificationType.Information);
                    }
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
            Set(title, content, labelYes, "", localize, type);
            if (gameObject.activeSelf)
            {
                return;
            }

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
            Set(title, content, labelYes, labelNo, localize, type);
            if (gameObject.activeSelf)
            {
                return;
            }

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

        public void SetConfirmCallbackToExit()
        {
            ConfirmCallback = () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                UnityEngine.Application.Quit();
#endif
            };
        }

        public void SetCancelCallbackToBackup()
        {
            CancelCallback = () =>
            {
                var agent = Game.Game.instance.Agent;
                var cachedPassphrase = KeyManager.GetCachedPassphrase(
                    agent.Address,
                    Util.AesDecrypt,
                    defaultValue: string.Empty);
                if (cachedPassphrase.Equals(string.Empty))
                {
                    Find<LoginSystem>().ShowResetPassword();
                }
                else
                {
                    new NativeShare().AddFile(Util.GetQrCodePngFromKeystore(), "shareQRImg.png")
                        .SetSubject(L10nManager.Localize("UI_SHARE_QR_TITLE"))
                        .SetText(L10nManager.Localize("UI_SHARE_QR_CONTENT"))
                        .SetCallback((_, _) =>
                        {
#if UNITY_EDITOR
                            UnityEditor.EditorApplication.ExitPlaymode();
#else
                            UnityEngine.Application.Quit();
#endif
                        })
                        .Share();
                }
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
            _type = type;

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
            Close(true);
            ConfirmCallback?.Invoke();
            AudioController.PlayClick();
        }

        public void Cancel()
        {
            CancelCallback?.Invoke();
            Close(true);
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
