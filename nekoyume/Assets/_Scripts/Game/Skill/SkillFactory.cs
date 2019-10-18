using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Data;
using Nekoyume.EnumType;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    public static class SkillFactory
    {
        public static Skill Get(SkillSheet.Row skillRow, int power, decimal chance)
        {
            if (!Tables.instance.SkillEffect.TryGetValue(skillRow.SkillEffectId, out var skillEffectRow))
            {
                throw new KeyNotFoundException(nameof(skillRow.SkillEffectId));
            }

            switch (skillEffectRow.skillType)
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
                case SkillType.Debuff:
                case SkillType.Buff:
                    switch (skillEffectRow.skillCategory)
                    {
                        case SkillCategory.Heal:
                            return new HealSkill(skillRow, power, chance);
                        case SkillCategory.DefenseBuff:
                        case SkillCategory.CriticalBuff:
                        case SkillCategory.DodgeBuff:
                        case SkillCategory.SpeedBuff:
                        case SkillCategory.AttackBuff:
                            return new BuffSkill(skillRow, power, chance);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillEffectRow.skillType}, {skillEffectRow.skillTargetType}, {skillEffectRow.skillCategory}");
        }

        public static Skill Deserialize(Bencodex.Types.Dictionary serialized) =>
            Get(
                SkillSheet.Row.Deserialize((Bencodex.Types.Dictionary) serialized[(Text) "skillRow"]),
                (int) ((Integer) serialized[(Text) "power"]).Value,
                serialized[(Text) "chance"].ToDecimal()
            );
    }
}
