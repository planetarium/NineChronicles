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
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private float end = 0f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool snapping = false;

        [SerializeField] private Ease showEase = Ease.Linear;
        [SerializeField] private Ease closeEase = Ease.Linear;

        [SerializeField] private bool isFrom = false;

        private RectTransform _rectTransform;
        private TweenerCore<Vector2, Vector2, VectorOptions> _tween;

        private Vector2 _originAnchoredPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originAnchoredPosition = _rectTransform.anchoredPosition;
        }

        public Tweener StartTween()
        {
            RefreshTween();

            _rectTransform.anchoredPosition = _originAnchoredPosition;
            if (isFrom)
            {
                _tween = _rectTransform
                    .DOAnchorPosX(end, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase)
                    .From();
            }
            else
            {
                _tween = _rectTransform
                    .DOAnchorPosX(end, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase);
            }

            return _tween;
        }

        public Tweener PlayReverse()
        {
            RefreshTween();

            _rectTransform.anchoredPosition = new Vector2(
                _originAnchoredPosition.x + end,
                _originAnchoredPosition.y);

            if (isFrom)
            {
                _tween = _rectTransform
                    .DOAnchorPosX(_originAnchoredPosition.x, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase)
                    .From();
            }
            else
            {
                _tween = _rectTransform.
                    DOAnchorPosX(_originAnchoredPosition.x, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase);
            }

            return _tween;
        }

        public Tweener StopTween()
        {
            RefreshTween();
            _tween = isFrom
                ? _rectTransform.DOAnchorPosX(end, duration, snapping).SetEase(closeEase)
                : _rectTransform.DOAnchorPosX(end, duration, snapping).SetEase(closeEase).From();
            return _tween;
        }

        private void RefreshTween()
        {
            _tween?.Kill();
            _tween = null;
        }
    }
}
