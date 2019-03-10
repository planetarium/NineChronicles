using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI.Tween
{
    public enum TweenType : int
    {
        Forward,
        Reverse,
        Yoyo,
        Refeat,
        PingPong,
    }

    public class DOTweenImageAlpha : MonoBehaviour
    {
        public float Duration = 0.0f;
        public TweenType TweenType = TweenType.Forward;
        public float BeginValue = 0.0f;
        public float EndValue = 1.0f;

        private Image _image;

        public void Start()
        {
            _image = GetComponent<Image>();
            Invoke($"Play{TweenType.ToString()}", 0.0f);
        }

        private void PlayForward()
        {
            _image.DOFade(BeginValue, 0.0f);
            if (TweenType.Yoyo == TweenType)
            {
                _image.DOFade(EndValue, Duration).onComplete = PlayReverse;
            }
            else if (TweenType.Refeat == TweenType)
            {
                _image.DOFade(EndValue, Duration).onComplete = PlayForward;
            }
            else if (TweenType.PingPong == TweenType)
            {
                _image.DOFade(EndValue, Duration).onComplete = PlayReverse;
            }
            else
            {
                _image.DOFade(EndValue, Duration);
            }
        }
        
        private void PlayReverse()
        {
            _image.DOFade(EndValue, 0.0f);
            if (TweenType.PingPong == TweenType)
            {
                _image.DOFade(BeginValue, Duration).onComplete = PlayForward;
            }
            else
            {
                _image.DOFade(BeginValue, Duration);
            }
        }

        private void PlayYoyo()
        {
            PlayForward();
        }

        private void PlayRepeat()
        {
            PlayForward();
        }

        private void PlayPingPong()
        {
            PlayForward();
        }
    }
}
