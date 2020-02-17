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
        // todo: add `begin`
        // todo: `to` -> `end`
        [SerializeField] private float begin = 0f;
        [SerializeField] private float end = 0f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool snapping = false;

        [SerializeField] private Ease showEase = Ease.Linear;
        [SerializeField] private Ease closeEase = Ease.Linear;
        // todo: `from` -> `isFrom`
        [SerializeField] private bool isFrom = false;

        private RectTransform _rectTransform;
        private TweenerCore<Vector2, Vector2, VectorOptions> _tween;

        private Vector2 originAnchoredPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            originAnchoredPosition = _rectTransform.anchoredPosition;
        }

        public Tweener Show()
        {
            RefreshTween();

            _rectTransform.anchoredPosition = originAnchoredPosition;
            if (isFrom)
            {
                _tween = _rectTransform.DOAnchorPosX(end, duration, snapping)
                    .SetEase(showEase)
                    .From();
            }
            else
            {
                _tween = _rectTransform.DOAnchorPosX(end, duration, snapping)
                    .SetEase(showEase);
            }

            return _tween;
        }

        public Tweener Close()
        {
            RefreshTween();
            if(isFrom)
            {
                _tween = _rectTransform.DOAnchorPosX(end, duration, snapping).SetEase(closeEase);
            }
            else
            {
                _tween = _rectTransform.DOAnchorPosX(end, duration, snapping).SetEase(closeEase).From();
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
