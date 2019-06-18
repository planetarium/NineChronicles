using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    public class RangedWeapon : Weapon
    {
        public RangedWeapon(Data.Table.Item data, float skillChance = 0f, SkillEffect skillEffect = null,
            Data.Table.Elemental.ElementalType skillElementalType = Nekoyume.Data.Table.Elemental.ElementalType.Normal)
            : base(data, skillChance, skillEffect, skillElementalType)
        {
        }
    }
}
