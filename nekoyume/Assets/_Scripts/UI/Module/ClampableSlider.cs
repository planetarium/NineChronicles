using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Slider))]
    public class ClampableSlider : MonoBehaviour
    {
        public Slider slider;
        public float thresholdRatio;

        public void Awake()
        {
            slider.onValueChanged.AddListener(value => Clamp(value, slider.maxValue));
        }

        public void Clamp(float value, float maxValue)
        {
            if (value / maxValue < thresholdRatio)
            {
                slider.value = 0.0f;
            }
        }
    }
}
