using UnityEngine;
using DG.Tweening;
using System.Collections;
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

        public bool StartWithPlay = true;
        public float StartDelay = 0.0f;
        public float Duration = 1.0f;
        public TweenType TweenType_ = TweenType.Forward;
        public Ease Ease_ = Ease.Linear;
        [HideInInspector]
        public string CompleteMethod = "";
        [HideInInspector]
        public int ComponentIndex = 0;
        [HideInInspector]
        public GameObject Target = null;
        [HideInInspector]
        public float CompleteDelay = 0.0f;
        public DG.Tweening.Tween currentTween;
        public readonly Subject<DG.Tweening.Tween> onStopSubject = new Subject<DG.Tweening.Tween>();
        public System.Action onCompleted = null;

        protected virtual void Awake()
        {
            onStopSubject.Subscribe(_ => OnStopTweening()).AddTo(gameObject);
        }

        protected IEnumerator Start()
        {
            if (!StartWithPlay)
            {
                yield break;
            }

            yield return new WaitForSeconds(StartDelay);
            Play();
        }

        public virtual void Play()
        {
            Invoke($"Play{TweenType_.ToString()}", 0.0f);
        }

        public virtual void PlayDelayed(float delay)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(CPlayDelayed(delay));
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

        public virtual void PlayForward()
        {
        }

        public virtual void PlayReverse()
        {
        }

        public virtual void PlayRepeat()
        {
            PlayForward();
        }

        public virtual void PlayPingPongOnce()
        {
            PlayForward();
        }


        public virtual void PlayPingPongRepeat()
        {
            PlayForward();
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

            if (!string.IsNullOrEmpty(CompleteMethod) && Target)
            {
                StartCoroutine(CoOnComplete());
            }
        }

        private IEnumerator CoOnComplete()
        {
            yield return new WaitForSeconds(CompleteDelay);
            var components = Target.GetComponents<Component>();
            var methodInfo = components[ComponentIndex].GetType().GetMethod(CompleteMethod);
            methodInfo.Invoke(components[ComponentIndex], new object[]{});
        }

        protected virtual IEnumerator CPlayDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            Play();
        }
    }
}
