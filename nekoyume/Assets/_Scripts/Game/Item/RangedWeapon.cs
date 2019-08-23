using System;

namespace Nekoyume.Game.Item
{
    public class RangedWeapon : Weapon
    {
        public RangedWeapon(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }
    }
}
