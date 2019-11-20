using System;
using System.Collections.Generic;
using Nekoyume.Helper;
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

        private const string DefaultPMAShader = "Spine/Skeleton";
        private const string DefaultStraightAlphaShader = "Sprites/Default";
        private const string WeaponSlot = "weapon";
        private const string EarLeftSlot = "ear_0001_L";
        private const string EarRightSlot = "ear_0001_R";
        private const string EyeOpenSlot = "eye_red_01";
        private const string EyeHalfSlot = "eye_red_02";
        private const string Hair01Slot = "hair_brown_01";
        private const string Hair02Slot = "hair_brown_02";
        private const string Hair03Slot = "hair_brown_03";
        private const string Hair04Slot = "hair_brown_04";
        private const string Hair05Slot = "hair_brown_05";
        private const string Hair06Slot = "hair_brown_06";
        private const string TailSlot = "tail_0002";

        public List<StateNameToAnimationReference> statesAndAnimations = new List<StateNameToAnimationReference>();

        public SkeletonAnimation SkeletonAnimation { get; private set; }

        private Spine.Animation TargetAnimation { get; set; }

        private Skin _clonedSkin;
        private bool _applyPMA;
        private Shader _shader;
        private Material _material;
        private AtlasPage _atlasPage;
        
        private Slot _earLeftSlot;
        private int _earLeftSlotIndex;
        private Slot _earRightSlot;
        private int _earRightSlotIndex;
        private Attachment _earLeftAttachmentDefault;
        private Attachment _earRightAttachmentDefault;
        
        private Slot _eyeOpenSlot;
        private int _eyeOpenSlotIndex;
        private Slot _eyeHalfSlot;
        private int _eyeHalfSlotIndex;
        private Attachment _eyeOpenAttachmentDefault;
        private Attachment _eyeHalfAttachmentDefault;

        private Slot _tailSlot;
        private int _tailSlotIndex;
        private Attachment _tailAttachmentDefault;
        
        private int _weaponSlotIndex;
        private RegionAttachment _weaponAttachmentDefault;

        #region Mono

        private void Awake()
        {
            foreach (var entry in statesAndAnimations)
            {
                entry.animation.Initialize();
            }

            SkeletonAnimation = GetComponent<SkeletonAnimation>();

            _clonedSkin = SkeletonAnimation.skeleton.Data.DefaultSkin.GetClone();
            _applyPMA = SkeletonAnimation.pmaVertexColors;
            _shader = _applyPMA ? Shader.Find(DefaultPMAShader) : Shader.Find(DefaultStraightAlphaShader);
            _material = new Material(_shader);
            _atlasPage = _material.ToSpineAtlasPage();

            _weaponSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(WeaponSlot);
            _weaponAttachmentDefault =
                MakeAttachment(SpriteHelper.GetPlayerSpineTextureWeapon(GameConfig.DefaultAvatarWeaponId));

            _eyeOpenSlot = SkeletonAnimation.skeleton.FindSlot(EyeOpenSlot);
            if (!(_eyeOpenSlot is null))
            {
                _eyeOpenSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EyeOpenSlot);
                _eyeHalfSlot = SkeletonAnimation.skeleton.FindSlot(EyeHalfSlot);
                _eyeHalfSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EyeHalfSlot);
                _eyeOpenAttachmentDefault = RemapAttachment(_eyeOpenSlot, SpriteHelper.GetPlayerSpineTextureEyeOpen(null));
                _eyeHalfAttachmentDefault =
                    RemapAttachment(_eyeHalfSlot, SpriteHelper.GetPlayerSpineTextureEyeHalf(null));
            }

            _earLeftSlot = SkeletonAnimation.skeleton.FindSlot(EarLeftSlot);
            if (!(_earLeftSlot is null))
            {
                _earLeftSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EarLeftSlot);
                _earLeftAttachmentDefault = RemapAttachment(_earLeftSlot, SpriteHelper.GetPlayerSpineTextureEarLeft(null));
            }

            _earRightSlot = SkeletonAnimation.skeleton.FindSlot(EarRightSlot);
            if (!(_earRightSlot is null))
            {
                _earRightSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EarRightSlot);
                _earRightAttachmentDefault =
                    RemapAttachment(_earRightSlot, SpriteHelper.GetPlayerSpineTextureEarRight(null));
            }

            _tailSlot = SkeletonAnimation.skeleton.FindSlot(TailSlot);
            if (!(_tailSlot is null))
            {
                _tailSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(TailSlot);
                _tailAttachmentDefault = RemapAttachment(_tailSlot, SpriteHelper.GetPlayerSpineTextureTail(null));
            }
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

        private int StringToHash(string s)
        {
            return Animator.StringToHash(s);
        }

        public void UpdateEar(Sprite spriteLeft, Sprite spriteRight)
        {
            if (_earLeftSlot is null || _earRightSlot is null)
                return;

            if (spriteLeft is null)
            {
                _clonedSkin.SetAttachment(_earLeftSlotIndex, EarLeftSlot, _earLeftAttachmentDefault);
            }
            else
            {
                var newEarLeft = RemapAttachment(_earLeftSlot, spriteLeft);
                _clonedSkin.SetAttachment(_earLeftSlotIndex, EarLeftSlot, newEarLeft);
            }

            if (spriteRight is null)
            {
                _clonedSkin.SetAttachment(_earRightSlotIndex, EarRightSlot, _earRightAttachmentDefault);    
            }
            else
            {
                var newEarRight = RemapAttachment(_earRightSlot, spriteRight);
                _clonedSkin.SetAttachment(_earRightSlotIndex, EarRightSlot, newEarRight);
            }

            UpdateInternal();
        }
        
        public void UpdateEye(Sprite spriteEyeOpen, Sprite spriteEyeHalf)
        {
            if (_eyeOpenSlot is null || _eyeHalfSlot is null)
                return;

            if (spriteEyeOpen is null)
            {
                _clonedSkin.SetAttachment(_eyeOpenSlotIndex, EyeOpenSlot, _eyeOpenAttachmentDefault);
            }
            else
            {
                var newEyeOpen = RemapAttachment(_eyeOpenSlot, spriteEyeOpen);
                _clonedSkin.SetAttachment(_eyeOpenSlotIndex, EyeOpenSlot, newEyeOpen);
            }
            
            if (spriteEyeHalf is null)
            {
                _clonedSkin.SetAttachment(_eyeHalfSlotIndex, EyeHalfSlot, _eyeHalfAttachmentDefault);
            }
            else
            {
                var newEyeHalf = RemapAttachment(_eyeHalfSlot, spriteEyeHalf);
                _clonedSkin.SetAttachment(_eyeHalfSlotIndex, EyeHalfSlot, newEyeHalf);
            }

            UpdateInternal();
        }

        public void UpdateTail(Sprite sprite)
        {
            if (_tailSlot is null)
                return;

            if (sprite is null)
            {
                _clonedSkin.SetAttachment(_tailSlotIndex, TailSlot, _tailAttachmentDefault);
            }
            else
            {
                var newTail = RemapAttachment(_tailSlot, sprite);
                _clonedSkin.SetAttachment(_tailSlotIndex, TailSlot, newTail);
            }

            UpdateInternal();
        }
        
        public void UpdateWeapon(Sprite sprite)
        {
            if (sprite is null)
            {
                _clonedSkin.SetAttachment(_weaponSlotIndex, WeaponSlot, _weaponAttachmentDefault);
            }
            else
            {
                var newWeapon = MakeAttachment(sprite);
                _clonedSkin.SetAttachment(_weaponSlotIndex, WeaponSlot, newWeapon);
            }

            UpdateInternal();
        }

        private void UpdateInternal()
        {
            var skeleton = SkeletonAnimation.skeleton;
            skeleton.SetSkin(_clonedSkin);
            skeleton.SetSlotsToSetupPose();
            SkeletonAnimation.Update(0);
        }

        private Attachment RemapAttachment(Slot slot, Sprite sprite)
        {
            return slot.Attachment.GetRemappedClone(sprite, _material);
        }

        private RegionAttachment MakeAttachment(Sprite sprite)
        {
            var attachment = _applyPMA
                ? sprite.ToRegionAttachmentPMAClone(_shader)
                : sprite.ToRegionAttachment(_atlasPage);

            return attachment;
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
            var loop = target.Name == nameof(CharacterAnimation.Type.Idle)
                       || target.Name == nameof(CharacterAnimation.Type.Run)
                       || target.Name == nameof(CharacterAnimation.Type.Casting);

            TargetAnimation = target;
            return SkeletonAnimation.AnimationState.SetAnimation(layerIndex, target, loop);
        }
    }
}
