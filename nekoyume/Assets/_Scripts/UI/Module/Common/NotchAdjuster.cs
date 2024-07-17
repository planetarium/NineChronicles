using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    using Game;

    public class NotchAdjuster : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 originAnchorMin;
        private Vector2 originAnchorMax;

        public enum NotchUIType
        {
            Normal,
            EventBanner,
            Left,
            Right
        }

        [SerializeField]
        private NotchUIType NotchType = NotchUIType.Normal;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originAnchorMin = rectTransform.anchorMin;
            originAnchorMax = rectTransform.anchorMax;

            ActionCamera.instance.OnResolutionDynamic += CalculateNotchSizeAndAdjustPosition;
            ActionCamera.instance.OnResolutionStatic += CalculateNotchSizeAndAdjustPosition;

            switch (NotchType)
            {
                case NotchUIType.Normal:
                    RefreshNotchByScreenState();
                    break;
                case NotchUIType.EventBanner:
                    break;
                case NotchUIType.Left:
                    RefreshNotchByScreenState();
                    break;
                case NotchUIType.Right:
                    RefreshNotchByScreenState();
                    break;
                default:
                    break;
            }
        }

        public void RefreshNotchByScreenState()
        {
            CalculateNotchSizeAndAdjustPosition();
        }

        private void CalculateNotchSizeAndAdjustPosition()
        {
            var safeArea = Screen.safeArea;

            var anchorMin = rectTransform.anchorMin;
            var anchorMax = rectTransform.anchorMax;

            switch (NotchType)
            {
                case NotchUIType.Normal:
                    anchorMin.x = safeArea.x / Screen.width;
                    anchorMax.x = (safeArea.x + safeArea.width) / Screen.width;
                    break;
                case NotchUIType.EventBanner:
                    anchorMin.x = anchorMax.x = (safeArea.x + safeArea.width) / Screen.width;
                    break;
                case NotchUIType.Left:
                    anchorMin.x = anchorMax.x = safeArea.x / Screen.width;
                    break;
                case NotchUIType.Right:
                    anchorMin.x = anchorMax.x = (safeArea.x + safeArea.width) / Screen.width;
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
