using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    // todo: `AnchoredPositionSingleTweener`로 바꾸고, 좌표계를 선택할 수 있도록. `RotateSingleTweener` 참고.
    [RequireComponent(typeof(RectTransform))]
    public class AnchoredPositionXTweener : MonoBehaviour
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
        private Tweener _tweener;

        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        private Vector2 OriginAnchoredPosition =>
            _originAnchoredPositionCache
            ?? (_originAnchoredPositionCache = RectTransform.anchoredPosition).Value;

        private void Awake()
        {
            _originAnchoredPositionCache = RectTransform.anchoredPosition;
        }

        public Tweener StartShowTween(bool reverse = false)
        {
            _tweener?.Kill();
            RectTransform.anchoredPosition = OriginAnchoredPosition;
            _tweener = RectTransform
                .DOAnchorPosX(end, duration, snapping)
                .SetDelay(startDelay)
                .SetEase(showEase);

            isFrom = reverse
                ? !isFrom
                : isFrom;

            if (isFrom)
            {
                _tweener = _tweener.From();
            }

            return _tweener;
        }

        public Tweener StartHideTween(bool reverse = false)
        {
            _tweener?.Kill();
            RectTransform.anchoredPosition = OriginAnchoredPosition;
            _tweener = RectTransform
                .DOAnchorPosX(end, duration, snapping)
                .SetEase(closeEase);

            isFrom = reverse
                ? !isFrom
                : isFrom;

            if (!isFrom)
            {
                _tweener = _tweener.From();
            }

            return _tweener;
        }
    }
}
