using System;
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

        protected override void Awake()
        {
            base.Awake();

            copyReferralCodeButton.onClick.AddListener(() =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_REFERRAL_CODE_COPY"),
                    NotificationCell.NotificationType.Notification);
            });

            shareReferralCodeButton.onClick.AddListener(() =>
            {
                Debug.LogError("ShareReferralCodeButton");
            });

            enterReferralCodeButton.onClick.AddListener(() =>
            {
                enterReferralCodePopup.Show(OnEnterReferralCode);
            });

            enterReferralCodePopup.Init();
            errorPopup.Init();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var inviterReward = 5;
            var inviteeReward = 1;
            var inviteeReward2 = 4;
            var inviteeRewardLevel = 50;
            referralRewardText.text = L10nManager.Localize("UI_INVITE_FRIENDS_BANNER_DESC",
                inviterReward, inviteeReward, inviteeReward2, inviteeRewardLevel);

            var referralCode = "123456";
            referralCodeText.text = referralCode;

            var isReferralCodeEntered = false;
            enterReferralCodeButton.gameObject.SetActive(!isReferralCodeEntered);

            base.Show(ignoreShowAnimation);
        }

        private void OnEnterReferralCode(string referralCode)
        {
            var success = true;
            if (success)
            {
                enterReferralCodeButton.gameObject.SetActive(false);
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_REFERRAL_CODE_ENTER_SUCCESS"),
                    NotificationCell.NotificationType.Notification);
            }
            else
            {
                errorPopup.Show(L10nManager.Localize(""));
            }
        }
    }
}
