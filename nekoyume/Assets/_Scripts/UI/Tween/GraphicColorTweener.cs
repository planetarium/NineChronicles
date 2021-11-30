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
            if (playAtStart)
            {
                _graphic.DOColor(beginValue, 0.0f);
            }
        }

        public override DG.Tweening.Tween PlayForward()
        {
            _graphic.DOColor(beginValue, 0.0f);
            currentTween = _graphic.DOColor(endValue, duration);
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
            _graphic.DOColor(endValue, 0.0f);
            currentTween = _graphic.DOColor(beginValue, duration);
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

        private void OnDisable()
        {
            Stop();
        }
    }
}
