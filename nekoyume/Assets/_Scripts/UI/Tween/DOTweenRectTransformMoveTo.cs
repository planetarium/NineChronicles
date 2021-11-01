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
            currentTween = _transform.DOMove(endValue, duration);
            if (TweenType.Repeat == tweenType)
            {
                SetEase().OnComplete(PlayForward);
            }
            else if (TweenType.PingPongOnce == tweenType || TweenType.PingPongRepeat == tweenType)
            {
                SetEase().OnComplete(PlayReverse);
            }
        }

        public override void PlayReverse()
        {
            _transform.DOMove(endValue, 0.0f);
            currentTween = _transform.DOMove(beginValue, duration);
            if (TweenType.PingPongRepeat == tweenType)
            {
                SetEase(true).OnComplete(PlayForward);
            }
            else
            {
                SetEase(true).OnComplete(OnComplete);
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
