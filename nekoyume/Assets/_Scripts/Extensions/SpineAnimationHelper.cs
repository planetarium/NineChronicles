using Nekoyume.Game.Character;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Avatar;
using UnityEngine;

namespace Nekoyume
{
    public static class SpineAnimationHelper
    {
        public static float GetAnimationDuration(SpineController controller, string stateName)
        {
            var state = controller.statesAndAnimations
                .FirstOrDefault(x => x.stateName == stateName);
            return state != null ? state.animation.Animation.Duration : 2f;
        }

        public static float GetAnimationDuration(CharacterAppearance appearance, string stateName)
        {
            return GetAnimationDuration(appearance.SpineController, stateName);
        }

        public static float GetAnimationDuration(AvatarSpineController controller, string stateName)
        {
            var state = controller.GetBodySkeletonAnimation().Skeleton.Data.FindAnimation(stateName);
            return state?.Duration ?? 2f;
        }
    }
}
