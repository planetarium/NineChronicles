using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterAnimator : SkeletonAnimator, ICharacterAnimator
    {
        private const string StringHUD = "HUD";
        private const float ColorTweenFrom = 0f;
        private const float ColorTweenTo = 0.6f;
        private const float ColorTweenDuration = 0.1f;
        private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");

        private Sequence _colorTweenSequence;
        
        private Vector3 HUDPosition { get; set; }

        protected CharacterAnimator(CharacterBase root) : base(root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);
            
            var hud = Skeleton.skeleton.FindBone(StringHUD);
            if (hud is null)
                throw new SpineBoneNotFoundException(StringHUD);
            HUDPosition = hud.GetWorldPosition(Target.transform) - Root.transform.position;
        }
        
        public Vector3 GetHUDPosition()
        {
            return HUDPosition;
        }

        #region Animation

        public void Appear()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Appear), BaseLayerIndex, 0f);
        }

        public void Standing()
        {
            if (!ValidateAnimator())
                return;

            if (Animator.GetBool(nameof(CharacterAnimation.Type.Standing)))
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Standing), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), true);
        }

        public void StandingToIdle()
        {
            if (!ValidateAnimator())
                return;

            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
        }

        public void Idle()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Idle), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
            Animator.SetBool(nameof(CharacterAnimation.Type.Touch), false);
        }

        public void Touch()
        {
            if (!ValidateAnimator())
                return;

            if (Animator.GetBool(nameof(CharacterAnimation.Type.Touch)))
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Touch), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Touch), true);
        }

        public void Run()
        {
            if (!ValidateAnimator())
                return;

            if (Animator.GetBool(nameof(CharacterAnimation.Type.Run)))
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Run), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), true);
        }

        public void StopRun()
        {
            if (!ValidateAnimator())
                return;

            Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
        }

        public void Attack()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Attack), BaseLayerIndex, 0f);
        }

        public void Cast()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Casting), BaseLayerIndex, 0f);
        }

        public void CastAttack()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.CastingAttack), BaseLayerIndex, 0f);
        }

        public void CriticalAttack()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.CriticalAttack), BaseLayerIndex, 0f);
        }

        public void Hit()
        {
            if (!ValidateAnimator() || !Animator.GetCurrentAnimatorStateInfo(_baseLayerIndex).IsName(nameof(CharacterAnimation.Type.Idle)))
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Hit), BaseLayerIndex, 0f);
        }

        public void Win()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Win), BaseLayerIndex, 0f);
            ColorTween();
        }

        public void Die()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Die), BaseLayerIndex, 0f);
            ColorTween();
        }

        public void Disappear()
        {
            if (!ValidateAnimator())
                return;

            Animator.Play(nameof(CharacterAnimation.Type.Disappear), BaseLayerIndex, 0f);
        }

        #endregion

        public void Dispose()
        {
            OnEvent?.Dispose();
        }

        private void ColorTween()
        {
            var mat = MeshRenderer.material;

            _colorTweenSequence?.Kill();

            _colorTweenSequence = DOTween.Sequence();
            _colorTweenSequence.Append(DOTween.To(() => ColorTweenFrom, value => mat.SetFloat(FillPhase, value),
                ColorTweenTo, ColorTweenDuration));
            _colorTweenSequence.Append(DOTween.To(() => ColorTweenTo, value => mat.SetFloat(FillPhase, value),
                ColorTweenFrom, ColorTweenDuration));
            _colorTweenSequence.Play().OnComplete(() => _colorTweenSequence = null);
        }
    }
}
