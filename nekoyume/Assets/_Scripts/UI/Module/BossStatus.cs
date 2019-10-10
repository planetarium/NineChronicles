using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class BossStatus : MonoBehaviour
    {
        public Slider hpBar;
        public Text hpText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI nameText;
        public Image profileImage;
        public BuffLayout buffLayout;

        public void Show() => gameObject.SetActive(true);

        public void Close() => gameObject.SetActive(false);

        public void SetHp(int current, int max)
        {
            current = Mathf.Max(current, 0);
            hpBar.value = (float) current / max;
            hpText.text = $"{current}/{max}";
        }

        public void SetProfile(int level, string name, Sprite sprite = null)
        {
            levelText.text = $"Lv.{level}";
            nameText.text = name;
            profileImage.overrideSprite = null;
        }

        public void SetBuff()
        {

        }
    }
}
