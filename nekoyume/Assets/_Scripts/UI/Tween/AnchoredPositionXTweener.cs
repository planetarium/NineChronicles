using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    // todo: `AnchoredPositionSingleTweener`로 바꾸고, 좌표계를 선택할 수 있도록. `RotateSingleTweener` 참고.
    [RequireComponent(typeof(RectTransform))]
    public class AnchoredPositionXTweener : BaseTweener
    {
        [SerializeField]
        private float startDelay = 0f;

        [SerializeField]
        private float end = 0f;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private bool snapping = false;

        [SerializeField]
        private Ease showEase = Ease.Linear;

        [SerializeField]
        private Ease closeEase = Ease.Linear;

        [SerializeField]
        private bool isFrom = false;

        private RectTransform _rectTransformCache;
        private Vector2? _originAnchoredPositionCache;
        
        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        private Vector2 OriginAnchoredPosition =>
            _originAnchoredPositionCache
            ?? (_originAnchoredPositionCache = RectTransform.anchoredPosition).Value;

        public Tweener PlayTween()
        {
            KillTween();
            RectTransform.anchoredPosition = OriginAnchoredPosition;
            Tweener = RectTransform
                .DOAnchorPosX(end, duration, snapping)
                .SetDelay(startDelay)
                .SetEase(showEase);

            if (isFrom)
            {
                Tweener.From();
            }

            return Tweener.Play();
        }

        public Tweener PlayReverse()
        {
            KillTween();
            RectTransform.anchoredPosition = OriginAnchoredPosition + new Vector2(end, 0f);
            Tweener = RectTransform
                .DOAnchorPosX(OriginAnchoredPosition.x, duration, snapping)
                .SetEase(closeEase);

            if (isFrom)
            {
                Tweener.From();
            }

            return Tweener.Play();
        }
    }
}
