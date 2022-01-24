using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Tween
{
    using DG.Tweening;
    public class DOTweenBase : MonoBehaviour
    {
        public enum TweenType : int
        {
            Forward,
            Reverse,
            Repeat,
            PingPongOnce,
            PingPongRepeat,
        }

        public bool playAtStart = true;
        public float startDelay = 0.0f;
        public float duration = 1.0f;
        public TweenType tweenType = TweenType.Forward;
        public Tween currentTween;
        public readonly Subject<Tween> onStopSubject = new Subject<Tween>();
        public System.Action onCompleted = null;

        public bool IsPlaying => currentTween != null && currentTween.IsActive() && currentTween.IsPlaying();

        [HideInInspector]
        public Ease ease = Ease.Linear;

        [HideInInspector]
        public string completeMethod = "";

        [HideInInspector]
        public int componentIndex = 0;

        [HideInInspector]
        public GameObject target = null;

        [HideInInspector]
        public float completeDelay = 0.0f;

        [HideInInspector]
        public bool useCustomEaseCurve = false;

        [HideInInspector]
        public AnimationCurve customEaseCurve = AnimationCurve.Linear(0,0,0,0);

        protected AnimationCurve _reverseEaseCurve = new AnimationCurve();

        protected virtual void Awake()
        {
            onStopSubject.Subscribe(_ => OnStopTweening()).AddTo(gameObject);
            if (useCustomEaseCurve)
            {
                _reverseEaseCurve = new AnimationCurve();
                customEaseCurve.keys
                    .Select(key => new Keyframe(key.value, key.time))
                    .ToList()
                    .ForEach(key => _reverseEaseCurve.AddKey(key));
            }
        }

        protected IEnumerator Start()
        {
            if (!playAtStart)
            {
                yield break;
            }

            yield return new WaitForSeconds(startDelay);
            Play();
        }

        public virtual void Play()
        {
            Invoke($"Play{tweenType.ToString()}", 0.0f);
        }

        public virtual void PlayDelayed(float delay)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(CoPlayDelayed(delay));
            }
        }

        public virtual void Stop()
        {
            if (currentTween == null)
            {
                return;
            }

            currentTween.Kill();
            currentTween = null;
            onStopSubject.OnNext(currentTween);
        }

        public virtual Tween PlayForward()
        {
            return null;
        }

        public virtual Tween PlayReverse()
        {
            return null;
        }

        public virtual Tween PlayRepeat()
        {
            return PlayForward();
        }

        public virtual Tween PlayPingPongOnce()
        {
            return PlayForward();
        }


        public virtual Tween PlayPingPongRepeat()
        {
            return PlayForward();
        }

        public virtual void OnStopTweening()
        {
        }

        public void OnComplete()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            onCompleted?.Invoke();

            if (!string.IsNullOrEmpty(completeMethod) && target)
            {
                StartCoroutine(CoOnComplete());
            }
        }

        private IEnumerator CoOnComplete()
        {
            yield return new WaitForSeconds(completeDelay);
            var components = target.GetComponents<Component>();
            var methodInfo = components[componentIndex].GetType().GetMethod(completeMethod);
            methodInfo.Invoke(components[componentIndex], new object[]{});
        }

        protected virtual IEnumerator CoPlayDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            Play();
        }

        protected virtual Tween SetEase(bool isReverse = false)
        {
            return useCustomEaseCurve
                ? currentTween.SetEase(isReverse ? _reverseEaseCurve : customEaseCurve)
                : currentTween.SetEase(isReverse ? ReverseEasingFunction(ease) : ease);
        }

        /// <summary>
        /// This applies only to the In or Out easing function.
        /// In XXX -> Parse int -> even number
        /// Out XXX -> Parse int -> odd number
        /// </summary>
        /// <param name="ease"></param>
        /// <returns></returns>
        public static Ease ReverseEasingFunction(Ease ease)
        {
            var easingString = ease.ToString();
            if (easingString.Contains("In") ^ easingString.Contains("Out"))
            {
                return (int) ease % 2 == 0 ? ease + 1 : ease - 1;
            }

            return ease;
        }
    }
}
