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
        [SerializeField]
        private bool isPositionAnchored = false;

        private RectTransform rectTransform;

        [ReadOnly]
        [SerializeField]
        private Vector2 originAnchoredPosition;

#if UNITY_EDITOR
        [ContextMenu("SetPositionAnchored")]
        public void SetPositionAnchored()
        {
            originAnchoredPosition = GetComponent<RectTransform>().anchoredPosition;
            isPositionAnchored = true;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (!isPositionAnchored)
            {
                originAnchoredPosition = rectTransform.anchoredPosition;
            }
            ActionCamera.instance.Test += CalculateNotchSizeAndAdjustPosition;
        }

        public void CalculateNotchSizeAndAdjustPosition()
        {
            //Debug.Log($"safeArea{Screen.safeArea.width} {Screen.safeArea.height}");
            //Debug.Log($"screenArea{Screen.width} {Screen.height}");
            Vector3 worldPosition = rectTransform.TransformPoint(rectTransform.rect.center);
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            if (screenPosition.x < Screen.safeArea.center.x)
            {
                //left
                rectTransform.anchoredPosition = originAnchoredPosition + new Vector2(Screen.safeArea.xMin, 0);
            }
            else
            {
                //right
                var rightBoader = Screen.width - Screen.safeArea.xMax;
                rectTransform.anchoredPosition = originAnchoredPosition + new Vector2(-rightBoader, 0);
            }
            /*RectTransform rectTransform = GetComponent<RectTransform>();
            Vector2 anchorMin = Screen.safeArea.position;
            Vector2 anchorMax = Screen.safeArea.position + Screen.safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;*/
        }
    }
}
