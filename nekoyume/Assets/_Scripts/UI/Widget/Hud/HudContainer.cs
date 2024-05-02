using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.UI
{
    public class HudContainer : HudWidget
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float maxHeight = 215;

        public void UpdatePosition(Camera canvasCamera, GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            var targetPos = targetPosition.ToCanvasPosition(canvasCamera, MainCanvas.instance.Canvas);
            targetPos.y = Mathf.Min(targetPos.y, maxHeight);

            RectTransform.anchoredPosition = targetPos;
            RectTransform.localScale = new Vector3(1, 1);
        }

        public void UpdateAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
    }
}
