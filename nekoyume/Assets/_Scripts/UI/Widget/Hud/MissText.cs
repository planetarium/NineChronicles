using DG.Tweening;
using Nekoyume.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MissText : HudWidget
    {
        private const float TweenDuration = 0.3f;
        private const float DestroyDelay = 1.4f;

        private static readonly Vector3 LocalScaleBefore = new Vector3(2.4f, 2.4f, 1f);
        private static readonly Vector3 LocalScaleAfter = new Vector3(1.4f, 1.4f, 1f);

        public CanvasGroup group;
        public Image[] images;

        public static MissText Show(Camera camera, Vector3 position, Vector3 force, int index)
        {
            var result = Create<MissText>(true);
            for (var i = 0; i < result.images.Length; i++)
            {
                result.images[i].gameObject.SetActive(false);
                if (i == index)
                {
                   result.images[i].gameObject.SetActive(true);
                }
            }

            var rect = result.RectTransform;
            rect.anchoredPosition = position.ToCanvasPosition(camera, MainCanvas.instance.Canvas);
            rect.localScale = LocalScaleBefore;

            var tweenPos = (position + force).ToCanvasPosition(camera, MainCanvas.instance.Canvas);
            rect.DOScale(LocalScaleAfter, TweenDuration).SetEase(Ease.OutCubic);
            rect.DOAnchorPos(tweenPos, TweenDuration * 2.0f).SetEase(Ease.InOutQuad).SetDelay(TweenDuration);
            result.group.DOFade(0.0f, TweenDuration * 2.0f).SetDelay(TweenDuration).SetEase(Ease.InCirc);

            Destroy(result.gameObject, DestroyDelay);

            return result;
        }

    }
}
