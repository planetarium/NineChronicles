using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.Modules.AttachmentTools;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(SkeletonAnimation))]
    public class SpineController : MonoBehaviour
    {
        [Serializable]
        public class StateNameToAnimationReference
        {
            public string stateName;
            public AnimationReferenceAsset animation;
        }

        private const string DefaultPMAShader = "Spine/Skeleton";
        private const string DefaultStraightAlphaShader = "Sprites/Default";

        public List<StateNameToAnimationReference> statesAndAnimations = new List<StateNameToAnimationReference>();

        public SkeletonAnimation SkeletonAnimation { get; private set; }
        public BoxCollider BoxCollider { get; private set; }

        private Spine.Animation TargetAnimation { get; set; }

        private bool _applyPMA;
        private Shader _shader;
        private Material _material;
        private AtlasPage _atlasPage;

        #region Mono

        protected virtual void Awake()
        {
            foreach (var entry in statesAndAnimations)
            {
                entry.animation.Initialize();
            }

            BoxCollider = GetComponent<BoxCollider>();
            BoxCollider.enabled = false;
            SkeletonAnimation = GetComponent<SkeletonAnimation>();

            _applyPMA = SkeletonAnimation.pmaVertexColors;
            _shader = _applyPMA ? Shader.Find(DefaultPMAShader) : Shader.Find(DefaultStraightAlphaShader);
            _material = new Material(_shader);
            _atlasPage = _material.ToSpineAtlasPage();
        }

        #endregion

        /// <summary>Sets the horizontal flip state of the skeleton based on a nonzero float. If negative, the skeleton is flipped. If positive, the skeleton is not flipped.</summary>
        public void SetFlip(float horizontal)
        {
            if (Math.Abs(horizontal) > 0f)
            {
                SkeletonAnimation.Skeleton.ScaleX = horizontal > 0 ? 1f : -1f;
            }
        }

        /// <summary>Plays an  animation based on the hash of the state name.</summary>
        public TrackEntry PlayAnimationForState(int shortNameHash, int layerIndex)
        {
            var foundAnimation = GetAnimationForState(shortNameHash);
            if (foundAnimation == null)
            {
                return null;
            }

            return PlayNewAnimation(foundAnimation, layerIndex);
        }

        public TrackEntry PlayAnimationForState(string stateName, int layerIndex)
        {
            var foundAnimation = GetAnimationForState(stateName);
            if (foundAnimation == null)
            {
                return null;
            }

            return PlayNewAnimation(foundAnimation, layerIndex);
        }

        /// <summary>Play a non-looping animation once then continue playing the state animation.</summary>
        public void PlayOneShot(Spine.Animation oneShot, int layerIndex)
        {
            var state = SkeletonAnimation.AnimationState;
            state.SetAnimation(0, oneShot, false);
            state.AddAnimation(0, TargetAnimation, true, 0f);
        }
        
        protected Attachment RemapAttachment(Slot slot, Sprite sprite)
        {
            return slot.Attachment.GetRemappedClone(sprite, _material);
        }

        protected RegionAttachment MakeAttachment(Sprite sprite)
        {
            var attachment = _applyPMA
                ? sprite.ToRegionAttachmentPMAClone(_shader)
                : sprite.ToRegionAttachment(_atlasPage);

            return attachment;
        }
        
        protected virtual bool IsLoopAnimation(string animationName)
        {
            return true;
        }

        private int StringToHash(string s)
        {
            return Animator.StringToHash(s);
        }

        /// <summary>Gets a Spine Animation based on the hash of the state name.</summary>
        private Spine.Animation GetAnimationForState(int shortNameHash)
        {
            var foundState = statesAndAnimations.Find(entry => StringToHash(entry.stateName) == shortNameHash);
            return foundState?.animation;
        }

        private Spine.Animation GetAnimationForState(string stateName)
        {
            var foundState = statesAndAnimations.Find(entry => entry.stateName == stateName);
            return foundState?.animation;
        }

        /// <summary>Play an animation. If a transition animation is defined, the transition is played before the target animation being passed.</summary>
        private TrackEntry PlayNewAnimation(Spine.Animation target, int layerIndex)
        {
            TargetAnimation = target;
            var isLoop = IsLoopAnimation(TargetAnimation.Name);
            return SkeletonAnimation.AnimationState.SetAnimation(layerIndex, target, isLoop);
        }
    }
}
