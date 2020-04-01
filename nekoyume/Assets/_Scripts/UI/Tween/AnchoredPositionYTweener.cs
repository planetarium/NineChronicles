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
        public TweenCallback OnComplete = null;
        public TweenCallback OnReverseComplete = null;

        [SerializeField] private float startDelay = 0f;
        [SerializeField] private float end = 0f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool snapping = false;

        [SerializeField] private Ease showEase = Ease.Linear;
        [SerializeField] private Ease closeEase = Ease.Linear;

        [SerializeField] private bool isFrom = false;

        private RectTransform _rectTransform;
        private TweenerCore<Vector2, Vector2, VectorOptions> _tween;

        private Vector2 originAnchoredPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            originAnchoredPosition = _rectTransform.anchoredPosition;
        }

        public Tweener StartTween()
        {
            RefreshTween();

            _rectTransform.anchoredPosition = originAnchoredPosition;
            if (isFrom)
            {
                _tween = _rectTransform.DOAnchorPosY(end, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase)
                    .From();
            }
            else
            {
                _tween = _rectTransform.DOAnchorPosY(end, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase);
            }

            _tween.onComplete = OnComplete;
            return _tween;
        }

        public Tweener PlayReverse()
        {
            RefreshTween();

            _rectTransform.anchoredPosition = new Vector2(
                originAnchoredPosition.x,
                originAnchoredPosition.y + end);

            if (isFrom)
            {
                _tween = _rectTransform.DOAnchorPosY(originAnchoredPosition.y, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase)
                    .From();
            }
            else
            {
                _tween = _rectTransform.DOAnchorPosY(originAnchoredPosition.y, duration, snapping)
                    .SetDelay(startDelay)
                    .SetEase(showEase);
            }

            _tween.onComplete = OnReverseComplete;
            return _tween;
        }

        public Tweener StopTween()
        {
            RefreshTween();
            if(isFrom)
            {
                _tween = _rectTransform.DOAnchorPosY(end, duration, snapping).SetEase(closeEase);
            }
            else
            {
                _tween = _rectTransform.DOAnchorPosY(end, duration, snapping).SetEase(closeEase).From();
            }
            return _tween;
        }

        private void RefreshTween()
        {
            _tween?.Kill();
            _tween = null;
        }
    }
}
