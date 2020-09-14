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
            {
                currentTween = _rectTransform.DOSizeDelta(BeginValue, 0.0f);
            }
        }

        public override void PlayForward()
        {
            currentTween = _rectTransform.DOSizeDelta(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                currentTween = _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(PlayForward);
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                currentTween = _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(PlayReverse);
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                currentTween = _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(PlayReverse);
            }
            else
            {
                currentTween = _rectTransform.DOSizeDelta(EndValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(OnComplete);
            }
        }

        public override void PlayReverse()
        {
            currentTween = _rectTransform.DOSizeDelta(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                currentTween = _rectTransform.DOSizeDelta(BeginValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(OnComplete);
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                currentTween = _rectTransform.DOSizeDelta(BeginValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(PlayForward);
            }
            else
            {
                currentTween = _rectTransform.DOSizeDelta(BeginValue, Duration)
                    .SetEase(Ease_)
                    .OnComplete(OnComplete);
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

        public override void Stop()
        {
            base.Stop();


        }
    }
}
