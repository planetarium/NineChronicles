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
        public static Skill Get(SkillSheet.Row skillRow, int power, int chance)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillTargetType)
                    {
                        case SkillTargetType.Enemy:
                            switch (skillRow.SkillCategory)
                            {
                                case SkillCategory.NormalAttack:
                                    return new NormalAttack(skillRow, power, chance);
                                case SkillCategory.DoubleAttack:
                                    return new DoubleAttack(skillRow, power, chance);
                                case SkillCategory.BlowAttack:
                                    return new BlowAttack(skillRow, power, chance);
                                default:
                                    return new NormalAttack(skillRow, power, chance);
                            }
                        case SkillTargetType.Enemies:
                            return new AreaAttack(skillRow, power, chance);
                    }

                    break;
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance);
                // todo: 코드상에서 버프와 디버프를 버프로 함께 구분하고 있는데, 고도화 될 수록 디버프를 구분해주게 될 것으로 보임.
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        public static Skill Deserialize(Bencodex.Types.Dictionary serialized) =>
            Get(
                SkillSheet.Row.Deserialize((Bencodex.Types.Dictionary) serialized["skillRow"]),
                (Integer) serialized["power"],
                (Integer) serialized["chance"]
            );
    }
}
