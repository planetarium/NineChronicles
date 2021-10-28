using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    public class DOTweenRectTransformMoveTo : DOTweenBase
    {
        public Vector3 beginValue = new Vector3();
        public Vector3 endValue = new Vector3();
        private RectTransform _transform;

        protected override void Awake()
        {
            base.Awake();
            _transform = GetComponent<RectTransform>();
        }

        public void SetBeginRect(RectTransform rect)
        {
            var beginPos = rect.GetWorldPositionOfCenter();
            beginValue = beginPos;
            endValue = transform.position;
            transform.position = beginPos;
        }

        public override void PlayForward()
        {
            _transform.DOMove(beginValue, 0.0f);
            if (TweenType.Repeat == tweenType)
            {
                _transform.DOMove(endValue, duration)
                    .SetEase(ease)
                    .onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == tweenType)
            {
                _transform.DOMove(endValue, duration)
                    .SetEase(ease)
                    .onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                _transform.DOMove(endValue, duration)
                    .SetEase(ease)
                    .onComplete = PlayReverse;
            }
            else
            {
                _transform.DOMove(endValue, duration)
                    .SetEase(ease)
                    .onComplete = OnComplete;
            }
        }

        public override void PlayReverse()
        {
            _transform.DOMove(endValue, 0.0f);
            if (TweenType.PingPongOnce == tweenType)
            {
                _transform.DOMove(beginValue, duration)
                    .SetEase(ease)
                    .onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                _transform.DOMove(beginValue, duration)
                    .SetEase(ease)
                    .onComplete = PlayForward;
            }
            else
            {
                _transform.DOMove(beginValue, duration)
                    .SetEase(ease)
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
