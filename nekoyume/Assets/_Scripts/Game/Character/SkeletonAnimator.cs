using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class SkeletonAnimator : MecanimAnimator
    {
        protected MeshRenderer MeshRenderer { get; private set; }
        protected SkeletonAnimation Skeleton { get; private set; }

        public SkeletonAnimator(GameObject root) : base(root)
        {
        }

        public override void InitTarget(
            GameObject target,
            MeshRenderer meshRenderer,
            SkeletonAnimation skeletonAnimation,
            Animator animator)
        {
            base.InitTarget(target, meshRenderer, skeletonAnimation, animator);

            if (Skeleton?.AnimationState != null)
            {
                Skeleton.AnimationState.Event -= RaiseEvent;
            }

            MeshRenderer = meshRenderer;
            if (MeshRenderer is null)
            {
                throw new NotFoundComponentException<MeshRenderer>();
            }

            Skeleton = skeletonAnimation;
            if (Skeleton is null)
            {
                throw new NotFoundComponentException<SkeletonAnimation>();
            }

            var skeletons = target.GetComponentsInChildren<SkeletonAnimation>();
            foreach (var skeleton in skeletons)
            {
                skeleton.timeScale = TimeScale;
            }

            if (Skeleton.AnimationState is null)
            {
                throw new NullReferenceException(nameof(Skeleton.AnimationState));
            }

            Skeleton.AnimationState.Event += RaiseEvent;
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);

            if (Skeleton != null && Skeleton.AnimationState != null)
            {
                Skeleton.AnimationState.Event -= RaiseEvent;
            }

            MeshRenderer = value.GetComponent<MeshRenderer>();
            if (MeshRenderer is null)
            {
                throw new NotFoundComponentException<MeshRenderer>();
            }

            Skeleton = value.GetComponent<SkeletonAnimation>();
            if (Skeleton is null)
            {
                throw new NotFoundComponentException<SkeletonAnimation>();
            }

            var skeletons = value.GetComponentsInChildren<SkeletonAnimation>();
            foreach (var skeleton in skeletons)
            {
                skeleton.timeScale = TimeScale;
            }

            if (Skeleton.AnimationState is null)
            {
                throw new NullReferenceException(nameof(Skeleton.AnimationState));
            }

            Skeleton.AnimationState.Event += RaiseEvent;
        }

        public override bool ValidateAnimator()
        {
            return base.ValidateAnimator() && !ReferenceEquals(Skeleton, null);
        }

        private void RaiseEvent(TrackEntry trackEntry, Spine.Event e)
        {
            OnEvent.OnNext(e.Data.Name);
        }
    }
}
