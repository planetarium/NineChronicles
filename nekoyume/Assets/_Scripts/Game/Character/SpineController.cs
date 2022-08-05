using System;
using System.Collections.Generic;
using DG.Tweening;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
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

        protected SkeletonAnimation TailAnimation { get; set; }

        private bool _applyPMA;
        private Shader _shader;
        private Material _material;
        private AtlasPage _atlasPage;

        private Sequence _doFadeSequence;
        private Tweener _fadeTweener;

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

        private void OnDisable()
        {
            StopFade();
        }

        #endregion

        #region Fade

        public void Appear(float duration = 1f, bool fromZero = true, System.Action onComplete = null)
        {
            if (fromZero)
            {
                SkeletonAnimation.skeleton.A = 0f;
            }

            duration *= 1f - SkeletonAnimation.skeleton.A;
            StartFade(1f, duration, onComplete);
        }

        public void Disappear(float duration = 1f, bool fromOne = true, System.Action onComplete = null)
        {
            if (fromOne)
            {
                SkeletonAnimation.skeleton.A = 1f;
            }

            duration *= SkeletonAnimation.skeleton.A;
            StartFade(0f, duration, onComplete);
        }

        private void StartFade(float toValue, float duration, System.Action onComplete = null)
        {
            StopFade();
            _fadeTweener = DOTween
                .To(() => SkeletonAnimation.skeleton.A, value => SkeletonAnimation.skeleton.A = value, toValue, duration)
                .OnComplete(() => onComplete?.Invoke())
                .Play();
        }

        private void StopFade()
        {
            if (_fadeTweener is null ||
                !_fadeTweener.IsActive() ||
                !_fadeTweener.IsPlaying())
            {
                return;
            }

            _fadeTweener.Kill();
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
        public (TrackEntry, TrackEntry) PlayAnimationForState(int shortNameHash, int layerIndex)
        {
            var foundAnimation = GetAnimationForState(shortNameHash);
            if (foundAnimation is null)
                throw new KeyNotFoundException(nameof(shortNameHash));

            return PlayNewAnimation(foundAnimation, layerIndex);
        }

        public (TrackEntry, TrackEntry) PlayAnimationForState(string stateName, int layerIndex)
        {
            var foundAnimation = GetAnimationForState(stateName);
            if (foundAnimation is null)
                throw new KeyNotFoundException(nameof(stateName));

            return PlayNewAnimation(foundAnimation, layerIndex);
        }

        public (TrackEntry, TrackEntry) PlayAnimationForState(AnimationReferenceAsset stateAsset, int layerIndex)
        {
            return PlayNewAnimation(stateAsset, layerIndex);
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

        private static int StringToHash(string s)
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
        private (TrackEntry, TrackEntry) PlayNewAnimation(Spine.Animation target, int layerIndex)
        {
            TargetAnimation = target;
            var isLoop = IsLoopAnimation(TargetAnimation.Name);

            var body = SkeletonAnimation.AnimationState.SetAnimation(layerIndex, target, isLoop);
            TrackEntry tail = null;
            if (TailAnimation != null && TailAnimation.AnimationState != null)
            {
                tail = TailAnimation.AnimationState.SetAnimation(layerIndex, target.Name, isLoop);
            }

            return (body, tail);
        }
    }
}
