using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Tween
{
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

        public bool startWithPlay = true;
        public float startDelay = 0.0f;
        public float duration = 1.0f;
        public TweenType tweenType = TweenType.Forward;
        public DG.Tweening.Tween currentTween;
        public readonly Subject<DG.Tweening.Tween> onStopSubject = new Subject<DG.Tweening.Tween>();
        public System.Action onCompleted = null;

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
            if (!startWithPlay)
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
            if (currentTween is null)
            {
                return;
            }

            currentTween.Kill();
            currentTween = null;
            onStopSubject.OnNext(currentTween);
        }

        public virtual DG.Tweening.Tween PlayForward()
        {
            return null;
        }

        public virtual DG.Tweening.Tween PlayReverse()
        {
            return null;
        }

        public virtual DG.Tweening.Tween PlayRepeat()
        {
            return PlayForward();
        }

        public virtual DG.Tweening.Tween PlayPingPongOnce()
        {
            return PlayForward();
        }


        public virtual DG.Tweening.Tween PlayPingPongRepeat()
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

        protected virtual DG.Tweening.Tween SetEase(bool isReverse = false)
        {
            return useCustomEaseCurve
                ? currentTween.SetEase(isReverse ? _reverseEaseCurve : customEaseCurve)
                : currentTween.SetEase(isReverse ? ReverseEasingFunction(ease) : ease);
            // ease == Ease.OutBack 27
            // ease == Ease.InBack 26
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
            return (int) ease % 2 == 0 ? ease + 1 : ease - 1;
        }
    }
}
