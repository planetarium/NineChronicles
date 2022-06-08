using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.Buff;

namespace Nekoyume.UI.Module
{
    public class ArenaStatus : MonoBehaviour
    {
        [SerializeField]
        private Image hpBar;

        [SerializeField]
        private TextMeshProUGUI hpText;

        [SerializeField]
        private TextMeshProUGUI infoText;

        [SerializeField]
        private Image portrait;

        [SerializeField]
        private BuffLayout buffLayout;

        [SerializeField]
        private Animator animator;

        public void Show(Sprite sprite, string avatarName, int level)
        {
            gameObject.SetActive(true);
            animator.enabled = true;

            SetProfile(sprite, avatarName, level);
            SetBuff();
        }

        public void Close(bool ignoreAnimation = true)
        {
            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                return;
            }

            animator.enabled = true;
            animator.Play("Close");
        }

        private void SetProfile(Sprite sprite, string avatarName, int level)
        {
            infoText.text = $"<color=#B38271>Lv.{level}</color> {avatarName}";
            portrait.overrideSprite = sprite;
        }

        public void SetHp(int current, int max)
        {
            current = Mathf.Max(current, 0);
            hpBar.fillAmount = (float) current / max;
            hpText.text = $"{current}/{max}";
        }

        public void SetBuff(Dictionary<int, Buff> modelBuffs = null)
        {
            buffLayout.SetBuff(modelBuffs);
        }
    }
}
