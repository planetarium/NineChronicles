using System;
using System.Collections.Generic;
using System.Linq;
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
        public Button avatarStatusButton;
        public Button inventoryButton;
        public Button switchBuyButton;
        public Button switchSellButton;
        public Button combinationEquipmentButton;
        public Button combinationConsumableButton;
        public Button combinationRecipeButton;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Awake()
        {
            hasNewMail.gameObject.SetActive(false);
            mailButton.button.onClick.AddListener(mail.Toggle);
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
            mail.gameObject.SetActive(false);
            _disposables.DisposeAllAndClear();
        }

        private void HasNewMail(MailBox mailBox)
        {
            hasNewMail.gameObject.SetActive(!(mailBox is null) && mailBox.Any(i => i.New));
        }

    }
}
