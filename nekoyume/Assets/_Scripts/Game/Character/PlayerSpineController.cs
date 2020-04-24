using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Spine;
using Spine.Unity.Modules.AttachmentTools;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PlayerSpineController : CharacterSpineController
    {
        private class SlotAndAttachment
        {
            public string Name { get; }
            public Slot Slot { get; }
            public int SlotIndex { get; }
            public Attachment Attachment { get; }

            public SlotAndAttachment(string name, Slot slot, int slotIndex, Attachment attachment)
            {
                Name = name;
                Slot = slot;
                SlotIndex = slotIndex;
                Attachment = attachment;
            }
        }

        private const string WeaponSlot = "weapon";
        private const string EarLeftSlot = "ear_L";
        private const string EarRightSlot = "ear_R";
        private const string EyeOpenSlot = "eye_01";
        private const string EyeHalfSlot = "eye_02";

        private static readonly string[] HairType0Slots =
        {
            "hair_01",
            "hair_02",
            "hair_03",
            "hair_04",
            "hair_05",
            "hair_06",
        };

        private static readonly string[] HairType1Slots =
        {
            "hair_01",
            "hair_02",
            "hair_03",
            "hair_04",
            "hair_05",
            "hair_06",
            "hair_07",
            "hair_08",
        };

        private const string TailSlot = "tail";

        public int hairTypeIndex = 0;

        private Skin _clonedSkin;

        private SlotAndAttachment _earLeft;
        private SlotAndAttachment _earRight;
        private SlotAndAttachment _eyeOpen;
        private SlotAndAttachment _eyeHalf;
        private readonly List<SlotAndAttachment> _hairs = new List<SlotAndAttachment>();
        private SlotAndAttachment _tail;

        private int _weaponSlotIndex;
        private RegionAttachment _weaponAttachmentDefault;

        private readonly List<string> _attachmentNames = new List<string>();

        protected override void Awake()
        {
            base.Awake();

            _clonedSkin = SkeletonAnimation.skeleton.Data.DefaultSkin.GetClone();

            _weaponSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(WeaponSlot);
            var weaponSprite =
                SpriteHelper.GetPlayerSpineTextureWeapon(GameConfig.DefaultAvatarWeaponId);
            _weaponAttachmentDefault = MakeAttachment(weaponSprite);

            TryGetSlotAndAttachment(
                EarLeftSlot,
                SpriteHelper.GetPlayerSpineTextureEarLeft,
                out _earLeft);

            TryGetSlotAndAttachment(
                EarRightSlot,
                SpriteHelper.GetPlayerSpineTextureEarRight,
                out _earRight);

            TryGetSlotAndAttachment(
                EyeHalfSlot,
                SpriteHelper.GetPlayerSpineTextureEyeHalf,
                out _eyeHalf);

            TryGetSlotAndAttachment(
                EyeOpenSlot,
                SpriteHelper.GetPlayerSpineTextureEyeOpen,
                out _eyeOpen);

            var hairSlots = hairTypeIndex == 0
                ? HairType0Slots
                : HairType1Slots;
            foreach (var hairSlot in hairSlots)
            {
                if (!TryGetSlotAndAttachment(hairSlot, null, out var result))
                {
                    break;
                }

                _hairs.Add(result);
            }

            TryGetSlotAndAttachment(
                TailSlot,
                SpriteHelper.GetPlayerSpineTextureTail,
                out _tail);
        }

        private bool TryGetSlotAndAttachment(
            string slotName,
            Func<string, Sprite> spriteGetter,
            out SlotAndAttachment slotAndAttachment)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                Debug.LogWarning($"Argument Null Or Empty: {nameof(slotName)}");
                slotAndAttachment = null;
                return false;
            }

            var slot = SkeletonAnimation.skeleton.FindSlot(slotName);
            if (slot is null)
            {
                Debug.LogWarning($"Not Found Slot: {slotName}");
                slotAndAttachment = null;
                return false;
            }

            var slotIndex = SkeletonAnimation.skeleton.FindSlotIndex(slotName);
            var attachment = spriteGetter is null
                ? slot.Attachment
                : RemapAttachment(slot, spriteGetter(null));
            slotAndAttachment = new SlotAndAttachment(slotName, slot, slotIndex, attachment);
            return true;
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
            if (_earLeft is null ||
                _earRight is null)
            {
                return;
            }

            SetSprite(_earLeft, spriteLeft);
            SetSprite(_earRight, spriteRight);
            UpdateInternal();
        }

        public void UpdateEye(Sprite spriteEyeHalf, Sprite spriteEyeOpen)
        {
            if (_eyeHalf is null ||
                _eyeOpen is null)
            {
                return;
            }

            SetSprite(_eyeHalf, spriteEyeHalf);
            SetSprite(_eyeOpen, spriteEyeOpen);
            UpdateInternal();
        }

        public void UpdateHair(IReadOnlyList<Sprite> sprites)
        {
            if (_hairs is null)
            {
                return;
            }

            for (var i = 0; i < _hairs.Count; i++)
            {
                if (sprites.Count <= i)
                {
                    break;
                }

                SetSprite(_hairs[i], sprites[i]);
            }

            UpdateInternal();
        }

        public void UpdateTail(Sprite sprite)
        {
            if (_tail is null)
            {
                return;
            }

            SetSprite(_tail, sprite);
            UpdateInternal();
        }

        private void SetSprite(SlotAndAttachment slot, Sprite sprite)
        {
            _attachmentNames.Clear();
            _clonedSkin.FindNamesForSlot(slot.SlotIndex, _attachmentNames);

            var attachmentName = _attachmentNames.Count > 0
                ? _attachmentNames[0]
                : string.Empty;

            if (string.IsNullOrEmpty(attachmentName))
            {
                _clonedSkin.AddAttachment(
                    slot.SlotIndex,
                    slot.Name,
                    slot.Attachment);
            }
            else if (sprite is null)
            {
                _clonedSkin.SetAttachment(
                    slot.SlotIndex,
                    attachmentName,
                    slot.Attachment);
            }
            else
            {
                var attachment = RemapAttachment(slot.Slot, sprite);
                _clonedSkin.SetAttachment(slot.SlotIndex, attachmentName, attachment);
            }
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
