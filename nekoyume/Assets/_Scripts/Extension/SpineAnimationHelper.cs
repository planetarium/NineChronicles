using Nekoyume.Game.Character;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume
{
    public static class SpineAnimationHelper
    {
        public static float GetAnimationDuration(CharacterAppearance appearance, string stateName) =>
            GetAnimationDuration(appearance.SpineController, stateName);

        public static float GetAnimationDuration(SpineController controller, string stateName)
        {
            var state = controller.statesAndAnimations
                .FirstOrDefault(x => x.stateName == stateName);
            return state != null ? state.animation.Animation.Duration : 2f;
        }
    }
}
