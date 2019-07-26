using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        private const string SpritePath = "images/icon/equipment/{0}";
        private const int DefaultResourceId = 10210000;

        public Armor(Data.Table.Item data, Guid id, SkillBase skillBase = null)
            : base(data, id, skillBase)
        {
        }

        public static Sprite GetIcon(Armor armor = null)
        {
            var id = armor?.Data.resourceId ?? DefaultResourceId;
            var path = string.Format(SpritePath, id);
            var sprite = Resources.Load<Sprite>(path);
            if (ReferenceEquals(sprite, null))
                throw new AssetNotFoundException(path);
            return sprite;
        }
    }
}
