using UnityEngine;
using DG.Tweening;
using System.Collections;

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
                currentTween = _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                currentTween = _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                currentTween = _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayReverse;
            }
            else
            {
                currentTween = _group.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = OnComplete;
            }
        }
        
        public override void PlayReverse()
        {
            _group.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                currentTween = _group.DOFade(BeginValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                currentTween = _group.DOFade(BeginValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayForward;
            }
            else
            {
                currentTween = _group.DOFade(BeginValue, Duration)
                    .SetEase(Ease_);
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

        protected override IEnumerator CPlayDelayed(float delay)
        {
            _group.DOFade(BeginValue, 0.0f);
            yield return new WaitForSeconds(delay);
            Play();
        }
    }
}
