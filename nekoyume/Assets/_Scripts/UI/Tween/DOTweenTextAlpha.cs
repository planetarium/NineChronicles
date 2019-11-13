using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DOTweenTextAlpha : DOTweenBase
    {
        public float BeginValue = 0.0f;
        public float EndValue = 1.0f;
        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        public override void PlayForward()
        {
            _text.DOFade(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else
            {
                _text.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
        }
        
        public override void PlayReverse()
        {
            _text.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                _text.DOFade(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _text.DOFade(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else
            {
                _text.DOFade(BeginValue, Duration)
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
