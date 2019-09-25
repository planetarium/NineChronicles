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
            if (!value)
            {
                throw new ArgumentNullException();
            }
            
            Target = value;
            Animator = value.GetComponentInChildren<T>();

            if (Animator is null)
            {
                throw new NotFoundComponentException<T>();
            }
        }

        public void DestroyTarget()
        {
            if (Target is null)
            {
                throw new ArgumentNullException();
            }

            Object.Destroy(Target);
            Target = null;
        }

        public abstract bool ValidateAnimator();
        public abstract Vector3 GetHUDPosition();

        #region Animation

        public abstract void Appear();
        public abstract void Standing();
        public abstract void StandingToIdle();
        public abstract void Idle();
        public abstract void Touch();
        public abstract void Run();
        public abstract void StopRun();
        public abstract void Attack();
        public abstract void Cast();
        public abstract void CastAttack();
        public abstract void CriticalAttack();
        public abstract void Hit();
        public abstract void Win();
        public abstract void Die();
        public abstract void Disappear();

        #endregion

        public void Dispose()
        {
            OnEvent?.Dispose();
        }
    }
}
