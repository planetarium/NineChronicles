using System;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    public class RangedWeapon : Weapon
    {
        public RangedWeapon(Data.Table.Item data, Guid id, SkillBase skillBase = null)
            : base(data, id, skillBase)
        {
        }
    }
}
