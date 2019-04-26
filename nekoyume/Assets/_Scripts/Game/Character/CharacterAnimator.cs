using System;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterAnimator<T> : ICharacterAnimator where T : Component
    {
        private enum State
        {
            None = -1,
            Appear,
            Idle,
            Run,
            Attack,
            Hit,
            Die,
            Disappear
        }

        private State _state = State.None;
        
        public CharacterBase root { get; }
        public GameObject target { get; private set; }
        
        protected T animator { get; private set; }

        protected CharacterAnimator(CharacterBase root)
        {
            this.root = root;
        }

        public virtual void ResetTarget(GameObject value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException();
            }
            
            target = value;
            animator = value.GetComponentInChildren<T>();

            if (ReferenceEquals(animator, null))
            {
                throw new NotFoundComponentException<T>();
            }
        }

        public abstract bool AnimatorValidation();

        public abstract Vector3 GetHUDPosition();
        
        public abstract void SetTimeScale(float value);

        #region Animation

        public abstract void Appear();
        public abstract void Idle();
        public abstract void Run();
        public abstract void Attack();
        public abstract void Hit();
        public abstract void Die();
        public abstract void Disappear();

        #endregion
    }
}
