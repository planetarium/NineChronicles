using System;
using System.Collections.Generic;
using DG.Tweening;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    [RequireComponent(typeof(SkeletonAnimation))]
    public class SpineController : MonoBehaviour
    {
        [Serializable]
        public class StateNameToAnimationReference
        {
            public string stateName;
            public AnimationReferenceAsset animation;
        }

        private const string DefaultPMAShader = "Spine/Skeleton Tint";
        private const string DefaultShader    = "Sprites/Default";

        public List<StateNameToAnimationReference> statesAndAnimations = new();

        public SkeletonAnimation SkeletonAnimation { get; private set; }
        public BoxCollider BoxCollider { get; private set; }

        private Spine.Animation TargetAnimation { get; set; }

        protected SkeletonAnimation TailAnimation { get; set; }

        private bool _applyPMA;
        private Shader _shader;
        private Material _material;
        private AtlasPage _atlasPage;

        private DG.Tweening.Sequence _doFadeSequence;
        private Tweener _fadeTweener;
        private System.Action _callback;

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
            SkeletonAnimation.AnimationState.Complete += delegate { _callback?.Invoke(); };

            _applyPMA  = SkeletonAnimation.pmaVertexColors;
            _shader    = _applyPMA ? Shader.Find(DefaultPMAShader) : Shader.Find(DefaultShader);
            _material  = new Material(_shader);
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

        public (TrackEntry, TrackEntry) PlayAnimationForState(string stateName, int layerIndex, System.Action callback)
        {
            var foundAnimation = GetAnimationForState(stateName);
            if (foundAnimation is null)
                throw new KeyNotFoundException(nameof(stateName));

            return PlayNewAnimation(stateName, layerIndex, callback);
            // return PlayNewAnimation(foundAnimation, layerIndex, callback);
        }

        public (TrackEntry, TrackEntry) PlayAnimationForState(AnimationReferenceAsset stateAsset, int layerIndex, System.Action callback)
        {
            return PlayNewAnimation(stateAsset.Animation.Name, layerIndex, callback);
            // return PlayNewAnimation(stateAsset, layerIndex, callback);
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

        private (TrackEntry, TrackEntry) PlayNewAnimation(String name, int layerIndex, System.Action callback)
        {
            _callback = callback;
            var isLoop = IsLoopAnimation(name);

            var animationName = name;
            var animations = SkeletonAnimation.skeleton.Data.Animations;

            if (!animations.Exists(x => x.Name == animationName))
            {
                switch (animationName)
                {
                    case "CastingAttack":
                    case "CriticalAttack":
                    case "Touch":
                        animationName = "Attack";
                        break;
                    default:
                        var splits = animationName.Split('_');
                        animationName = splits[0];
                        if (!animations.Exists(x => x.Name == animationName))
                        {
                            animationName = "Idle";
                        }
                        break;
                }
            }

            var body = SkeletonAnimation.AnimationState.SetAnimation(layerIndex, animationName, isLoop);

            TrackEntry tail = null;
            if (TailAnimation != null && TailAnimation.AnimationState != null)
            {
                tail = TailAnimation.AnimationState.SetAnimation(layerIndex, animationName, isLoop);
            }

            return (body, tail);
        }

        /// <summary>Returns the axis aligned bounding box (AABB) of the region and mesh attachments for the current pose.</summary>
        /// <param name="x">The horizontal distance between the skeleton origin and the left side of the AABB.</param>
        /// <param name="y">The vertical distance between the skeleton origin and the bottom side of the AABB.</param>
        /// <param name="width">The width of the AABB</param>
        /// <param name="height">The height of the AABB.</param>
        /// <returns>Reference to hold a float[]. May be a null reference. This method will assign it a new float[] with the appropriate size as needed.</returns>
        public float[] GetSpineBound(out float x, out float y, out float width, out float height)
        {
            float[] vertexBuffer = null;
            SkeletonAnimation.Skeleton.GetBounds(out x, out y, out width, out height, ref vertexBuffer);
            return vertexBuffer;
        }
    }
}
