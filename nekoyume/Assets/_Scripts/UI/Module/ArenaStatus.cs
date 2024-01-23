using System;
using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.Game;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.Buff;

namespace Nekoyume.UI.Module
{
    public class ArenaStatus : MonoBehaviour
    {
        [SerializeField]
        private DetailedCharacterView characterView = null;

        [SerializeField]
        private Image hpBar;

        [SerializeField]
        private TextMeshProUGUI hpText;

        [SerializeField]
        private TextMeshProUGUI infoText;

        [SerializeField]
        private BuffLayout buffLayout;

        [SerializeField]
        private Animator animator;

        public void OnDisable()
        {
            gameObject.SetActive(false);
        }

        public void Set(int portraitId, string avatarName, int level, Address address)
        {
            SetProfile(portraitId, avatarName, level, address);
            SetBuff();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            animator.Play("Show");
        }

        public void Close(bool ignoreAnimation = true)
        {
            animator.Play("Close");
        }

        private void SetProfile(int portraitId, string avatarName, int level, Address address)
        {
            if (Dcc.instance.Avatars.TryGetValue(address.ToString(), out var dccId))
            {
                characterView.SetByDccId(dccId, level);
            }
            else
            {
                characterView.SetByFullCostumeOrArmorId(portraitId, level);
            }

            infoText.text = avatarName;
        }

        public void SetHp(long current, long max)
        {
            current = Math.Max(current, 0);
            hpBar.fillAmount = (float) current / max;
            hpText.text = $"{current}/{max}";
        }

        public void SetBuff(Dictionary<int, Buff> modelBuffs = null)
        {
            buffLayout.SetBuff(modelBuffs);
        }

        public void OnCompleteOfShowAnimation()
        {
            animator.enabled = false;
        }

        public void OnCompleteOfCloseAnimation()
        {
            animator.enabled = false;

            gameObject.SetActive(false);
        }
    }
}
