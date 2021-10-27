using Nekoyume.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ProgressBar : HudWidget
    {
        private const float Margin = 50f;

        public Slider slider;
        public Sprite greenBar;
        public Sprite redBar;
        public Color greenColor;
        public Color redColor;

        public Image bar;
        public TextMeshProUGUI label;
        public float colorChangeThreshold = 0.35f;

        public void Set(int current, int max)
        {
            SetText($"{current} / {max}");
            SetValue((float) current / max);
        }

        protected void SetText(string text)
        {
            label.text = text;
        }

        protected void SetValue(float value)
        {
            slider.value = value;

            if (value < colorChangeThreshold)
            {
                label.color = redColor;
                bar.sprite = redBar;
            }
            else
            {
                label.color = greenColor;
                bar.sprite = greenBar;
            }
        }
    }
}
