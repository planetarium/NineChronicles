using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    // todo: `AnchoredPositionSingleTweener`로 바꾸고, 좌표계를 선택할 수 있도록. `RotateSingleTweener` 참고.
    [RequireComponent(typeof(RectTransform))]
    public class AnchoredPositionYTweener : MonoBehaviour
    {
        public TweenCallback onComplete = null;
        public TweenCallback onReverseComplete = null;

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
        private Tweener _tweener;

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
            _tweener = RectTransform
                .DOAnchorPosY(end, duration, snapping)
                .SetDelay(startDelay)
                .SetEase(showEase);

            if (isFrom)
            {
                _tweener.From();
            }

            _tweener.onComplete = onComplete;
            return _tweener.Play();
        }

        public Tweener PlayReverse()
        {
            KillTween();
            RectTransform.anchoredPosition = OriginAnchoredPosition + new Vector2(0f, end);
            _tweener = RectTransform
                .DOAnchorPosY(OriginAnchoredPosition.y, duration, snapping)
                .SetEase(closeEase);

            if (isFrom)
            {
                _tweener.From();
            }

            _tweener.onComplete = onReverseComplete;
            return _tweener.Play();
        }

        public void KillTween()
        {
            if (_tweener?.IsPlaying() ?? false)
            {
                _tweener?.Kill();
            }

            _tweener = null;
        }
    }
}
