using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using Nekoyume.UI.Tween;
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
        // ¿ÞÂÊ
        public Button goToMainButton;
        public Button mailButton;
        public Button questButton;
        public Button chatButton;
        public Button collectionButton;
        public Button settingButton;
        // ¿À¸¥ÂÊ
        public Button avatarInfoButton;
        public Button inventoryButton;
        public Button switchBuyButton;
        public Button switchSellButton;
        public Button combinationEquipmentButton;
        public Button combinationConsumableButton;
        public Button combinationRecipeButton;
        public Button WorldMapButton;
        public Button AutoRepeatButton;

        private Button[] _buttons;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();


        public void Awake()
        {
            hasNewMail.gameObject.SetActive(false);
            mailButton.button.onClick.AddListener(mail.Toggle);

            if (_buttons is null)
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
                    WorldMapButton,
                    AutoRepeatButton
                };

            foreach (var btn in _buttons)
            {
                if (btn.text != null)
                    btn.text.text = LocalizationManager.Localize(btn.localizationKey);
            }
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
