using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CutSceneTest : HudWidget
    {
        public enum AnimationType
        {
            Type4,
            Type5,
        }
        public const float DestroyDelay = 1.5f;

        private SkeletonAnimator skeletonAnimator;
        public Animator animator;

        public static CutSceneTest Show(AnimationType type)
        {
            var widget = Create<CutSceneTest>(true);

            widget.skeletonAnimator = new SkeletonAnimator(widget.gameObject);
            widget.skeletonAnimator.TimeScale = 1f;
            widget.skeletonAnimator.ResetTarget(widget.gameObject);

            if (!widget.skeletonAnimator.ValidateAnimator())
            {
                Destroy(widget.gameObject);
                return null;
            }

            var animationName = type.ToString().Remove(0, 4);

            var baseLayerIndex = widget.Animator.GetLayerIndex("Base Layer");
            widget.Animator.Play(animationName, baseLayerIndex, 0f);

            Destroy(widget.gameObject, DestroyDelay);

            return widget;
        }
    }
}
