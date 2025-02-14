using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class SweepSlider : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        [SerializeField]
        private TextMeshProUGUI maxText;

        [SerializeField]
        private TextMeshProUGUI curText;

        [SerializeField]
        private float sliderMinWidth;

        [SerializeField]
        private float sliderMaxWidth;

        [SerializeField]
        private GameObject container;

        [SerializeField]
        private GameObject empty;

        [SerializeField]
        private GameObject maskBackground;

        [SerializeField]
        private GameObject background;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Set(int sliderMinValue, int sliderMaxValue, int sliderCurValue,
            int max, int multiplier,
            Action<int> callback = null, bool interactable = true, bool isMax = false)
        {
            UpdateListener(multiplier, callback);
            var curValue = isMax ? max : sliderCurValue;
            UpdateSliderValues(sliderMinValue, sliderMaxValue, curValue);
            UpdateText(curValue, max, multiplier);
            UpdateContainer(sliderMaxValue);
            UpdateSliderWidth(sliderMaxValue, max, multiplier);
            UpdateBackground(sliderMaxValue, max, multiplier);
            slider.interactable = interactable;
        }

        public void ForceMove(int value)
        {
            curText.text = $"{value}";
            slider.value = value;
        }

        private void UpdateListener(int multiplier, Action<int> callback)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v =>
            {
                callback?.Invoke((int)v);
                curText.text = $"{v * multiplier}";
            });
        }

        private void UpdateSliderValues(int sliderMinValue, int sliderMaxValue, int sliderCurValue)
        {
            slider.minValue = sliderMinValue;
            slider.maxValue = sliderMaxValue;
            slider.value = sliderCurValue;
        }

        private void UpdateText(int sliderCurValue, int max, int multiplier)
        {
            maxText.text = max.ToString();
            curText.text = $"{sliderCurValue * multiplier}";
        }

        private void UpdateContainer(int sliderMaxValue)
        {
            var isActive = sliderMaxValue > 0;
            container.SetActive(isActive);
            empty.SetActive(!isActive);
        }

        private void UpdateSliderWidth(int sliderMaxValue, int max, int multiplier)
        {
            var each = max / multiplier;
            var interval = sliderMaxWidth / each;
            var width = Mathf.Max(sliderMinWidth, interval * sliderMaxValue);
            if (math.isnan(width))
            {
                width = sliderMinWidth;
            }
            _rectTransform.sizeDelta = new Vector2(width, _rectTransform.sizeDelta.y);
        }

        private void UpdateBackground(int sliderMaxValue, int max, int multiplier)
        {
            var isActiveMaskBg = max == sliderMaxValue * multiplier;
            maskBackground.SetActive(isActiveMaskBg);
            background.SetActive(!isActiveMaskBg);
        }
    }
}
