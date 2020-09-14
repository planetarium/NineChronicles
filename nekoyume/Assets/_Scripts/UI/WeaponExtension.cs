using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI
{
    public static class WeaponExtension
    {
        public static Sprite GetPlayerSpineTexture(this Weapon weapon)
        {
            return weapon is null
                ? SpriteHelper.GetPlayerSpineTextureWeapon(GameConfig.DefaultAvatarWeaponId)
                : SpriteHelper.GetPlayerSpineTextureWeapon(weapon.Id);
        }
    }
}
