using System;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(RectTransform))]
    public class AnchoredPositionSingleTweener : DOTweenBase
    {
        public enum AxisType
        {
            X,
            Y
        }

        [SerializeField]
        private float endValue = 0f;

        [SerializeField]
        private bool snapping = false;

        [SerializeField]
        private bool isFrom = false;

        public AxisType axisType = AxisType.X;

        private RectTransform _rectTransformCache;
        private Vector2? _originAnchoredPositionCache;
        private Vector2 _startPosition;
        private Vector2 _endPosition;

        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        private Vector2 OriginAnchoredPosition =>
            _originAnchoredPositionCache
            ?? (_originAnchoredPositionCache = RectTransform.anchoredPosition).Value;

        protected override void Awake()
        {
            base.Awake();
            Assert.NotNull(RectTransform);
            Assert.AreEqual(OriginAnchoredPosition, _originAnchoredPositionCache);
        }

        private void OnEnable()
        {
            _startPosition = OriginAnchoredPosition;
            _endPosition = OriginAnchoredPosition;

            switch (axisType)
            {
                case AxisType.X:
                    _startPosition.x = isFrom ? endValue : _startPosition.x;
                    _endPosition.x = isFrom ? OriginAnchoredPosition.x : endValue;
                    break;
                case AxisType.Y:
                    _startPosition.y = isFrom ? endValue : _startPosition.y;
                    _endPosition.y = isFrom ? OriginAnchoredPosition.y : _endPosition.y;
                    break;
            }
        }

        public DG.Tweening.Tween PlayTween()
        {
            Stop();

            OnEnable();
            RectTransform.anchoredPosition = _startPosition;
            currentTween = RectTransform
                .DOAnchorPos(_endPosition, duration, snapping)
                .SetDelay(startDelay);
            SetEase();

            return currentTween.Play();
        }

        public override DG.Tweening.Tween PlayReverse()
        {
            Stop();

            RectTransform.anchoredPosition = _endPosition;
            currentTween = RectTransform.DOAnchorPos(_startPosition, duration, snapping);
            SetEase(true);

            return currentTween.Play();
        }

        public void ResetToOrigin()
        {
            Stop();
            RectTransform.anchoredPosition = OriginAnchoredPosition;
        }
    }
}
