using System.Linq;
using Nekoyume.Game.Controller;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PlayerAnimator : CharacterAnimator<SkeletonAnimation>
    {
        public PlayerAnimator(CharacterBase root) : base(root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            if (!ReferenceEquals(animator, null))
            {
                animator.AnimationState.Event -= OnEvent;
            }
            
            base.ResetTarget(value);
            
            if (ReferenceEquals(animator, null))
            {
                return;
            }

            animator.AnimationState.Event += OnEvent;
        }

        public override bool AnimatorValidation()
        {
            return !ReferenceEquals(animator, null);
        }

        public override Vector3 GetHUDPosition()
        {
            return Vector3.zero;
//            var face = target.GetComponentsInChildren<Transform>().First(g => g.name == "face");
//            var faceRenderer = face.GetComponent<Renderer>();
//            var x = faceRenderer.bounds.min.x - target.transform.position.x + faceRenderer.bounds.size.x / 2;
//            var y = faceRenderer.bounds.max.y - target.transform.position.y;
//            return new Vector3(x, y, 0.0f);
        }

        public override void SetTimeScale(float value)
        {
            if (!AnimatorValidation())
            {
                return;
            }
            
            animator.timeScale = value;
        }

        public override void Appear()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("appear"))
            {
                return;
            }

            animator.AnimationState.SetAnimation(0, "idle", true);
        }

        public override void Idle()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("idle"))
            {
                return;
            }
            
            animator.AnimationState.SetAnimation(0, "idle", true);
        }

        public override void Run()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("run"))
            {
                return;
            }
            
            animator.AnimationState.SetAnimation(0, "run", true);
        }

        public override void Attack()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("attack"))
            {
                return;
            }
            
            animator.AnimationState.SetAnimation(0, "attack", false);
        }

        public override void Hit()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("hit"))
            {
                return;
            }
            
            animator.AnimationState.SetAnimation(0, "hit", false);
        }

        public override void Die()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("die"))
            {
                return;
            }
            
            animator.AnimationState.SetAnimation(0, "die", false);
        }

        public override void Disappear()
        {
            if (!AnimatorValidation() ||
                animator.AnimationState.GetCurrent(0).Animation.Name.Equals("disappear"))
            {
                return;
            }
            
            animator.AnimationState.SetAnimation(0, "idle", true);
        }

        private void OnEvent(TrackEntry trackentry, Spine.Event e)
        {
            switch (e.Data.Name)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "attackPoint":
                    Event.OnAttackEnd.Invoke(root);
                    break;
                case "hitEnd":
                    Event.OnHitEnd.Invoke(root);
                    break;
                case "dieEnd":
                    Event.OnDieEnd.Invoke(root);
                    break;
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }
    }
}
