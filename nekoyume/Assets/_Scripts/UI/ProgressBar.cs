using Nekoyume.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ProgressBar : HudWidget
    {
        private const float Margin = 50f;
        
        public Sprite greenBar;
        public Sprite redBar;
        public Color greenColor;
        public Color redColor;

        public Image bar;
        public Text label;
        public Text[] labelShadows;

        public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            RectTransform.anchoredPosition = targetPosition.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
        }

        public void SetText(string text)
        {
            label.text = text;

            if (labelShadows.Length == 0)
                labelShadows = transform.Find("TextShadow").GetComponentsInChildren<Text>();
            foreach (var l in labelShadows)
            {
                l.text = text;
            }
        }

        public void SetValue(float value)
        {
            Slider slider = gameObject.GetComponent<Slider>();
            if (slider == null)
                return;

            if (value <= 0.0f)
                slider.fillRect.gameObject.SetActive(false);
            else
                slider.fillRect.gameObject.SetActive(true);

            if (value > 1.0f)
                value = 1.0f;
            if (value < 0.1f)
                value = 0.1f;

            slider.value = value;

            if (value < 0.35f)
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
