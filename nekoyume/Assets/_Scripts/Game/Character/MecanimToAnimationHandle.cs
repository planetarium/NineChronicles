using Spine.Unity.Examples;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class MecanimToAnimationHandle : StateMachineBehaviour {
        private SkeletonAnimationController _controller;

        public override void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (ReferenceEquals(_controller, null)) {
                _controller = animator.GetComponent<SkeletonAnimationController>();

                if (ReferenceEquals(_controller, null))
                {
                    throw new NotFoundComponentException<SkeletonAnimationController>();
                }
            }

            _controller.PlayAnimationForState(stateInfo.shortNameHash, layerIndex);
        }
    }
}
