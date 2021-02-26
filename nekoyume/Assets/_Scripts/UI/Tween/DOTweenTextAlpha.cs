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
            if (TweenType.Repeat == TweenType_)
            {
                currentTween = _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                currentTween = _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                currentTween = _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayReverse;
            }
            else
            {
                currentTween = _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = OnComplete;
            }
        }
        
        public override void PlayReverse()
        {
            _text.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                currentTween = _text.DOFade(BeginValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                currentTween = _text.DOFade(BeginValue, Duration)
                    .SetEase(Ease_);
                currentTween.onComplete = PlayForward;
            }
            else
            {
                currentTween = _text.DOFade(BeginValue, Duration)
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
