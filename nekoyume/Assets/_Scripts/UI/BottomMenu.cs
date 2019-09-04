using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class BottomMenu : MonoBehaviour
    {
        public Button goToMainButton;
        public Button inventoryButton;
        public Button questButton;
        public Button infoAndEquipButton;
        public Button mailButton;
        public Button dictionaryButton;
        public Image hasNewMail;
        public Mail mail;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Awake()
        {
            hasNewMail.gameObject.SetActive(false);
            mailButton.onClick.AddListener(mail.Toggle);
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
