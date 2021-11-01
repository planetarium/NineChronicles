using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(Graphic))]
    public class GraphicColorTweener : DOTweenBase
    {
        [SerializeField]
        private Color beginValue = Color.white;

        [SerializeField]
        private Color endValue = Color.black;

        private Graphic _graphic;

        protected override void Awake()
        {
            base.Awake();
            _graphic = GetComponent<Graphic>();
            if (startWithPlay)
            {
                _graphic.DOColor(beginValue, 0.0f);
            }
        }

        public override void PlayForward()
        {
            _graphic.DOColor(beginValue, 0.0f);
            currentTween = _graphic.DOColor(endValue, duration);
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
            _graphic.DOColor(endValue, 0.0f);
            currentTween = _graphic.DOColor(beginValue, duration);
            if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = SetEase(true).OnComplete(PlayForward);
            }
            else
            {
                currentTween = SetEase(true).OnComplete(OnComplete);
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

        private void OnDisable()
        {
            Stop();
        }
    }
}
