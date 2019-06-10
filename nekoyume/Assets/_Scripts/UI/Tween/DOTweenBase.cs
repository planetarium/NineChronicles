using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

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

        protected virtual void Start()
        {
            if (StartWithPlay)
                Invoke($"Play{TweenType_.ToString()}", 0.0f);
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

        public void OnComplete()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (!string.IsNullOrEmpty(CompleteMethod) && Target)
            {
                StartCoroutine(CoOnComplete());
            }
        }

        public IEnumerator CoOnComplete()
        {
            yield return new WaitForSeconds(CompleteDelay);
            var components = Target.GetComponents<Component>();
            var methodInfo = components[ComponentIndex].GetType().GetMethod(CompleteMethod);
            methodInfo.Invoke(components[ComponentIndex], new object[]{});
        }
    }
}
