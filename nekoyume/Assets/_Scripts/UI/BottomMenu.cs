using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class BottomMenu : MonoBehaviour
    {
        [Serializable]
        public struct Button
        {
            public UnityEngine.UI.Button button;
            public Image image;
            public TextMeshProUGUI text;
            public string localizationKey;
        }

        public Image hasNewMail;

        public Mail mail;

        // 공용 버튼.
        public Button goToMainButton;
        public Button mailButton;
        public Button questButton;
        public Button chatButton;
        public Button collectionButton;

        public Button settingButton;

        // 추가 버튼.
        public Button avatarInfoButton;
        public Button inventoryButton;
        public Button switchBuyButton;
        public Button switchSellButton;
        public Button combinationEquipmentButton;
        public Button combinationConsumableButton;
        public Button combinationRecipeButton;
        public Button worldMapButton;
        public Button stageButton;
        public Button autoRepeatButton;

        private Button[] _buttons;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _isInitialized = false;


        public void Awake()
        {
            void ShowUnderConstructPopup()
            {
                var alert = Widget.Find<Alert>();
                alert.Show(null, "UI_NOT_IMPLEMENTED", "UI_OK", true);
            }

            hasNewMail.gameObject.SetActive(false);
            mailButton.button.onClick.AddListener(mail.Toggle);

            if (!_isInitialized)
            {
                _buttons = new Button[]
                {
                    goToMainButton,
                    mailButton,
                    questButton,
                    chatButton,
                    collectionButton,
                    settingButton,
                    avatarInfoButton,
                    inventoryButton,
                    switchBuyButton,
                    switchSellButton,
                    combinationEquipmentButton,
                    combinationConsumableButton,
                    combinationRecipeButton,
                    worldMapButton,
                    stageButton,
                    autoRepeatButton
                };

                chatButton.button.onClick.AddListener(ShowUnderConstructPopup);
                collectionButton.button.onClick.AddListener(ShowUnderConstructPopup);
                settingButton.button.onClick.AddListener(ShowUnderConstructPopup);
            }

            foreach (var btn in _buttons)
            {
                if (btn.text != null)
                {
                    btn.text.text = LocalizationManager.Localize(btn.localizationKey);
                }
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (ReactiveCurrentAvatarState.MailBox is null)
                return;

            _disposables.DisposeAllAndClear();
            ReactiveCurrentAvatarState.MailBox.Subscribe(HasNewMail).AddTo(_disposables);
        }

        private void OnDisable()
        {
            foreach (var btn in _buttons)
            {
                if (btn.image != null)
                    btn.image.SetNativeSize();
            }

            mail.gameObject.SetActive(false);
            _disposables.DisposeAllAndClear();
        }

        private void HasNewMail(MailBox mailBox)
        {
            hasNewMail.gameObject.SetActive(!(mailBox is null) && mailBox.Any(i => i.New));
        }
    }
}
