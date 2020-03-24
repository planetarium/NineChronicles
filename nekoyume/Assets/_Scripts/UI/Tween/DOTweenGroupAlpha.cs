using UnityEngine;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DOTweenGroupAlpha : DOTweenBase
    {
        public float BeginValue = 0.0f;
        public float EndValue = 1.0f;
        private CanvasGroup _group;

        protected override void Awake()
        {
            base.Awake();
            _group = GetComponent<CanvasGroup>();
            if (StartWithPlay)
                _group.DOFade(BeginValue, 0.0f);
        }

        public override void PlayForward()
        {
            _group.DOFade(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else
            {
                _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
        }
        
        public override void PlayReverse()
        {
            _group.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                _group.DOFade(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _group.DOFade(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else
            {
                _group.DOFade(BeginValue, Duration)
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
