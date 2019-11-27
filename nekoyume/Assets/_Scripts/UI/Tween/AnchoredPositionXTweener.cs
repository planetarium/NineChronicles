using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(RectTransform))]
    public class AnchoredPositionXTweener : MonoBehaviour
    {
        [SerializeField] private float to;
        [SerializeField] private float duration;
        [SerializeField] private bool snapping;

        [SerializeField] private Ease ease = Ease.Linear;
        [SerializeField] private bool from;

        private RectTransform _rectTransform;
        private TweenerCore<Vector2, Vector2, VectorOptions> tween;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (from)
            {
                tween = _rectTransform.DOAnchorPosX(to, duration, snapping)
                    .SetEase(ease)
                    .From();
            }
            else
            {
                tween = _rectTransform.DOAnchorPosX(to, duration, snapping)
                    .SetEase(ease);
            }
        }

        private void OnDisable()
        {
            tween?.Complete();
        }
    }
}
