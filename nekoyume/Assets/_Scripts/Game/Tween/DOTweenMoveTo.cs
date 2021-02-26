using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.Game.Tween
{
    public class DOTweenMoveTo : MonoBehaviour
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
        public Vector3 BeginValue = new Vector3();
        public Vector3 EndValue = new Vector3();

        public void Start()
        {
            Invoke($"Play{TweenType_.ToString()}", 0.0f);
        }

        private void PlayForward()
        {
            transform.DOLocalMove(BeginValue, 0.0f);
            if (TweenType.Repeat == TweenType_)
            {
                transform.DOLocalMove(EndValue, Duration).onComplete = PlayForward;
            }
            else if (TweenType.PingPongOnce == TweenType_)
            {
                transform.DOLocalMove(EndValue, Duration).onComplete = PlayReverse;
            }
            else if (TweenType.PingPongRepeat == TweenType_)
            {
                transform.DOLocalMove(EndValue, Duration).onComplete = PlayReverse;
            }
            else
            {
                transform.DOLocalMove(EndValue, Duration);
            }
        }

        private void PlayReverse()
        {
            transform.DOLocalMove(EndValue, 0.0f);
            if (TweenType.PingPongRepeat == TweenType_)
            {
                transform.DOLocalMove(BeginValue, Duration).onComplete = PlayForward;
            }
            else
            {
                transform.DOLocalMove(BeginValue, Duration);
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
