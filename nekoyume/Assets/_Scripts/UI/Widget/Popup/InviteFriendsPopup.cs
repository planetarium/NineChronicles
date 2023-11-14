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

        private PortalConnect.ReferralResult _referralInformation;

        protected override void Awake()
        {
            base.Awake();

            copyReferralCodeButton.onClick.AddListener(() =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_REFERRAL_CODE_COPY"),
                    NotificationCell.NotificationType.Notification);
                ClipboardHelper.CopyToClipboard(_referralInformation.referralCode);
            });

            shareReferralCodeButton.onClick.AddListener(() =>
            {
                new NativeShare()
                    .SetSubject(L10nManager.Localize("UI_SHARE_REFERRAL_CODE_TITLE"))
                    .SetText(L10nManager.Localize("UI_SHARE_REFERRAL_CODE_CONTENT",
                        _referralInformation.referralCode, _referralInformation.referralUrl))
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
            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            _referralInformation ??= await Game.Game.instance.PortalConnect.GetReferralInformation();

            Set(_referralInformation);
            base.Show(ignoreShowAnimation);
        }

        private void Set(PortalConnect.ReferralResult referralInformation)
        {
            referralRewardText.text = L10nManager.Localize(
                "UI_INVITE_FRIENDS_BANNER_DESC",
                referralInformation.inviterReward,
                referralInformation.inviteeReward,
                referralInformation.inviteeLevelReward,
                referralInformation.requiredLevel);
            referralCodeText.text = referralInformation.referralCode;
            enterReferralCodeButton.gameObject.SetActive(!referralInformation.isRegistered);
        }

        private async void EnterReferralCode(string referralCode)
        {
            enterReferralCodeButton.gameObject.SetActive(false);

            var result = await Game.Game.instance.PortalConnect.EnterReferralCode(referralCode);
            if (result.resultCode == 1000)
            {
                _referralInformation.isRegistered = true;

                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_REFERRAL_CODE_ENTER_SUCCESS"),
                    NotificationCell.NotificationType.Notification);
            }
            else
            {
                if (result.resultCode == 4001)
                {
                    _referralInformation.isRegistered = true;
                }

                errorPopup.Show($"{result.title}\n{result.message}\n({result.resultCode})");
            }

            enterReferralCodeButton.gameObject.SetActive(!_referralInformation.isRegistered);
        }
    }
}
