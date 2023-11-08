using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    using Nekoyume.Game;
    public class NotchAdjuster : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 originAnchorMin;
        private Vector2 originAnchorMax;
        public enum NotchUIType
        {
            Normal,
            EventBanner
        }

        [SerializeField]
        private NotchUIType NotchType = NotchUIType.Normal;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originAnchorMin = rectTransform.anchorMin;
            originAnchorMax = rectTransform.anchorMax;

            ActionCamera.instance.OnResolutionDynamic += CalculateNotchSizeAndAdjustPosition;
            ActionCamera.instance.OnResolutionStatic += ResetNotchSizeAndAdjustPosition;

            switch (NotchType)
            {
                case NotchUIType.Normal:
                    RefreshNotchByScreenState();
                    break;
                case NotchUIType.EventBanner:
                    break;
                default:
                    break;
            }
        }

        public void RefreshNotchByScreenState()
        {
            if (ActionCamera.instance.IsResolutionDynamic)
            {
                CalculateNotchSizeAndAdjustPosition();
            }
            else
            {
                ResetNotchSizeAndAdjustPosition();
            }
        }

        private void CalculateNotchSizeAndAdjustPosition()
        {
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            switch (NotchType)
            {
                case NotchUIType.Normal:
                    anchorMin.x = safeArea.x / Screen.width;
                    anchorMax.x = (safeArea.x + safeArea.width) / Screen.width;
                    break;
                case NotchUIType.EventBanner:
                    anchorMax.x = (safeArea.x + safeArea.width) / Screen.width;
                    anchorMin.x = anchorMax.x;
                    break;
                default:
                    break;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }

        private void ResetNotchSizeAndAdjustPosition()
        {
            rectTransform.anchorMin = originAnchorMin;
            rectTransform.anchorMax = originAnchorMax;
        }
    }
}
