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
            // ReferenceEquals(left, null) 함수는 left 변수의 메모리에 담긴 포인터가이 null인지 검사하고,
            // `left == null` 식은 left 변수의 메모리에 담긴 포인터가 가리키는 메모리의 값이 null인지 검사합니다.
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
            
            Animator.Play("Appear");
        }

        public override void Idle()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play("Idle");
            Animator.SetBool("Run", false);
        }

        public override void Run()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play("Run");
            Animator.SetBool("Run", true);
        }

        public override void StopRun()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.SetBool("Run", false);
        }

        public override void Attack()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            Animator.Play("Attack", _baseLayerIndex, 0f);
        }

        public override void Hit()
        {
            if (!AnimatorValidation())
            {
                return;
            }

            Animator.Play("Hit", _baseLayerIndex, 0f);
        }

        public override void Die()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play("Die");
        }

        public override void Disappear()
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            Animator.Play("Disappear");
        }
    }
}
