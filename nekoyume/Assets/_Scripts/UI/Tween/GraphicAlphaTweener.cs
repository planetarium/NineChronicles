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

        [SerializeField]
        private bool infiniteLoop = false;

        [SerializeField]
        private LoopType loopType = LoopType.Yoyo;

        private Graphic _graphic;

        protected override void Awake()
        {
            base.Awake();
            _graphic = GetComponent<Graphic>();
        }

        public override void PlayForward()
        {
            _graphic.DOFade(beginValue, 0.0f);
            if (TweenType.Repeat == tweenType)
            {
                currentTween = _graphic.DOFade(endValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == tweenType)
            {
                currentTween = _graphic.DOFade(endValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = _graphic.DOFade(endValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayReverse;
            }
            else
            {
                currentTween = _graphic.DOFade(endValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = OnComplete;
            }
        }

        public override void PlayReverse()
        {
            _graphic.DOFade(endValue, 0.0f);
            if (TweenType.PingPongOnce == tweenType)
            {
                currentTween = _graphic.DOFade(beginValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = _graphic.DOFade(beginValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayForward;
            }
            else
            {
                currentTween = _graphic.DOFade(beginValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = OnComplete;
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

        private void OnEnable()
        {
            if (startWithPlay)
            {
                _graphic.DOFade(beginValue, 0f);
                currentTween = _graphic.DOFade(endValue, duration).SetEase(ease);

                if (infiniteLoop)
                {
                    currentTween = currentTween.SetLoops(-1, loopType);
                }
            }
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
