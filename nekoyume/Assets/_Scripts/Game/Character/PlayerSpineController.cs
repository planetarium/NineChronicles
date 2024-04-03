using System.Collections.Generic;
using Nekoyume.Helper;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
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

        private int _weaponSlotIndex;
        private RegionAttachment _weaponAttachmentDefault;
        private GameObject _cachedWeaponVFX;
        private GameObject _currentWeaponVFXPrefab;

        private readonly List<string> _attachmentNames = new List<string>();

        public int HairSlotCount => hairTypeIndex == 0
            ? 6
            : 8;

        protected override void Awake()
        {
            base.Awake();

            var defaultSkin = SkeletonAnimation.skeleton.Data.DefaultSkin;
            _clonedSkin = new Skin($"{defaultSkin.Name} Clone");
            _clonedSkin.CopySkin(defaultSkin);

            var weaponSlot = SkeletonAnimation.Skeleton.FindSlot(WeaponSlot);
            _weaponSlotIndex = weaponSlot == null ? -1 : weaponSlot.Data.Index;
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
        }

        private bool TryGetSlotAndAttachment(
            string slotName,
            out SlotAndAttachment slotAndAttachment)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                NcDebug.LogWarning($"Argument Null Or Empty: {nameof(slotName)}");
                slotAndAttachment = null;
                return false;
            }

            var slot = SkeletonAnimation.skeleton.FindSlot(slotName);
            if (slot is null)
            {
                NcDebug.LogWarning($"Not Found Slot: {slotName}");
                slotAndAttachment = null;
                return false;
            }

            var slotIndex  = slot.Data.Index;
            slotAndAttachment = new SlotAndAttachment(slotName, slot, slotIndex, slot.Attachment);
            return true;
        }
    }
}
