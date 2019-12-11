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
        [SerializeField] private float to = 0f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool snapping = false;

        [SerializeField] private Ease ease = Ease.Linear;
        [SerializeField] private bool from = false;

        private RectTransform _rectTransform;
        private TweenerCore<Vector2, Vector2, VectorOptions> _tween;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (from)
            {
                _tween = _rectTransform.DOAnchorPosX(to, duration, snapping)
                    .SetEase(ease)
                    .From();
            }
            else
            {
                _tween = _rectTransform.DOAnchorPosX(to, duration, snapping)
                    .SetEase(ease);
            }
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;
        }
    }
}
