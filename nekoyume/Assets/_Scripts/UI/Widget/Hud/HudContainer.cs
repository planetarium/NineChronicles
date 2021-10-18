using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.UI
{
    public class HudContainer : HudWidget
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            RectTransform.anchoredPosition = targetPosition.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            RectTransform.localScale = new Vector3(1, 1);
        }

        public void UpdateAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
    }
}
