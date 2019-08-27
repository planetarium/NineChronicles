using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
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

        public void Awake()
        {
            hasNewMail.gameObject.SetActive(false);
            Game.Event.OnCombinationEnd.AddListener(OnCombinationEnd);
            mailButton.onClick.AddListener(mail.Toggle);
        }

        private void OnCombinationEnd(bool isSuccess)
        {
            if (isSuccess)
            {
                hasNewMail.gameObject.SetActive(true);
            }
        }

        private void OnEnable()
        {
            var currentAvatarState = States.Instance.currentAvatarState.Value;
            if (currentAvatarState != null)
                hasNewMail.gameObject.SetActive(currentAvatarState.mailBox.Any(i => i.New));
        }

        private void OnDisable()
        {
            mail.gameObject.SetActive(false);
        }
    }
}
