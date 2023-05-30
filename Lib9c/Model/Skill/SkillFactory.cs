using Bencodex.Types;
using Nekoyume.Model.Skill.Arena;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using System;

namespace Nekoyume.Model.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(
            SkillSheet.Row skillRow,
            int power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new NormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.DoubleAttack:
                            return new DoubleAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BlowAttack:
                            return new BlowAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.AreaAttack:
                            return new AreaAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BuffRemovalAttack:
                            return new BuffRemovalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        default:
                            return new NormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                    }
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        [Obsolete("Use Get() instead.")]
        public static Skill GetV1(
            SkillSheet.Row skillRow,
            int power,
            int chance)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new NormalAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.DoubleAttack:
                            return new DoubleAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.BlowAttack:
                            return new BlowAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.AreaAttack:
                            return new AreaAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.BuffRemovalAttack:
                            return new BuffRemovalAttack(skillRow, power, chance, default, StatType.NONE);
                        default:
                            return new NormalAttack(skillRow, power, chance, default, StatType.NONE);
                    }
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance, default, StatType.NONE);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance, default, StatType.NONE);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        // Convert skill to arena skill
        public static ArenaSkill GetForArena(
            SkillSheet.Row skillRow,
            int power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new ArenaNormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.DoubleAttack:
                            return new ArenaDoubleAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BlowAttack:
                            return new ArenaBlowAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.AreaAttack:
                            return new ArenaAreaAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BuffRemovalAttack:
                            return new ArenaBuffRemovalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        default:
                            return new ArenaNormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                    }
                case SkillType.Heal:
                    return new ArenaHealSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new ArenaBuffSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        public static Skill Deserialize(Dictionary serialized)
        {
            var ratio = serialized.TryGetValue((Text)"stat_power_ratio", out var ratioValue) ?
                ratioValue.ToInteger() : default;
            var statType = serialized.TryGetValue((Text)"referenced_stat_type", out var refStatType) ?
                StatTypeExtension.Deserialize((Binary)refStatType) : StatType.NONE;

            return Get(
                SkillSheet.Row.Deserialize((Dictionary)serialized["skillRow"]),
                serialized["power"].ToInteger(),
                serialized["chance"].ToInteger(),
                ratio,
                statType
            );
        }
    }
}
