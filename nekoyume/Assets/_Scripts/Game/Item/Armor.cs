using System;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }

        public static Sprite GetIcon(Armor armor = null)
        {
            var id = armor?.Data.resourceId ?? GameConfig.DefaultAvatarArmorId;
            var path = string.Format(EquipmentPath, id);
            var sprite = Resources.Load<Sprite>(path);
            if (ReferenceEquals(sprite, null))
                throw new AssetNotFoundException(path);
            return sprite;
        }
    }
}
