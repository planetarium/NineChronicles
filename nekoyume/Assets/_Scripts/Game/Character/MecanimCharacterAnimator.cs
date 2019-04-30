using System.Linq;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class MecanimCharacterAnimator : CharacterAnimator<Animator>
    {
        private int _baseLayerIndex;
        
        public MecanimCharacterAnimator(CharacterBase root) : base (root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);
            
            _baseLayerIndex = animator.GetLayerIndex("Base Layer");
        }

        public override bool AnimatorValidation()
        {
            // Reference.
            // if (ReferenceEquals(_anim, null)) 이 라인일 때와 if (_anim == null) 이 라인일 때의 결과가 달라서 주석을 남겨뒀어요.
            // 아마 전자는 포인터가 가리키는 실제 값을 검사하는 것이고, 후자는 _anim의 값을 검사하는 것 같아요.
            // return !ReferenceEquals(animator, null);
            return animator != null;
        }

        public override Vector3 GetHUDPosition()
        {
            return Vector3.zero;
        }

        public override void SetTimeScale(float value)
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.speed = value;
        }

        public override void Appear()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.Play("Appear");
        }

        public override void Idle()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.Play("Idle");
            animator.SetBool("Run", false);
        }

        public override void Run()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.Play("Run");
            animator.SetBool("Run", true);
        }

        public override void StopRun()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.SetBool("Run", false);
        }

        public override void Attack()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            animator.Play("Attack", _baseLayerIndex, 0f);
        }

        public override void Hit()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            animator.Play("Hit", _baseLayerIndex, 0f);
        }

        public override void Die()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.Play("Die");
        }

        public override void Disappear()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.Play("Disappear");
        }
    }
}
