using System;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterAnimator<T> : ICharacterAnimator where T : Component
    {
        public CharacterBase Root { get; }
        public GameObject Target { get; private set; }
        public Subject<string> OnEvent { get; }
        public float TimeScale { get; set; }

        protected T Animator { get; private set; }

        protected CharacterAnimator(CharacterBase root)
        {
            Root = root;
            OnEvent = new Subject<string>();
        }

        public virtual void ResetTarget(GameObject value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException();
            }
            
            Target = value;
            Animator = value.GetComponentInChildren<T>();

            if (ReferenceEquals(Animator, null))
            {
                throw new NotFoundComponentException<T>();
            }
        }

        public void DestroyTarget()
        {
            if (ReferenceEquals(Target, null))
            {
                throw new ArgumentNullException();
            }

            //애니메이터 초기화시 타이밍이 꼬이는 문제를 회피하기 위해 즉시 파괴시킵니다.
            Object.DestroyImmediate(Target);
            Target = null;
        }

        public abstract bool AnimatorValidation();
        public abstract Vector3 GetHUDPosition();

        #region Animation

        public abstract void Appear();
        public abstract void Idle();
        public abstract void Run();
        public abstract void StopRun();
        public abstract void Attack();
        public abstract void Cast();
        public abstract void Hit();
        public abstract void Die();
        public abstract void Disappear();

        #endregion

        public void Dispose()
        {
            OnEvent?.Dispose();
        }
    }
}
