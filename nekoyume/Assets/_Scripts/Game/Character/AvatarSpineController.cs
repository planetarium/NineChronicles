using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using UnityEngine;
using DG.Tweening;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;

namespace Nekoyume
{
    public class AvatarSpineController : MonoBehaviour
    {
        private const string DefaultPmaShader = "Spine/Skeleton";

        [Serializable]
        public class StateNameToAnimationReference
        {
            public string stateName;
            public AnimationReferenceAsset animation;
        }

        [SerializeField]
        private List<AvatarParts> parts;

        private Shader _shader;
        private Material _material;
        private AtlasPage _atlasPage;
        private Spine.Animation _targetAnimation;
        private Sequence _doFadeSequence;
        private System.Action _callback;
        private readonly List<Tweener> _fadeTweener = new();
        public List<StateNameToAnimationReference> statesAndAnimations = new();
        public BoxCollider BoxCollider { get; private set; }

        private void Awake()
        {
            foreach (var entry in statesAndAnimations)
            {
                entry.animation.Initialize();
            }

            foreach (var p in parts)
            {
                p.SkeletonAnimation.AnimationState.End += delegate { _callback?.Invoke(); };
            }

            BoxCollider = GetComponent<BoxCollider>();
            BoxCollider.enabled = false;
            _shader = Shader.Find(DefaultPmaShader);
            _material = new Material(_shader);
            _atlasPage = _material.ToSpineAtlasPage();
        }

        private void OnDisable()
        {
            StopFade();
        }

        public void Appear(float duration = 1f, System.Action onComplete = null)
        {
            foreach (var p in parts)
            {
                p.SkeletonAnimation.skeleton.A = 0;
            }

            StartFade(1f, duration, onComplete);
        }

        public void Disappear(float duration = 1f, System.Action onComplete = null)
        {
            foreach (var p in parts)
            {
                p.SkeletonAnimation.skeleton.A = 1;
            }

            StartFade(0f, duration, onComplete);
        }

        private void StartFade(float toValue, float duration, System.Action onComplete = null)
        {
            StopFade();
            foreach (var p in parts)
            {
                var tweener = DOTween
                    .To(() => p.SkeletonAnimation.skeleton.A,
                        value => p.SkeletonAnimation.skeleton.A = value, toValue, duration)
                    .OnComplete(() => onComplete?.Invoke())
                    .Play();
                _fadeTweener.Add(tweener);
            }
        }

        private void StopFade()
        {
            foreach (var tweener in _fadeTweener)
            {
                tweener.Kill();
            }

            _fadeTweener.Clear();
        }

        public void PlayAnimationForState(
            string stateName,
            int layerIndex,
            float timeScale,
            System.Action callback)
        {
            var foundAnimation = GetAnimationForState(stateName);
            if (foundAnimation is null)
            {
                throw new KeyNotFoundException(nameof(stateName));
            }

            PlayNewAnimation(foundAnimation, layerIndex, timeScale, callback);
        }

        public void PlayAnimationForState(
            AnimationReferenceAsset stateAsset,
            int layerIndex,
            float timeScale,
            System.Action callback)
        {
            PlayNewAnimation(stateAsset, layerIndex, timeScale, callback);
        }

        private void PlayNewAnimation(
            Spine.Animation target,
            int layerIndex,
            float timeScale,
            System.Action callback)
        {
            _targetAnimation = target;
            var isLoop = IsLoopAnimation(_targetAnimation.Name);
            foreach (var p in parts)
            {
                var entry = p.SkeletonAnimation.AnimationState.SetAnimation(layerIndex, target, isLoop);
                entry.TimeScale = timeScale;
            }
            _callback = callback;
        }

        protected Attachment RemapAttachment(Slot slot, Sprite sprite)
        {
            return slot.Attachment.GetRemappedClone(sprite, _material);
        }

        protected RegionAttachment MakeAttachment(Sprite sprite)
        {
            return sprite.ToRegionAttachmentPMAClone(_shader);
        }

        protected virtual bool IsLoopAnimation(string animationName)
        {
            return animationName
                is nameof(CharacterAnimation.Type.Idle)
                or nameof(CharacterAnimation.Type.Run)
                or nameof(CharacterAnimation.Type.Casting)
                or nameof(CharacterAnimation.Type.TurnOver_01);
        }

        private static int StringToHash(string s)
        {
            return Animator.StringToHash(s);
        }

        /// <summary>Gets a Spine Animation based on the hash of the state name.</summary>
        private Spine.Animation GetAnimationForState(int shortNameHash)
        {
            var foundState =
                statesAndAnimations.Find(entry => StringToHash(entry.stateName) == shortNameHash);
            return foundState?.animation;
        }

        private Spine.Animation GetAnimationForState(string stateName)
        {
            var foundState = statesAndAnimations.Find(entry => entry.stateName == stateName);
            return foundState?.animation;
        }
    }
}
