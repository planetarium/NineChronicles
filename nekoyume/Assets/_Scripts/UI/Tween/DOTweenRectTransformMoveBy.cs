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

        protected override void Awake()
        {
            base.Awake();
            _transform = GetComponent<RectTransform>();
            BeginValue = StartFromDelta ? _transform.localPosition - DeltaValue : _transform.localPosition;
            EndValue = StartFromDelta ? _transform.localPosition : _transform.localPosition + DeltaValue;
            if (startWithPlay)
            {
                _transform.DOLocalMove(BeginValue, 0.0f);
            }
        }

        public override void PlayForward()
        {
            _transform.DOLocalMove(BeginValue, 0.0f);
            currentTween = _transform.DOLocalMove(EndValue, duration);
            if (TweenType.Repeat == tweenType)
            {
                currentTween = SetEase().OnComplete(PlayForward);
            }
            else if (TweenType.PingPongOnce == tweenType || TweenType.PingPongRepeat == tweenType)
            {
                currentTween = SetEase().OnComplete(PlayReverse);
            }
            else
            {
                currentTween = SetEase().OnComplete(OnComplete);
            }
        }

        public override void PlayReverse()
        {
            _transform.DOLocalMove(EndValue, 0.0f);
            currentTween = _transform.DOLocalMove(BeginValue, duration);
            if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = SetEase().OnComplete(PlayForward);
            }
            else
            {
                currentTween = SetEase().OnComplete(OnComplete);
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
