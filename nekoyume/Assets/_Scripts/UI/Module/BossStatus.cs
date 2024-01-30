using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.Buff;
using Nekoyume.Model;
using Nekoyume.L10n;
using Nekoyume.Helper;

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

        public void SetHp(long current, long max)
        {
            current = Math.Max(current, 0);
            hpBar.fillAmount = (float) current / max;
            hpText.text = $"{current}/{max}";
        }

        public void SetProfile(Enemy enemy)
        {
            var level = enemy.Level;
            var name = L10nManager.LocalizeCharacterName(enemy.CharacterId);
            var sprite = SpriteHelper.GetCharacterIcon(enemy.CharacterId);
            SetProfile(level, name, sprite);
            SetHp(enemy.HP, enemy.HP);
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
