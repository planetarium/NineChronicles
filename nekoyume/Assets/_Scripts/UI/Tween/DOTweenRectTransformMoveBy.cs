using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    public class DOTweenRectTransformMoveBy : DOTweenBase
    {
        public bool startFromDelta = false;
        public Vector3 deltaValue = new Vector3();

        private Vector3 _beginValue = new Vector3();
        private Vector3 _endValue = new Vector3();
        private RectTransform _transform;

        protected override void Awake()
        {
            base.Awake();
            _transform = GetComponent<RectTransform>();
            _beginValue = startFromDelta ? _transform.localPosition - deltaValue : _transform.localPosition;
            _endValue = startFromDelta ? _transform.localPosition : _transform.localPosition + deltaValue;
            if (playAtStart)
            {
                _transform.DOLocalMove(_beginValue, 0.0f);
            }
        }

        public override DG.Tweening.Tween PlayForward()
        {
            _transform.DOLocalMove(_beginValue, 0.0f);
            currentTween = _transform.DOLocalMove(_endValue, duration);
            if (TweenType.Repeat == tweenType)
            {
                currentTween = SetEase().OnComplete(() => PlayForward());
            }
            else if (TweenType.PingPongOnce == tweenType || TweenType.PingPongRepeat == tweenType)
            {
                currentTween = SetEase().OnComplete(() => PlayReverse());
            }
            else
            {
                currentTween = SetEase().OnComplete(OnComplete);
            }

            return currentTween;
        }

        public override DG.Tweening.Tween PlayReverse()
        {
            _transform.DOLocalMove(_endValue, 0.0f);
            currentTween = _transform.DOLocalMove(_beginValue, duration);
            if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = SetEase(true).OnComplete(() => PlayForward());
            }
            else
            {
                currentTween = SetEase(true).OnComplete(OnComplete);
            }

            return currentTween;
        }

        public override DG.Tweening.Tween PlayRepeat()
        {
            return PlayForward();
        }

        public override DG.Tweening.Tween PlayPingPongOnce()
        {
            return PlayForward();
        }


        public override DG.Tweening.Tween PlayPingPongRepeat()
        {
            return PlayForward();
        }
    }
}
