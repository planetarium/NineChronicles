using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class ProgressBar : HUD
    {
        public Sprite greenBar;
        public Sprite redBar;
        public Color greenColor;
        public Color redColor;

        public Image bar;
        public Text label;
        public Text[] labelShadows;

        public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
        {
            Vector3 targetPosition = target.transform.position + offset;

            // https://answers.unity.com/questions/799616/unity-46-beta-19-how-to-convert-from-world-space-t.html
            float screenHeight = Screen.height * 0.5f;
            RectTransform canvasRect = transform.root.gameObject.GetComponent<RectTransform>();
            Vector2 viewportPosition = Camera.main.WorldToViewportPoint(targetPosition);
            Vector2 canvasPosition = new Vector2(
                ((viewportPosition.x * canvasRect.sizeDelta.x)),
                ((viewportPosition.y * canvasRect.sizeDelta.y)));
            if (canvasPosition.y > screenHeight)
            {
                float margin = 50.0f;
                canvasPosition.y = screenHeight - margin;
            }
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = canvasPosition;
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
