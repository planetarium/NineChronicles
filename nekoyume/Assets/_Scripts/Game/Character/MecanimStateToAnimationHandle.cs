using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace Nekoyume.Game.Character
{
    public class MecanimStateToAnimationHandle : StateMachineBehaviour
    {
        private static readonly int TransitionHash = Animator.StringToHash("Transition");

        public int layer;
        public string animationClip;
        public AnimationReferenceAsset animationAsset;
        public float timeScale = 1f;
        public float exitTime = 1f;
        public bool loop;

        private SpineController _controller;
        private SkeletonAnimation _skeletonAnimation;
        private Spine.AnimationState _spineAnimationState;
        private Spine.TrackEntry _trackEntry;
        private float _normalizedTime;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (!_controller)
            {
                _controller = animator.GetComponent<SpineController>();
                if (!_controller)
                {
                    throw new NotFoundComponentException<SpineController>();
                }
            }

            if (!_skeletonAnimation)
            {
                _skeletonAnimation = animator.GetComponentInChildren<SkeletonAnimation>();
                if (!_skeletonAnimation)
                {
                    throw new NotFoundComponentException<SkeletonAnimation>();
                }

                _spineAnimationState = _skeletonAnimation.state;
            }

            try
            {
                var (body, tail) = animationAsset ?
                    _controller.PlayAnimationForState(animationAsset, layer, () => OnEnd(animator)) :
                    _controller.PlayAnimationForState(animationClip, layer, () => OnEnd(animator));
                _trackEntry = body;
                _trackEntry.TimeScale = timeScale;
                if (tail != null)
                {
                    tail.TimeScale = timeScale;
                }

                _normalizedTime = 0f;
            }
            catch (KeyNotFoundException)
            {
                NcDebug.LogError($"{nameof(_trackEntry)} is null! :{animationClip.ToString()} : {layer.ToString()}");
            }
        }

        // public override void OnStateUpdate(
        //     Animator animator,
        //     AnimatorStateInfo stateInfo,
        //     int layerIndex)
        // {
        //     if (loop)
        //     {
        //         return;
        //     }
        //
        //     if (_trackEntry is null)
        //     {
        //         return;
        //     }
        //
        //     _normalizedTime = _trackEntry.AnimationTime / _trackEntry.AnimationEnd;
        //     if (_normalizedTime >= exitTime)
        //     {
        //         animator.SetTrigger(TransitionHash);
        //     }
        // }

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
