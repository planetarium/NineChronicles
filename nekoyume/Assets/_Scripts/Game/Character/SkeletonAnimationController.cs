using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.Modules.AttachmentTools;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    [RequireComponent(typeof(SkeletonAnimation))]
    public class SkeletonAnimationController : MonoBehaviour
    {
        [Serializable]
        public class StateNameToAnimationReference
        {
            public string stateName;
            public AnimationReferenceAsset animation;
        }

        public List<StateNameToAnimationReference> statesAndAnimations = new List<StateNameToAnimationReference>();

        public SkeletonAnimation skeletonAnimation;

        private Spine.Animation TargetAnimation { get; set; }

        [SpineSlot] public string weaponSlot = "sword_0001";

        [SpineAttachment(slotField: "weaponSlot")]
        public string weaponAttachment;

        #region Mono

        private void Awake()
        {
            foreach (var entry in statesAndAnimations)
            {
                entry.animation.Initialize();
            }

            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        #endregion

        /// <summary>Sets the horizontal flip state of the skeleton based on a nonzero float. If negative, the skeleton is flipped. If positive, the skeleton is not flipped.</summary>
        public void SetFlip(float horizontal)
        {
            if (Math.Abs(horizontal) > 0f)
            {
                skeletonAnimation.Skeleton.ScaleX = horizontal > 0 ? 1f : -1f;
            }
        }

        /// <summary>Plays an  animation based on the hash of the state name.</summary>
        public void PlayAnimationForState(int shortNameHash, int layerIndex)
        {
            var foundAnimation = GetAnimationForState(shortNameHash);
            if (foundAnimation == null)
                return;

            PlayNewAnimation(foundAnimation, layerIndex);
        }

        /// <summary>Play a non-looping animation once then continue playing the state animation.</summary>
        public void PlayOneShot(Spine.Animation oneShot, int layerIndex)
        {
            var state = skeletonAnimation.AnimationState;
            state.SetAnimation(0, oneShot, false);
            state.AddAnimation(0, TargetAnimation, true, 0f);
        }

        private int StringToHash(string s)
        {
            return Animator.StringToHash(s);
        }

        public void UpdateWeapon(Sprite sprite)
        {
            var skeleton = skeletonAnimation.skeleton;
            int weaponIndex = skeleton.FindSlotIndex(weaponSlot);
            var weapon = skeletonAnimation.skeleton.GetAttachment(weaponSlot, weaponSlot);
            var skin = new Skin("weapon switch");
            if (sprite != null)
            {
                var rend = GetComponentInParent<MeshRenderer>();
                var material = rend.material;
                var newWeapon = weapon.GetRemappedClone(sprite, material);
                skin.AddAttachment(weaponIndex, weaponSlot, newWeapon);
            }
            else
            {
                skin = skeleton.Data.FindSkin("default");
            }

            skeleton.SetSkin(skin);
            skeleton.SetSlotsToSetupPose();
            skeletonAnimation.Update(0);
        }

        /// <summary>Gets a Spine Animation based on the hash of the state name.</summary>
        private Spine.Animation GetAnimationForState(int shortNameHash)
        {
            var foundState = statesAndAnimations.Find(entry => StringToHash(entry.stateName) == shortNameHash);
            return foundState?.animation;
        }

        /// <summary>Play an animation. If a transition animation is defined, the transition is played before the target animation being passed.</summary>
        private void PlayNewAnimation(Spine.Animation target, int layerIndex)
        {
            var loop = target.Name == nameof(CharacterAnimation.Type.Idle)
                       || target.Name == nameof(CharacterAnimation.Type.Run)
                       || target.Name == nameof(CharacterAnimation.Type.Casting);

            skeletonAnimation.AnimationState.SetAnimation(layerIndex, target, loop);
            TargetAnimation = target;
        }
    }
}
