using UnityEngine;
using DG.Tweening;
using TMPro;
using UniRx;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DOTweenTextAlpha : DOTweenBase
    {
        public float BeginValue = 0.0f;
        public float EndValue = 1.0f;
        public bool ClearOnStop;
        private TextMeshProUGUI _text;

        protected override void Awake()
        {
            base.Awake();
            _text = GetComponent<TextMeshProUGUI>();
        }

        public override void PlayForward()
        {
            _text.DOFade(BeginValue, 0.0f);
            if (TweenType.Repeat == tweenType)
            {
                currentTween = _text.DOFade(EndValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == tweenType)
            {
                currentTween = _text.DOFade(EndValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = _text.DOFade(EndValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayReverse;
            }
            else
            {
                currentTween = _text.DOFade(EndValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = OnComplete;
            }
        }

        public override void PlayReverse()
        {
            _text.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongOnce == tweenType)
            {
                currentTween = _text.DOFade(BeginValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == tweenType)
            {
                currentTween = _text.DOFade(BeginValue, duration)
                    .SetEase(ease);
                currentTween.onComplete = PlayForward;
            }
            else
            {
                currentTween = _text.DOFade(BeginValue, duration)
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

        public override void OnStopTweening()
        {
            base.OnStopTweening();
            if (ClearOnStop)
                ClearAlpha();
        }

        private void ClearAlpha()
        {
            _text.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}
