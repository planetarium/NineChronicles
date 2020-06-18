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

        private RectTransform _rectTransform;
        private TweenerCore<Vector2, Vector2, VectorOptions> _tween;

        private Vector2 _originAnchoredPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originAnchoredPosition = _rectTransform.anchoredPosition;
        }

        public Tweener StartShowTween(bool reverse = false)
        {
            _tween?.Kill();
            _rectTransform.anchoredPosition = _originAnchoredPosition;
            _tween = _rectTransform
                .DOAnchorPosX(end, duration, snapping)
                .SetDelay(startDelay)
                .SetEase(showEase);

            isFrom = reverse
                ? !isFrom
                : isFrom;

            if (isFrom)
            {
                _tween = _tween.From();
            }

            return _tween;
        }

        public Tweener StartHideTween(bool reverse = false)
        {
            _tween?.Kill();
            _rectTransform.anchoredPosition = _originAnchoredPosition;
            _tween = _rectTransform
                .DOAnchorPosX(end, duration, snapping)
                .SetEase(closeEase);

            isFrom = reverse
                ? !isFrom
                : isFrom;

            if (!isFrom)
            {
                _tween = _tween.From();
            }

            return _tween;
        }
    }
}
