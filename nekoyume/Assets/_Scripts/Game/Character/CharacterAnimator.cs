using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterAnimator : SkeletonAnimator
    {
        private const string StringHUD = "HUD";
        private const string StringHealBorn = "HealBorn";
        private const float ColorTweenFrom = 0f;
        private const float ColorTweenTo = 0.6f;
        private const float ColorTweenDuration = 0.1f;
        private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");

        private Sequence _colorTweenSequence;
        private static readonly int PrologueSpeed = Animator.StringToHash("PrologueSpeed");

        private Vector3 HUDPosition { get; set; }
        public Vector3 HealPosition { get; private set; }

        protected CharacterAnimator(Character root) : base(root.gameObject)
        {
        }

        protected CharacterAnimator(PrologueCharacter root) : base(root.gameObject)
        {
        }

        public override void InitTarget(
            GameObject target,
            MeshRenderer meshRenderer,
            SkeletonAnimation skeletonAnimation,
            Animator animator)
        {
            base.InitTarget(target, meshRenderer, skeletonAnimation, animator);

            var hud = Skeleton.skeleton.FindBone(StringHUD);
            if (hud is null)
            {
                throw new SpineBoneNotFoundException(StringHUD);
            }

            HUDPosition = hud.GetWorldPosition(Target.transform) - Root.transform.position;

            var heal = Skeleton.skeleton.FindBone(StringHealBorn);
            if (heal != null)
            {
                HealPosition = heal.GetWorldPosition(Target.transform) - Root.transform.position;
            }
            else
            {
                HealPosition = hud.GetWorldPosition(Target.transform) - Root.transform.position;
            }
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);

            var hud = Skeleton.skeleton.FindBone(StringHUD);
            if (hud is null)
            {
                throw new SpineBoneNotFoundException(StringHUD);
            }

            HUDPosition = hud.GetWorldPosition(Target.transform) - Root.transform.position;

            var heal = Skeleton.skeleton.FindBone(StringHealBorn);
            if (heal != null)
            {
                HealPosition = heal.GetWorldPosition(Target.transform) - Root.transform.position;
            }
            else
            {
                HealPosition = hud.GetWorldPosition(Target.transform) - Root.transform.position;
            }
        }

        public Vector3 GetHUDPosition()
        {
            return HUDPosition;
        }

        #region Animation

        public void Play(CharacterAnimation.Type type)
        {
            switch (type)
            {
                case CharacterAnimation.Type.Standing:
                    Standing();
                    break;
                case CharacterAnimation.Type.StandingToIdle:
                    StandingToIdle();
                    break;
                case CharacterAnimation.Type.Idle:
                    Idle();
                    break;
                case CharacterAnimation.Type.Run:
                    Run();
                    break;
                case CharacterAnimation.Type.Skill_01:
                    Skill(1);
                    break;
                case CharacterAnimation.Type.Skill_02:
                    Skill(2);
                    break;
                default:
                    Animator.Rebind();
                    Animator.Play(type.ToString(), BaseLayerIndex, 0f);
                    break;
            }
        }
        public bool HasType(CharacterAnimation.Type type)
        {
            var stateId = Animator.StringToHash(type.ToString());
            return Animator.HasState(BaseLayerIndex, stateId);
        }

        public void Standing()
        {
            Animator.SetFloat(PrologueSpeed, 0.1f);
            if (!ValidateAnimator())
            {
                return;
            }

            if (Animator.GetBool(nameof(CharacterAnimation.Type.Standing)))
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Standing), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), true);
        }

        public void StandingToIdle()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
        }

        public void Idle()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Idle), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
        }

        public void Touch()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Touch), BaseLayerIndex, 0f);
        }

        public void Run()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            if (Animator.GetBool(nameof(CharacterAnimation.Type.Run)))
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Run), BaseLayerIndex, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), true);
        }

        public void StopRun()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
        }

        public void Attack()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Attack), BaseLayerIndex, 0f);
        }

        public float AnimationLength()
        {
            return Animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).length;
        }

        public void Cast()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Casting), BaseLayerIndex, 0f);
        }

        public void CastAttack()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.CastingAttack), BaseLayerIndex, 0f);
        }

        public void CriticalAttack()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.CriticalAttack), BaseLayerIndex, 0f);
        }

        public void Hit()
        {
            if (!ValidateAnimator() || !Animator.GetCurrentAnimatorStateInfo(BaseLayerIndex)
                    .IsName(nameof(CharacterAnimation.Type.Idle)))
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Hit), BaseLayerIndex, 0f);
        }

        public void Win(int score = 3)
        {
            if (!ValidateAnimator())
            {
                return;
            }

            var animationType = CharacterAnimation.Type.Win;

            switch (score)
            {
                case 2:
                    animationType = CharacterAnimation.Type.Win_02;
                    break;
                case 3:
                    animationType = CharacterAnimation.Type.Win_03;
                    break;
            }

            Animator.Play(animationType.ToString(), BaseLayerIndex, 0f);
            ColorTween();
        }

        public void TurnOver()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.TurnOver_02), BaseLayerIndex, 0f);
            ColorTween();
        }

        public void Die()
        {
            if (!ValidateAnimator())
            {
                return;
            }

            Animator.Play(nameof(CharacterAnimation.Type.Die), BaseLayerIndex, 0f);
            ColorTween();
        }

        public void Skill(int animationId = 1)
        {
            if (!ValidateAnimator())
            {
                return;
            }

            var animation = animationId == 1 ? CharacterAnimation.Type.Skill_01 : CharacterAnimation.Type.Skill_02;
            Animator.Play(animation.ToString(), BaseLayerIndex, 0f);
        }

        #endregion

        private void ColorTween()
        {
            if (MeshRenderer == null)
            {
                return;
            }

            var mat = MeshRenderer.material;

            _colorTweenSequence?.Kill();

            _colorTweenSequence = DOTween.Sequence();
            _colorTweenSequence.Append(DOTween.To(
                () => ColorTweenFrom,
                value => mat.SetFloat(FillPhase, value),
                ColorTweenTo,
                ColorTweenDuration));
            _colorTweenSequence.Append(DOTween.To(
                () => ColorTweenTo,
                value => mat.SetFloat(FillPhase, value),
                ColorTweenFrom,
                ColorTweenDuration));
            _colorTweenSequence.Play().OnComplete(() => _colorTweenSequence = null);
        }

        public bool IsIdle()
        {
            return Animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).IsName(nameof(CharacterAnimation.Type.Idle));
        }
    }
}
