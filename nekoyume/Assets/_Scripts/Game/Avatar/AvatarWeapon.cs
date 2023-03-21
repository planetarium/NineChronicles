using Nekoyume.Helper;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;

namespace Nekoyume.Game.Avatar
{
    public class AvatarWeapon : MonoBehaviour
    {
        private const string DefaultPmaShader = "Spine/Skeleton";
        private const string WeaponSlot = "weapon";

        [SerializeField]
        private int weaponId = 10100000;

        [SerializeField]
        private SkeletonGraphic skeletonGraphic;

        private void Awake()
        {
            var shader = Shader.Find(DefaultPmaShader);
            var weaponSlotIndex = skeletonGraphic.Skeleton.FindSlotIndex(WeaponSlot);
            var weaponSprite = SpriteHelper.GetPlayerSpineTextureWeapon(weaponId);
            var newWeapon = weaponSprite.ToRegionAttachmentPMAClone(shader);
            skeletonGraphic.Skeleton.Data.DefaultSkin
                .SetAttachment(weaponSlotIndex, WeaponSlot, newWeapon);
            skeletonGraphic.Skeleton.SetSlotsToSetupPose();
        }
    }
}
