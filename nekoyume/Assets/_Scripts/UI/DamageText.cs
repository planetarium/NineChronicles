using DG.Tweening;
using Nekoyume.Game;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DamageText : HudWidget
    {
        private const float TweenDuration = 0.3f;
        private const float DestroyDelay = 1.4f;
        
        private static readonly Vector3 LocalScaleBefore = new Vector3(2.4f, 2.4f, 1f);
        private static readonly Vector3 LocalScaleAfter = new Vector3(1.2f, 1.2f, 1f);
        
        public TextMeshProUGUI label;
        public TextMeshProUGUI shadow;
        public CanvasGroup group;

        public static DamageText Show(Vector3 position, Vector3 force, string text)
        {
            var result = Create<DamageText>(true);
            result.label.text = text;
            result.shadow.text = text;
            
            var rect = result.RectTransform;
            rect.anchoredPosition = position.WorldToScreen(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.localScale = LocalScaleBefore;

            var tweenPos = (position + force).WorldToScreen(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.DOScale(LocalScaleAfter, TweenDuration).SetEase(Ease.OutCubic);
            rect.DOAnchorPos(tweenPos, TweenDuration * 2.0f).SetEase(Ease.InOutQuad).SetDelay(TweenDuration);
            result.group.DOFade(0.0f, TweenDuration * 2.0f).SetDelay(TweenDuration).SetEase(Ease.InCirc);
            
            Destroy(result.gameObject, DestroyDelay);

            return result;
        }
    }
}
