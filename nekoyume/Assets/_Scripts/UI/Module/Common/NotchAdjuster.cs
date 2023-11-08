using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Common
{
    using Nekoyume.Game;
    using UniRx;
    public class NotchAdjuster : MonoBehaviour
    {
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ActionCamera.instance.Test += CalculateNotchSizeAndAdjustPosition;
        }

        public void CalculateNotchSizeAndAdjustPosition()
        {
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            anchorMin.x = safeArea.x / Screen.width;
            anchorMax.x = (safeArea.x + safeArea.width) / Screen.width;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}
