using Spine.Unity;
using UnityEngine;
using UnityEngine.Animations;

namespace Nekoyume.Game.Avatar
{
    public class AvatarStateMachine : StateMachineBehaviour
    {
        private static readonly int TransitionHash = Animator.StringToHash("Transition");
        public int layer;
        public string animationClip;
        public AnimationReferenceAsset animationAsset;
        public bool loop;

        private AvatarSpineController _controller;
        private Spine.TrackEntry _trackEntry;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (_controller == null)
            {
                _controller = animator.GetComponent<AvatarSpineController>();
            }

            var animationName = animationAsset ? animationAsset.Animation.Name : animationClip;
            _controller.PlayAnimation(animationName, layer);
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex,
            AnimatorControllerPlayable controller)
        {
            if (loop)
            {
                return;
            }

            animator.SetTrigger(TransitionHash);
        }
    }
}
