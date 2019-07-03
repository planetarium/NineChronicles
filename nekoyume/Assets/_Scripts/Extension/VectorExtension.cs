using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume
{
    public static class VectorExtension
    {
        public static float2 WorldToScreen(this Vector3 position, Camera camera, Canvas canvas)
        {
            return WorldToScreen((float3) position, camera, canvas);
        }
        
        public static float2 WorldToScreen(this float3 position, Camera camera, Canvas canvas)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            var canvasRectSizeDelta = canvasRect.sizeDelta;
            var viewportPoint = camera.WorldToViewportPoint(position);
            var screenPosition = new float2(
                (viewportPoint.x - 0.5f) * canvasRectSizeDelta.x,
                (viewportPoint.y - 0.5f) * canvasRectSizeDelta.y);
            return screenPosition;
        }
    }
}
