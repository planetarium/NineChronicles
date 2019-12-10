using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    public class DOTweenRectTransformSize : DOTweenBase
    {
        public float multiplier = 1.2f;

        private Vector2 BeginValue;
        private Vector2 EndValue;
        private RectTransform _rectTransform;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            BeginValue = _rectTransform.sizeDelta;
            EndValue = BeginValue * multiplier;
            if (StartWithPlay)
                _rectTransform.DOSizeDelta(BeginValue, 0.0f);
        }

        public override void PlayForward()
        {
            _rectTransform.DOSizeDelta(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else
            {
                _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
        }

        public override void PlayReverse()
        {
            _rectTransform.DOSizeDelta(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                _rectTransform.DOSizeDelta(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _rectTransform.DOSizeDelta(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else
            {
                _rectTransform.DOSizeDelta(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
        }

        public override void PlayRepeat()
        {
            PlayForward();
        }

        public override void PlayPingPongOnce()
        {
            PlayForward();
        }


        public override void PlayPingPongRepeat()
        {
            PlayForward();
        }
    }
}
