using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.Game.Tween
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DOTweenSpriteAlpha : MonoBehaviour
    {
        public enum TweenType : int
        {
            Forward,
            Reverse,
            Repeat,
            PingPongOnce,
            PingPongRepeat,
        }

        public float Duration = 0.0f;
        public TweenType TweenType_ = TweenType.Forward;
        public float BeginValue = 0.0f;
        public float EndValue = 1.0f;

        private SpriteRenderer _sprite;

        public void Start()
        {
            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite)
                Invoke($"Play{TweenType_.ToString()}", 0.0f);
        }

        private void PlayForward()
        {
            _sprite.DOFade(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                _sprite.DOFade(EndValue, Duration).onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                _sprite.DOFade(EndValue, Duration).onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                _sprite.DOFade(EndValue, Duration).onComplete = PlayReverse;
            }
            else
            {
                _sprite.DOFade(EndValue, Duration);
            }
        }

        private void PlayReverse()
        {
            _sprite.DOFade(EndValue, 0.0f);
            if (TweenType.PingPongRepeat == TweenType_)
            {
                _sprite.DOFade(BeginValue, Duration).onComplete = PlayForward;
            }
            else
            {
                _sprite.DOFade(BeginValue, Duration);
            }
        }

        private void PlayRepeat()
        {
            PlayForward();
        }

        private void PlayPingPongOnce()
        {
            PlayForward();
        }

        private void PlayPingPongRepeat()
        {
            PlayForward();
        }
    }
}
