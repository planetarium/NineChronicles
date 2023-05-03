using Bencodex.Types;
using Nekoyume.Model.Skill.Arena;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(SkillSheet.Row skillRow, int power, int chance)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new NormalAttack(skillRow, power, chance);
                        case SkillCategory.DoubleAttack:
                            return new DoubleAttack(skillRow, power, chance);
                        case SkillCategory.BlowAttack:
                            return new BlowAttack(skillRow, power, chance);
                        case SkillCategory.AreaAttack:
                            return new AreaAttack(skillRow, power, chance);
                        case SkillCategory.BuffRemovalAttack:
                            return new BuffRemovalAttack(skillRow, power, chance);
                        default:
                            return new NormalAttack(skillRow, power, chance);
                    }
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        // Convert skill to arena skill
        public static ArenaSkill GetForArena(SkillSheet.Row skillRow, int power, int chance)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new ArenaNormalAttack(skillRow, power, chance);
                        case SkillCategory.DoubleAttack:
                            return new ArenaDoubleAttack(skillRow, power, chance);
                        case SkillCategory.BlowAttack:
                            return new ArenaBlowAttack(skillRow, power, chance);
                        case SkillCategory.AreaAttack:
                            return new ArenaAreaAttack(skillRow, power, chance);
                        case SkillCategory.BuffRemovalAttack:
                            return new ArenaBuffRemovalAttack(skillRow, power, chance);
                        default:
                            return new ArenaNormalAttack(skillRow, power, chance);
                    }
                case SkillType.Heal:
                    return new ArenaHealSkill(skillRow, power, chance);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new ArenaBuffSkill(skillRow, power, chance);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        public static Skill Deserialize(Dictionary serialized) =>
            Get(
                SkillSheet.Row.Deserialize((Dictionary) serialized["skillRow"]),
                serialized["power"].ToInteger(),
                serialized["chance"].ToInteger()
            );
    }
}
