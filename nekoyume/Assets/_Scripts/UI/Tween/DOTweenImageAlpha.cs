using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(Image))]
    public class DOTweenImageAlpha : DOTweenBase
    {
        public float BeginValue = 0.0f;
        public float EndValue = 1.0f;
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (StartWithPlay)
                _image.DOFade(BeginValue, 0.0f);
        }

        public override void PlayForward()
        {
            _image.DOFade(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                _image.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                _image.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _image.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayReverse;
            }
            else
            {
                _image.DOFade(EndValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
        }
        
        public override void PlayReverse()
        {
            _image.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongOnce == TweenType_)
            {
                _image.DOFade(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = OnComplete;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _image.DOFade(BeginValue, Duration)
                    .SetEase(Ease_)
                    .onComplete = PlayForward;
            }
            else
            {
                _image.DOFade(BeginValue, Duration)
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
