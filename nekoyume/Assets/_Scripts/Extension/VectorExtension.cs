using UnityEngine;

namespace Nekoyume
{
    public static class VectorExtension
    {
        public static Vector3 ToCanvasPosition(this Vector3 worldPosition, Camera camera, Canvas canvas)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            var canvasRectSizeDelta = canvasRect.sizeDelta;
            var viewportPoint = camera.WorldToViewportPoint(worldPosition);
            var screenPosition = new Vector2(
                (viewportPoint.x - 0.5f) * canvasRectSizeDelta.x,
                (viewportPoint.y - 0.5f) * canvasRectSizeDelta.y);
            return screenPosition;
        }
    }
}
