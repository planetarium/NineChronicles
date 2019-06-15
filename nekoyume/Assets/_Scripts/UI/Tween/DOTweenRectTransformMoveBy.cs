using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    public class DOTweenRectTransformMoveBy : DOTweenBase
    {
        public bool StartFromDelta = false;
        public Vector3 DeltaValue = new Vector3();
        private Vector3 BeginValue = new Vector3();
        private Vector3 EndValue = new Vector3();
        private RectTransform _transform;

        private void Awake()
        {
            _transform = GetComponent<RectTransform>();
            BeginValue = StartFromDelta ? _transform.localPosition - DeltaValue : _transform.localPosition;
            EndValue = StartFromDelta ? _transform.localPosition : _transform.localPosition + DeltaValue;
            if (StartWithPlay)
                _transform.DOLocalMove(BeginValue, 0.0f);
        }

        public override void PlayForward()
        {
            _transform.DOLocalMove(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                _transform.DOLocalMove(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                _transform.DOLocalMove(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _transform.DOLocalMove(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else
            {
                _transform.DOLocalMove(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
        }
        
        public override void PlayReverse()
        {
            _transform.DOLocalMove(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                _transform.DOLocalMove(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _transform.DOLocalMove(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else
            {
                _transform.DOLocalMove(BeginValue, Duration)
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
