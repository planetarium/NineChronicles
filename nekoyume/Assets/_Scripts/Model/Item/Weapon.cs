using System;
using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        public Weapon(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }

    public static class WeaponExtensions
    {
        public static Sprite GetPlayerSpineTexture(this Weapon weapon)
        {
            return weapon is null
                ? SpriteHelper.GetPlayerSpineTextureWeapon(GameConfig.DefaultAvatarWeaponId)
                : SpriteHelper.GetPlayerSpineTextureWeapon(weapon.Data.Id);
        }
    }
}
