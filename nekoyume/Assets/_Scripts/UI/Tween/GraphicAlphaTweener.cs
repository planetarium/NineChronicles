using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(Graphic))]
    public class GraphicAlphaTweener : DOTweenBase
    {
        [SerializeField]
        private float beginValue = 0f;

        [SerializeField]
        private float endValue = 1f;

        private Graphic _graphic;

        protected override void Awake()
        {
            base.Awake();
            _graphic = GetComponent<Graphic>();
            if (startWithPlay)
            {
                _graphic.DOFade(beginValue, 0.0f);
            }
        }

        public override void PlayForward()
        {
            _graphic.DOFade(beginValue, 0.0f);
            currentTween = _graphic.DOFade(endValue, duration);
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
            _graphic.DOFade(endValue, 0.0f);
            currentTween = _graphic.DOFade(beginValue, duration);
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

        private void OnDisable()
        {
            Stop();
        }
    }
}
