using DG.Tweening;
using Nekoyume.Game;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CriticalText : HudWidget
    {
        private const float TweenDuration = 0.3f;
        private const float DestroyDelay = 1.4f;
        
        private static readonly Vector3 LocalScaleBefore = new Vector3(2.4f, 2.4f, 1f);
        private static readonly Vector3 LocalScaleAfter = new Vector3(1.4f, 1.4f, 1f);
        
        public TextMeshProUGUI[] labels;
        public TextMeshProUGUI[] shadows;
        public CanvasGroup group;

        public static CriticalText Show(Vector3 position, Vector3 force, string text, DamageText.TextGroupState group)
        {
            var result = Create<CriticalText>(true);
            for (var i = 0; i < result.labels.Length; i++)
            {
                result.labels[i].gameObject.SetActive(false);
                result.shadows[i].gameObject.SetActive(false);
                if ((int) group == i)
                {
                    result.labels[i].gameObject.SetActive(true);
                    result.shadows[i].gameObject.SetActive(true);
                    result.labels[i].text = text;
                    result.shadows[i].text = text;
                }
            }

            var rect = result.RectTransform;
            rect.anchoredPosition = position.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.localScale = LocalScaleBefore;

            var tweenPos = (position + force).ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.DOScale(LocalScaleAfter, TweenDuration).SetEase(Ease.OutCubic);
            rect.DOAnchorPos(tweenPos, TweenDuration * 2.0f).SetEase(Ease.InOutQuad).SetDelay(TweenDuration);
            result.group.DOFade(0.0f, TweenDuration * 2.0f).SetDelay(TweenDuration).SetEase(Ease.InCirc);
            
            Destroy(result.gameObject, DestroyDelay);

            return result;
        }
    }
}
