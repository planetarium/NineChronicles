using System.Collections.Generic;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(SkillSheet.Row skillRow, int power, decimal chance)
        {
            if (!Tables.instance.SkillEffect.TryGetValue(skillRow.SkillEffectId, out var skillEffectRow))
            {
                throw new KeyNotFoundException(nameof(skillRow.SkillEffectId));
            }
            
            switch (skillEffectRow.type)
            {
                case SkillType.Attack:
                    switch (skillEffectRow.skillTargetType)
                    {
                        case SkillTargetType.Enemy:
                            switch (skillEffectRow.skillCategory)
                            {
                                case SkillCategory.Normal:
                                    return new NormalAttack(skillRow, power, chance);
                                case SkillCategory.Double:
                                    return new DoubleAttack(skillRow, power, chance);
                                case SkillCategory.Blow:
                                    return new BlowAttack(skillRow, power, chance);
                                default:
                                    return new NormalAttack(skillRow, power, chance);
                            }
                        case SkillTargetType.Enemies:
                            return new AreaAttack(skillRow, power, chance);
                    }
                    break;
                case SkillType.Buff:
                    switch (skillEffectRow.skillTargetType)
                    {
                        case SkillTargetType.Self:
                            return new Heal(skillRow, power, chance);
                    }
                    break;
                case SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
