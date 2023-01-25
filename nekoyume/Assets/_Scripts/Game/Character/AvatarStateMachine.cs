using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class AvatarStateMachine : StateMachineBehaviour
    {
        private static readonly int TransitionHash = Animator.StringToHash("Transition");
        public int layer;
        public string animationClip;
        public AnimationReferenceAsset animationAsset;
        public float timeScale = 1f;
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

            if (animationAsset)
            {
                _controller.PlayAnimationForState(animationAsset, layer, timeScale,
                    () => OnEnd(animator));
            }
            else
            {
                _controller.PlayAnimationForState(animationClip, layer, timeScale,
                    () => OnEnd(animator));
            }
        }

        private void OnEnd(Animator animator)
        {
            if (loop)
            {
                return;
            }

            animator.SetTrigger(TransitionHash);
        }
    }
}
