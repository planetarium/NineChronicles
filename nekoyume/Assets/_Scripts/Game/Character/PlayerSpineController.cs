using System.Collections.Generic;
using Nekoyume.Helper;
using Spine;
using Spine.Unity.Modules.AttachmentTools;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PlayerSpineController : CharacterSpineController
    {
        private const string WeaponSlot = "weapon";
        private const string EarLeftSlot = "ear_L";
        private const string EarRightSlot = "ear_R";
        private const string EyeOpenSlot = "eye_01";
        private const string EyeHalfSlot = "eye_02";
        private const string Hair01Slot = "hair_01";
        private const string Hair02Slot = "hair_02";
        private const string Hair03Slot = "hair_03";
        private const string Hair04Slot = "hair_04";
        private const string Hair05Slot = "hair_05";
        private const string Hair06Slot = "hair_06";
        private const string TailSlot = "tail";

        private Skin _clonedSkin;

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

        private readonly List<string> _attachmentNames = new List<string>();

        protected override void Awake()
        {
            base.Awake();

            _clonedSkin = SkeletonAnimation.skeleton.Data.DefaultSkin.GetClone();

            _weaponSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(WeaponSlot);
            _weaponAttachmentDefault =
                MakeAttachment(SpriteHelper.GetPlayerSpineTextureWeapon(GameConfig.DefaultAvatarWeaponId));

            _eyeOpenSlot = SkeletonAnimation.skeleton.FindSlot(EyeOpenSlot);
            if (!(_eyeOpenSlot is null))
            {
                _eyeOpenSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EyeOpenSlot);
                _eyeHalfSlot = SkeletonAnimation.skeleton.FindSlot(EyeHalfSlot);
                _eyeHalfSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EyeHalfSlot);
                _eyeOpenAttachmentDefault =
                    RemapAttachment(_eyeOpenSlot, SpriteHelper.GetPlayerSpineTextureEyeOpen(null));
                _eyeHalfAttachmentDefault =
                    RemapAttachment(_eyeHalfSlot, SpriteHelper.GetPlayerSpineTextureEyeHalf(null));
            }

            _earLeftSlot = SkeletonAnimation.skeleton.FindSlot(EarLeftSlot);
            if (!(_earLeftSlot is null))
            {
                _earLeftSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(EarLeftSlot);
                _earLeftAttachmentDefault =
                    RemapAttachment(_earLeftSlot, SpriteHelper.GetPlayerSpineTextureEarLeft(null));
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

        #region Equipments & Costomize

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

        public void UpdateEar(Sprite spriteLeft, Sprite spriteRight)
        {
            if (_earLeftSlot is null || _earRightSlot is null)
                return;

            _attachmentNames.Clear();
            _clonedSkin.FindNamesForSlot(_earLeftSlotIndex, _attachmentNames);

            var attachmentName = _attachmentNames.Count > 0 ? _attachmentNames[0] : string.Empty;

            if (string.IsNullOrEmpty(attachmentName))
            {
                _clonedSkin.AddAttachment(_earLeftSlotIndex, EarLeftSlot, _earLeftAttachmentDefault);
            }
            else if (spriteLeft is null)
            {
                _clonedSkin.SetAttachment(_earLeftSlotIndex, attachmentName, _earLeftAttachmentDefault);
            }
            else
            {
                var newEarLeft = RemapAttachment(_earLeftSlot, spriteLeft);
                _clonedSkin.SetAttachment(_earLeftSlotIndex, attachmentName, newEarLeft);
            }

            _attachmentNames.Clear();
            _clonedSkin.FindNamesForSlot(_earRightSlotIndex, _attachmentNames);

            attachmentName = _attachmentNames.Count > 0 ? _attachmentNames[0] : string.Empty;

            if (string.IsNullOrEmpty(attachmentName))
            {
                _clonedSkin.AddAttachment(_earRightSlotIndex, EarRightSlot, _earRightAttachmentDefault);
            }
            else if (spriteRight is null)
            {
                _clonedSkin.SetAttachment(_earRightSlotIndex, attachmentName, _earRightAttachmentDefault);
            }
            else
            {
                var newEarRight = RemapAttachment(_earRightSlot, spriteRight);
                _clonedSkin.SetAttachment(_earRightSlotIndex, attachmentName, newEarRight);
            }

            UpdateInternal();
        }

        public void UpdateEye(Sprite spriteEyeOpen, Sprite spriteEyeHalf)
        {
            if (_eyeOpenSlot is null || _eyeHalfSlot is null)
                return;

            _attachmentNames.Clear();
            _clonedSkin.FindNamesForSlot(_eyeOpenSlotIndex, _attachmentNames);

            var attachmentName = _attachmentNames.Count > 0 ? _attachmentNames[0] : string.Empty;

            if (string.IsNullOrEmpty(attachmentName))
            {
                _clonedSkin.AddAttachment(_eyeOpenSlotIndex, EyeOpenSlot, _earRightAttachmentDefault);
            }
            else if (spriteEyeOpen is null)
            {
                _clonedSkin.SetAttachment(_eyeOpenSlotIndex, attachmentName, _eyeOpenAttachmentDefault);
            }
            else
            {
                var newEyeOpen = RemapAttachment(_eyeOpenSlot, spriteEyeOpen);
                _clonedSkin.SetAttachment(_eyeOpenSlotIndex, attachmentName, newEyeOpen);
            }

            _attachmentNames.Clear();
            _clonedSkin.FindNamesForSlot(_eyeHalfSlotIndex, _attachmentNames);

            attachmentName = _attachmentNames.Count > 0 ? _attachmentNames[0] : string.Empty;

            if (string.IsNullOrEmpty(attachmentName))
            {
                _clonedSkin.AddAttachment(_eyeHalfSlotIndex, EyeHalfSlot, _eyeHalfAttachmentDefault);
            }
            else if (spriteEyeHalf is null)
            {
                _clonedSkin.SetAttachment(_eyeHalfSlotIndex, attachmentName, _eyeHalfAttachmentDefault);
            }
            else
            {
                var newEyeHalf = RemapAttachment(_eyeHalfSlot, spriteEyeHalf);
                _clonedSkin.SetAttachment(_eyeHalfSlotIndex, attachmentName, newEyeHalf);
            }

            UpdateInternal();
        }

        public void UpdateTail(Sprite sprite)
        {
            if (_tailSlot is null)
                return;

            _attachmentNames.Clear();
            _clonedSkin.FindNamesForSlot(_tailSlotIndex, _attachmentNames);

            var attachmentName = _attachmentNames.Count > 0 ? _attachmentNames[0] : string.Empty;

            if (string.IsNullOrEmpty(attachmentName))
            {
                _clonedSkin.AddAttachment(_tailSlotIndex, TailSlot, _tailAttachmentDefault);
            }
            else if (sprite is null)
            {
                _clonedSkin.SetAttachment(_tailSlotIndex, attachmentName, _tailAttachmentDefault);
            }
            else
            {
                var newTail = RemapAttachment(_tailSlot, sprite);
                _clonedSkin.SetAttachment(_tailSlotIndex, attachmentName, newTail);
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

        #endregion
    }
}
