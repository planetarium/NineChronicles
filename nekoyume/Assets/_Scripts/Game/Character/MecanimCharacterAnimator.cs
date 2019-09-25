using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class MecanimCharacterAnimator : CharacterAnimator<Animator>
    {
        private static readonly Vector3 Vector3Zero = Vector3.zero;

        private int _baseLayerIndex;

        public MecanimCharacterAnimator(CharacterBase root) : base(root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);

            Animator.speed = TimeScale;

            _baseLayerIndex = Animator.GetLayerIndex("Base Layer");
        }

        public override bool ValidateAnimator()
        {
            // Reference.
            // if (ReferenceEquals(_anim, null)) 이 라인일 때와 if (_anim == null) 이 라인일 때의 결과가 달라서 주석을 남겨뒀어요.
            // ReferenceEquals(left, null) 함수는 left 변수의 메모리에 담긴 포인터가 null인지 검사하고,
            // `left == null` 식은 left 변수의 메모리에 담긴 포인터가 가리키는 메모리의 값이 null인지 검사합니다.
            return Animator != null;
        }

        public override Vector3 GetHUDPosition()
        {
            return Vector3Zero;
        }

        public override void Appear()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Appear), _baseLayerIndex, 0f);
        }

        public override void Standing()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Standing), _baseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), true);
        }

        public override void StandingToIdle()
        {
            if (!ValidateAnimator())
                return;

            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
        }

        public override void Idle()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Idle), _baseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
        }

        public override void Touch()
        {
            if (!ValidateAnimator())
                return;

            if (Animator.GetBool(nameof(CharacterAnimation.Type.Touch)))
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Touch), _baseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Touch), true);
        }

        public override void Run()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Run), _baseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), true);
        }

        public override void StopRun()
        {
            if (!ValidateAnimator())
                return;

            Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
        }

        public override void Attack()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Attack), _baseLayerIndex, 0f);
        }

        public override void Cast()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Casting), _baseLayerIndex, 0f);
        }

        public override void CastAttack()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.CastingAttack), _baseLayerIndex, 0f);
        }

        public override void CriticalAttack()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.CriticalAttack), _baseLayerIndex, 0f);
        }

        public override void Hit()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Hit), _baseLayerIndex, 0f);
        }

        public override void Win()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Win), _baseLayerIndex, 0f);
        }

        public override void Die()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Die), _baseLayerIndex, 0f);
        }

        public override void Disappear()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Disappear), _baseLayerIndex, 0f);
        }
    }
}
