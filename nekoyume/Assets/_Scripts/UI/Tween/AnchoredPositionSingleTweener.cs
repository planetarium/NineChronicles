using System;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    // todo: `AnchoredPositionSingleTweener`로 바꾸고, 좌표계를 선택할 수 있도록. `RotateSingleTweener` 참고.
    [RequireComponent(typeof(RectTransform))]
    public class AnchoredPositionSingleTweener : DOTweenBase
    {
        public enum AxisType
        {
            X,
            Y,
            Z
        }

        [SerializeField]
        private float endValue = 0f;

        [SerializeField]
        private bool snapping = false;

        [SerializeField]
        private bool isFrom = false;

        public Single single = Single.Z;

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

            switch (single)
            {
                case Single.X:
                    _startPosition.x = isFrom ? endValue : _startPosition.x;
                    _endPosition.x = isFrom ? OriginAnchoredPosition.x : endValue;
                    break;
                case Single.Y:
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

            if (isFrom)
            {
                RectTransform.anchoredPosition = OriginAnchoredPosition;
                currentTween = RectTransform
                    .DOAnchorPosX(endValue, duration, snapping);
                SetEase(true);
            }
            else
            {
                RectTransform.anchoredPosition = new Vector2(endValue, OriginAnchoredPosition.y);
                currentTween = RectTransform
                    .DOAnchorPosX(OriginAnchoredPosition.x, duration, snapping);
                SetEase(true);
            }

            return currentTween.Play();
        }

        public void ResetToOrigin()
        {
            Stop();
            RectTransform.anchoredPosition = OriginAnchoredPosition;
        }
    }
}
