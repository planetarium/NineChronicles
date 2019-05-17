using System.Linq;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class MecanimCharacterAnimator : CharacterAnimator<Animator>
    {
        private static readonly Vector3 Vector3Zero = Vector3.zero;
        
        private int _baseLayerIndex;
        
        public MecanimCharacterAnimator(CharacterBase root) : base (root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);

            Animator.speed = TimeScale;
            
            _baseLayerIndex = Animator.GetLayerIndex("Base Layer");
        }

        public override bool AnimatorValidation()
        {
            // Reference.
            // if (ReferenceEquals(_anim, null)) 이 라인일 때와 if (_anim == null) 이 라인일 때의 결과가 달라서 주석을 남겨뒀어요.
            // 아마 전자는 포인터가 가리키는 실제 값을 검사하는 것이고, 후자는 _anim의 값을 검사하는 것 같아요.
            // return !ReferenceEquals(animator, null);
            return Animator != null;
        }

        public override Vector3 GetHUDPosition()
        {
            return Vector3Zero;
        }

        public override void Appear()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play(CharacterAnimation.Appear);
        }

        public override void Idle()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play(CharacterAnimation.Idle);
            Animator.SetBool(CharacterAnimation.Run, false);
        }

        public override void Run()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play(CharacterAnimation.Run);
            Animator.SetBool(CharacterAnimation.Run, true);
        }

        public override void StopRun()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.SetBool(CharacterAnimation.Run, false);
        }

        public override void Attack()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            Animator.Play(CharacterAnimation.Attack, _baseLayerIndex, 0f);
        }

        public override void Casting()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            Animator.Play(CharacterAnimation.Casting, _baseLayerIndex, 0f);
        }

        public override void Hit()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            Animator.Play(CharacterAnimation.Hit, _baseLayerIndex, 0f);
        }

        public override void Die()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play(CharacterAnimation.Die);
        }

        public override void Disappear()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play(CharacterAnimation.Disappear);
        }
    }
}
