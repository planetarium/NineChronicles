using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume
{
    public static class VectorExtensions
    {
        public static float2 ToCanvasPosition(this Vector3 worldPosition, Camera camera, Canvas canvas)
        {
            return ToCanvasPosition((float3) worldPosition, camera, canvas);
        }

        public static float2 ToCanvasPosition(this float3 worldPosition, Camera camera, Canvas canvas)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            var canvasRectSizeDelta = canvasRect.sizeDelta;
            var viewportPoint = camera.WorldToViewportPoint(worldPosition);
            var screenPosition = new float2(
                (viewportPoint.x - 0.5f) * canvasRectSizeDelta.x,
                (viewportPoint.y - 0.5f) * canvasRectSizeDelta.y);
            return screenPosition;
        }
    }
}
