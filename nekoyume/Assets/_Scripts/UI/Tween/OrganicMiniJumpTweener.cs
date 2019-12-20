using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(RectTransform))]
    public class OrganicMiniJumpTweener : MonoBehaviour
    {
        [SerializeField] private Vector2 endValue = Vector2.zero;
        [SerializeField] private float jumpPower = 1;
        [SerializeField] private int jumpCount = 1;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool snapping = false;
        [SerializeField] private int loopCount = -1;
        [SerializeField] private float loopDelay;
        
        private RectTransform _rectTransform;
        private Vector2 _fromValue;
        private Sequence _sequence;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _fromValue = _rectTransform.anchoredPosition;
            _sequence = _rectTransform
                .DOJumpAnchorPos(endValue, jumpPower, jumpCount, duration, snapping)
                .SetLoops(loopCount);
        }

        private void Reset()
        {
            endValue = _rectTransform.anchoredPosition;
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            _rectTransform.anchoredPosition = _fromValue;
        }
    }
}