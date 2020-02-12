using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.Buff;

namespace Nekoyume.UI.Module
{
    public class BossStatus : MonoBehaviour
    {
        public Image hpBar;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI infoText;
        public Image portrait;
        public BuffLayout buffLayout;
        public Animator animator;

        public void Show()
        {
            gameObject.SetActive(true);
            buffLayout.SetBuff(null);

            animator.enabled = true;
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

        public void SetHp(int current, int max)
        {
            current = Mathf.Max(current, 0);
            hpBar.fillAmount = (float) current / max;
            hpText.text = $"{current}/{max}";
        }

        public void SetProfile(int level, string name, Sprite sprite = null)
        {
            infoText.text = $"<color=#B38271>Lv.{level}</color> {name}";
            portrait.overrideSprite = sprite; 
        }

        public void SetBuff(Dictionary<int, Buff> modelBuffs)
        {
            buffLayout.SetBuff(modelBuffs);
        }

        public void OnShowComplete()
        {
            animator.enabled = false;
        }

        public void OnCloseComplete()
        {
            animator.enabled = false;
            
            gameObject.SetActive(false);
        }
    }
}
