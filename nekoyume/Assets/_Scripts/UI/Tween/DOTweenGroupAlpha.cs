using UnityEngine;
using System.Collections;

namespace Nekoyume.UI.Tween
{
    using DG.Tweening;
    [RequireComponent(typeof(CanvasGroup))]
    public class DOTweenGroupAlpha : DOTweenBase
    {
        public float beginValue = 0.0f;
        public float endValue = 1.0f;

        [SerializeField]
        private bool infiniteLoop = false;

        private CanvasGroup _group;

        protected override void Awake()
        {
            base.Awake();
            _group = GetComponent<CanvasGroup>();
            if (playAtStart)
            {
                ResetToBeginningValue();
            }
        }

        public override Tween PlayForward()
        {
            ResetToBeginningValue();
            currentTween = _group.DOFade(endValue, duration);
            if (infiniteLoop)
            {
                currentTween.SetLoops(-1, LoopType.Incremental);
            }

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

        public override Tween PlayReverse()
        {
            ResetToEndingValue();
            currentTween = _group.DOFade(beginValue, duration);
            if (TweenType.PingPongOnce == tweenType)
            {
                currentTween = SetEase().OnComplete(OnComplete);
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = SetEase(true).OnComplete(() => PlayForward());
            }
            else
            {
                currentTween = SetEase(true).OnComplete(OnComplete);
            }

            return currentTween;
        }

        public override Tween PlayRepeat()
        {
            return PlayForward();
        }

        public override Tween PlayPingPongOnce()
        {
            return PlayForward();
        }

        public override Tween PlayPingPongRepeat()
        {
            return PlayForward();
        }

        protected override IEnumerator CoPlayDelayed(float delay)
        {
            ResetToBeginningValue();
            yield return new WaitForSeconds(delay);
            Play();
        }

        private void OnEnable()
        {
            if (playAtStart)
            {
                Play();
            }
        }

        public void ResetToBeginningValue()
        {
            _group.alpha = beginValue;
        }

        public void ResetToEndingValue()
        {
            _group.alpha = endValue;
        }
    }
}
