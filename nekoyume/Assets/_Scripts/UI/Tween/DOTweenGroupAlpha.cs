using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DOTweenGroupAlpha : DOTweenBase
    {
        public float beginValue = 0.0f;
        public float endValue = 1.0f;
        private CanvasGroup _group;

        protected override void Awake()
        {
            base.Awake();
            _group = GetComponent<CanvasGroup>();
            if (startWithPlay)
            {
                _group.DOFade(beginValue, 0.0f);
            }
        }

        public override void PlayForward()
        {
            _group.DOFade(beginValue, 0.0f);
            currentTween = _group.DOFade(endValue, duration);
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
            _group.DOFade(endValue, 0.0f);
            currentTween = _group.DOFade(beginValue, duration);
            if (TweenType.PingPongOnce == tweenType)
            {
                currentTween = SetEase().OnComplete(OnComplete);
            }
            else if (TweenType.PingPongRepeat == tweenType)
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

        protected override IEnumerator CoPlayDelayed(float delay)
        {
            _group.DOFade(beginValue, 0.0f);
            yield return new WaitForSeconds(delay);
            Play();
        }
    }
}
