using System.Collections.Generic;
using Nekoyume.Game;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class BossStatus : MonoBehaviour
    {
        public Image hpBar;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI infoText;
        public Image portrait;
        public BuffLayout buffLayout;

        public void Show()
        {
            gameObject.SetActive(true);
            buffLayout.SetBuff(null);
        }

        public void Close() => gameObject.SetActive(false);

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
    }
}
