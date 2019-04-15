using DG.Tweening;
using Nekoyume.Game;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DamageText : HudWidget
    {
        private const float TweenDuration = 0.3f;
        private const float DestroyDelay = 0.8f;
        
        private static readonly Vector3 LocalScaleBefore = new Vector3(0.7f, 0.3f, 1f);
        private static readonly Vector3 LocalScaleAfter = new Vector3(1.5f, 1.5f, 1f);
        
        public TextMeshProUGUI label;

        private Vector3 _force;
        private Vector3 _addForce;

        public static DamageText Show(Vector3 position, Vector3 force, string text)
        {
            var result = Create<DamageText>(true);
            result.label.text = text;
            
            var rect = result.RectTransform;
            rect.anchoredPosition = position.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.localScale = LocalScaleBefore;

            var tweenPos = (position + force).ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.DOAnchorPos(tweenPos, TweenDuration).SetEase(Ease.OutBack);
            rect.DOScale(LocalScaleAfter, TweenDuration).SetEase(Ease.OutBack);
            
            Destroy(result.gameObject, DestroyDelay);

            return result;
        }
    }
}
