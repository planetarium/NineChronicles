using DG.Tweening;
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

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (from)
            {
                _rectTransform.DOAnchorPosX(to, duration, snapping)
                    .SetEase(ease)
                    .From();
            }
            else
            {
                _rectTransform.DOAnchorPosX(to, duration, snapping)
                    .SetEase(ease);
            }
        }
    }
}
