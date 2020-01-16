using Spine;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class SkeletonAnimator : MecanimAnimator
    {
        protected MeshRenderer MeshRenderer { get; private set; }
        protected SkeletonAnimation Skeleton { get; private set; }

        public SkeletonAnimator(CharacterBase root) : base(root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);

            if (!(Skeleton is null))
            {
                Skeleton.AnimationState.Event -= RaiseEvent;
            }

            MeshRenderer = value.GetComponent<MeshRenderer>();
            if (MeshRenderer is null)
                throw new NotFoundComponentException<MeshRenderer>();

            Skeleton = value.GetComponent<SkeletonAnimation>();
            if (Skeleton is null)
                throw new NotFoundComponentException<SkeletonAnimation>();

            Skeleton.timeScale = TimeScale;
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
