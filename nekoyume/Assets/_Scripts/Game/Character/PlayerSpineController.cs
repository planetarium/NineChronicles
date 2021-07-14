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

        public const string WeaponSlot = "weapon";
        public const string EarLeftSlot = "ear_L";
        public const string EarRightSlot = "ear_R";
        public const string EyeOpenSlot = "eye_01";
        public const string EyeHalfSlot = "eye_02";

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

        /// <summary>
        /// 헤어 스타일은 리소스에서 부터 결정되는 것이라서 리소스 자체에 정보를 포함하는 것이 좋다고 생각합니다.
        /// 하지만 기존의 플레이어 스파인에서는 헤어 스타일을 구분할 수 있는 정보를 포함하고 있지 않기 때문에
        /// 스파인 프리팹을 생성할 때 외부에서 그 정보를 결정해주고 직렬화될 수 있도록 합니다.
        /// 또한, hairTypeIndex를 수동으로 입력하기가 너무 번거로워서 SpineEditor에서 주입하고 있는데
        /// 이로 인해서 캡슐화가 깨지고 있습니다.
        /// 이와같은 복잡도를 없애는 가장 이상적인 방법은 스파인 리소스 내부에 헤어 타입 정보를 포함하는 것이라고 생각합니다.
        /// </summary>
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

        public int HairSlotCount => hairTypeIndex == 0
            ? 6
            : 8;

        protected override void Awake()
        {
            base.Awake();

            _clonedSkin = SkeletonAnimation.skeleton.Data.DefaultSkin.GetClone();

            _weaponSlotIndex = SkeletonAnimation.skeleton.FindSlotIndex(WeaponSlot);
            var weaponSprite =
                SpriteHelper.GetPlayerSpineTextureWeapon(GameConfig.DefaultAvatarWeaponId);
            _weaponAttachmentDefault = MakeAttachment(weaponSprite);

            var hairSlots = hairTypeIndex == 0
                ? HairType0Slots
                : HairType1Slots;
            foreach (var hairSlot in hairSlots)
            {
                if (!TryGetSlotAndAttachment(hairSlot, out var result))
                {
                    break;
                }

                _hairs.Add(result);
            }

            TryGetSlotAndAttachment(EarLeftSlot, out _earLeft);
            TryGetSlotAndAttachment(EarRightSlot, out _earRight);
            TryGetSlotAndAttachment(EyeHalfSlot, out _eyeHalf);
            TryGetSlotAndAttachment(EyeOpenSlot, out _eyeOpen);
            TryGetSlotAndAttachment(TailSlot, out _tail);
        }

        private bool TryGetSlotAndAttachment(
            string slotName,
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
            slotAndAttachment = new SlotAndAttachment(slotName, slot, slotIndex, slot.Attachment);
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
            if (sprites is null ||
                _hairs is null)
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
