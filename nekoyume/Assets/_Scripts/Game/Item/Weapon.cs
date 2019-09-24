using System;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        private const string SpritePath = "images/equipment/{0}";

        public static Sprite GetSprite(Equipment equipment)
        {
            return equipment is null
                ? null
                : GetSprite(equipment.Data.resourceId);
        }
        
        public new static Sprite GetSprite(int equipmentId)
        {
            return Resources.Load<Sprite>(string.Format(SpritePath, equipmentId));
        }

        public Weapon(Data.Table.Item data, Guid id)
            : base(data, id)
        {
        }
    }
}
