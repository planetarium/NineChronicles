using System;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class InviteFriendsPopup : PopupWidget
    {
        [Serializable]
        private class EnterReferralCodePopup
        {
            public GameObject gameObject;
            public Button closeButton;
            public Button enterButton;
            public TMP_InputField inputField;

            private Action<string> _onEnter;

            public void Init()
            {
                inputField.onValueChanged.AddListener(value =>
                {
                    enterButton.interactable = !string.IsNullOrEmpty(value);
                });

                enterButton.onClick.AddListener(() =>
                {
                    if (string.IsNullOrEmpty(inputField.text))
                    {
                        return;
                    }

                    _onEnter.Invoke(inputField.text);
                    gameObject.SetActive(false);
                    inputField.text = string.Empty;
                });

                closeButton.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                });
            }

            public void Show(Action<string> onEnter)
            {
                _onEnter = onEnter;
                inputField.text = string.Empty;
                gameObject.SetActive(true);
            }
        }

        [Serializable]
        private class ErrorPopup
        {
            public GameObject gameObject;
            public Button closeButton;
            public TextMeshProUGUI text;

            public void Init()
            {
                closeButton.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                });
            }

            public void Show(string message)
            {
                gameObject.SetActive(true);
                text.text = message;
            }
        }

        [SerializeField]
        private TextMeshProUGUI referralRewardText;

        [SerializeField]
        private TextMeshProUGUI referralCodeText;

        [SerializeField]
        private Button copyReferralCodeButton;

        [SerializeField]
        private Button shareReferralCodeButton;

        [SerializeField]
        private Button enterReferralCodeButton;

        [SerializeField]
        private EnterReferralCodePopup enterReferralCodePopup;

        [SerializeField]
        private ErrorPopup errorPopup;

        private string _referralCode;
        private string _referralUrl;

        protected override void Awake()
        {
            base.Awake();

            copyReferralCodeButton.onClick.AddListener(() =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_REFERRAL_CODE_COPY"),
                    NotificationCell.NotificationType.Notification);
                ClipboardHelper.CopyToClipboard(_referralCode);
            });

            shareReferralCodeButton.onClick.AddListener(() =>
            {
                new NativeShare()
                    .SetSubject(L10nManager.Localize("UI_SHARE_REFERRAL_CODE_TITLE"))
                    .SetText(L10nManager.Localize("UI_SHARE_REFERRAL_CODE_CONTENT",
                        _referralCode, _referralUrl))
                    .Share();
            });

            enterReferralCodeButton.onClick.AddListener(() =>
            {
                enterReferralCodePopup.Show(EnterReferralCode);
            });

            enterReferralCodePopup.Init();
            errorPopup.Init();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync();
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            var referralInfo = await Game.Game.instance.PortalConnect.GetReferralInformation();

            referralRewardText.text = L10nManager.Localize(
                "UI_INVITE_FRIENDS_BANNER_DESC",
                referralInfo.inviterReward,
                referralInfo.inviteeReward,
                referralInfo.inviteeLevelReward,
                referralInfo.requiredLevel);
            referralCodeText.text = referralInfo.referralCode;
            enterReferralCodeButton.gameObject.SetActive(!referralInfo.isRegistered);

            _referralCode = referralInfo.referralCode;
            _referralUrl = referralInfo.referralUrl;

            base.Show(ignoreShowAnimation);
        }

        private async void EnterReferralCode(string referralCode)
        {
            var result = await Game.Game.instance.PortalConnect.EnterReferralCode(referralCode);
            if (result.resultCode == 1000)
            {
                enterReferralCodeButton.gameObject.SetActive(false);
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_REFERRAL_CODE_ENTER_SUCCESS"),
                    NotificationCell.NotificationType.Notification);
            }
            else
            {
                if (result.resultCode == 4001)
                {
                    enterReferralCodeButton.gameObject.SetActive(false);
                }

                errorPopup.Show($"{result.title}\n{result.message}\n({result.resultCode})");
            }
        }
    }
}
