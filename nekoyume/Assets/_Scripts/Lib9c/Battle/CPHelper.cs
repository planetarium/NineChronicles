using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Skill = Nekoyume.Model.Skill.Skill;

namespace Nekoyume.Battle
{
    // NOTE: 확장 함수로 빼는 것이 어떨까?
    public static class CPHelper
    {
        private static class LevelSettings
        {
            private const int MinLevel = 1;
            private const int MaxLevel = 999;
            private const int LevelRange = MaxLevel - MinLevel;
            private const decimal MinCp = 1m;
            private const decimal MaxCp = 333333m;
            private const decimal CPRange = MaxCp - MinCp;

            public static decimal GetLevelCP(int level)
            {
                return CPRange / LevelRange * (level - MinLevel) + MinCp;
            }
        }

        private static class StatSettings
        {
            public struct Settings
            {
                public readonly decimal minStat;
                public readonly decimal maxStat;
                public readonly decimal statRange;
                public readonly decimal minCp;
                public readonly decimal maxCp;
                public readonly decimal cpRange;

                public Settings(decimal minStat, decimal maxStat, decimal minCp, decimal maxCp)
                {
                    if (minStat > maxStat)
                    {
                        throw new ArgumentException($"{minStat} > ${maxStat}");
                    }

                    if (minCp > maxCp)
                    {
                        throw new ArgumentException($"{minCp} > {maxCp}");
                    }

                    this.minStat = minStat;
                    this.maxStat = maxStat;
                    statRange = this.maxStat - this.minStat;
                    this.minCp = minCp;
                    this.maxCp = maxCp;
                    cpRange = this.maxCp - this.minCp;
                }
            }

            public static readonly Settings HpSettings = new Settings(1m, 99999999m, 1m, 33333333m);
            public static readonly Settings AtkSettings = new Settings(0m, 9999999m, 0m, 33333333m);
            public static readonly Settings DefSettings = new Settings(0m, 9999999m, 0m, 33333333m);
            public static readonly Settings CriSettings = new Settings(0m, 100m, 1m, 1.5m);
            public static readonly Settings HitSettings = new Settings(0m, 100m, 1m, 30m);
            public static readonly Settings SpdSettings = new Settings(0m, 10m, 1m, 10m);

            public static decimal GetStatCP(StatType statType, decimal value)
            {
                if (!TryGetSettings(statType, out var settings))
                {
                    return 0m;
                }

                return settings.cpRange / settings.statRange * (value - settings.minStat) +
                       settings.minCp;
            }

            private static bool TryGetSettings(StatType type, out Settings settings)
            {
                switch (type)
                {
                    case StatType.NONE:
                        settings = default;
                        return false;
                    case StatType.HP:
                        settings = HpSettings;
                        return true;
                    case StatType.ATK:
                        settings = AtkSettings;
                        return true;
                    case StatType.DEF:
                        settings = DefSettings;
                        return true;
                    case StatType.CRI:
                        settings = CriSettings;
                        return true;
                    case StatType.HIT:
                        settings = HitSettings;
                        return true;
                    case StatType.SPD:
                        settings = SpdSettings;
                        return true;
                    default:
                        settings = default;
                        return false;
                }
            }
        }

        private static class StatSynergySettings
        {
            private struct Settings
            {
                public readonly StatType[] SynergyGroup;
                public readonly decimal[] Multiplies;

                public Settings(StatType[] synergyGroup, decimal[] multiplies)
                {
                    SynergyGroup = synergyGroup;
                    Multiplies = multiplies;
                }
            }

            private static readonly Settings OffenseSettings = new Settings(
                new[] {StatType.ATK, StatType.CRI, StatType.HIT, StatType.SPD},
                new[] {1m, 1.2m, 1.5m, 1.8m});

            private static readonly Settings DefenseSettings = new Settings(
                new[] {StatType.HP, StatType.DEF},
                new[] {1m, 1.2m});

            public static decimal GetMultiply(params StatType[] statTypes)
            {
                var result = 1m;
                var offenseSynergyCount = 0;
                var defenseSynergyCount = 0;
                foreach (var statType in statTypes.Distinct())
                {
                    if (OffenseSettings.SynergyGroup.Any(item => item == statType))
                    {
                        offenseSynergyCount++;
                    }

                    if (DefenseSettings.SynergyGroup.Any(item => item == statType))
                    {
                        defenseSynergyCount++;
                    }
                }

                if (offenseSynergyCount > 0)
                {
                    result *= OffenseSettings.Multiplies[offenseSynergyCount - 1];
                }

                if (defenseSynergyCount > 0)
                {
                    result *= DefenseSettings.Multiplies[defenseSynergyCount - 1];
                }

                return result;
            }
        }

        private static class SkillSettings
        {
            private const decimal NormalAttackMultiply = 1m;
            private const decimal BlowAttackMultiply = 1.1m;
            private const decimal BlowAllAttackMultiply = 1.15m;
            private const decimal DoubleAttackMultiply = 1.15m;
            private const decimal AreaAttackMultiply = 1.2m;
            private const decimal HealMultiply = 1.1m;
            private const decimal BuffMultiply = 1.1m;
            private const decimal DebuffMultiply = 1.1m;

            public static decimal GetMultiply(Skill skill)
            {
                switch (skill.SkillRow.SkillType)
                {
                    case SkillType.Attack:
                        switch (skill.SkillRow.SkillCategory)
                        {
                            case SkillCategory.NormalAttack:
                                return NormalAttackMultiply;
                            case SkillCategory.BlowAttack:
                                switch (skill.SkillRow.SkillTargetType)
                                {
                                    case SkillTargetType.Enemies:
                                        return BlowAllAttackMultiply;
                                    default:
                                        return BlowAttackMultiply;
                                }
                            case SkillCategory.DoubleAttack:
                                return DoubleAttackMultiply;
                            case SkillCategory.AreaAttack:
                                return AreaAttackMultiply;
                            default:
                                throw new ArgumentOutOfRangeException(
                                    $"{nameof(skill.SkillRow.SkillType)}, {nameof(skill.SkillRow.SkillCategory)}");
                        }
                    case SkillType.Heal:
                        return HealMultiply;
                    case SkillType.Buff:
                        return BuffMultiply;
                    case SkillType.Debuff:
                        return DebuffMultiply;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static class SkillSynergySettings
        {
            private struct Settings
            {
                public readonly SkillCategory[] SynergyGroup;
                public readonly decimal[] Multiplies;

                public Settings(SkillCategory[] synergyGroup, decimal[] multiplies)
                {
                    SynergyGroup = synergyGroup;
                    Multiplies = multiplies;
                }
            }

            private static readonly Settings HealSettings = new Settings(
                new[] {SkillCategory.Heal, SkillCategory.HPBuff},
                new[] {1m, 1.2m});

            public static decimal GetMultiply(params SkillCategory[] statTypes)
            {
                var healSynergyCount = 0;
                foreach (var statType in statTypes.Distinct())
                {
                    if (HealSettings.SynergyGroup.Any(item => item == statType))
                    {
                        healSynergyCount++;
                    }
                }

                return HealSettings.Multiplies[healSynergyCount];
            }
        }

        private static class StatAndSkillSynergySettings
        {
            private const decimal HpHealMultiply = 1.1m;
            private const decimal HpHpBuffMultiply = 1.1m;
            private const decimal HpDefBuffMultiply = 1.1m;

            private const decimal AtkBlowAttackMultiply = 1.1m;
            private const decimal AtkBlowAllAttackMultiply = 1.15m;
            private const decimal AtkDoubleAttackMultiply = 1.15m;
            private const decimal AtkAreaAttackMultiply = 1.2m;
            private const decimal AtkAtkBuffMultiply = 1.1m;
            private const decimal AtkCriBuffMultiply = 1.1m;
            private const decimal AtkHitBuffMultiply = 1.1m;
            private const decimal AtkSpdBuffMultiply = 1.1m;

            private const decimal DefHealMultiply = 1.1m;
            private const decimal DefHpBuffMultiply = 1.1m;
            private const decimal DefDefBuffMultiply = 1.1m;

            private const decimal CriAtkBuffMultiply = 1.1m;
            private const decimal CriCriBuffMultiply = 1.1m;
            private const decimal CriHitBuffMultiply = 1.1m;
            private const decimal CriSpdBuffMultiply = 1.1m;

            private const decimal HitAtkBuffMultiply = 1.1m;
            private const decimal HitCriBuffMultiply = 1.1m;
            private const decimal HitHitBuffMultiply = 1.1m;
            private const decimal HitSpdBuffMultiply = 1.1m;

            private const decimal SpdAtkBuffMultiply = 1.1m;
            private const decimal SpdCriBuffMultiply = 1.1m;
            private const decimal SpdHitBuffMultiply = 1.1m;
            private const decimal SpdSpdBuffMultiply = 1.1m;

            public static decimal GetMultiply(StatType statType, SkillSheet.Row skillRow)
            {
                switch (statType)
                {
                    case StatType.HP:
                        switch (skillRow.SkillCategory)
                        {
                            case SkillCategory.Heal:
                                return HpHealMultiply;
                            case SkillCategory.HPBuff:
                                return HpHpBuffMultiply;
                            case SkillCategory.DefenseBuff:
                                return HpDefBuffMultiply;
                            default:
                                return 1m;
                        }
                    case StatType.ATK:
                        switch (skillRow.SkillCategory)
                        {
                            case SkillCategory.BlowAttack:
                                return skillRow.SkillTargetType == SkillTargetType.Enemies
                                    ? AtkBlowAllAttackMultiply
                                    : AtkBlowAttackMultiply;
                            case SkillCategory.DoubleAttack:
                                return AtkDoubleAttackMultiply;
                            case SkillCategory.AreaAttack:
                                return AtkAreaAttackMultiply;
                            case SkillCategory.AttackBuff:
                                return AtkAtkBuffMultiply;
                            case SkillCategory.CriticalBuff:
                                return AtkCriBuffMultiply;
                            case SkillCategory.HitBuff:
                                return AtkHitBuffMultiply;
                            case SkillCategory.SpeedBuff:
                                return AtkSpdBuffMultiply;
                            default:
                                return 1m;
                        }
                    case StatType.DEF:
                        switch (skillRow.SkillCategory)
                        {
                            case SkillCategory.Heal:
                                return DefHealMultiply;
                            case SkillCategory.HPBuff:
                                return DefHpBuffMultiply;
                            case SkillCategory.DefenseBuff:
                                return DefDefBuffMultiply;
                            default:
                                return 1m;
                        }
                    case StatType.CRI:
                        switch (skillRow.SkillCategory)
                        {
                            case SkillCategory.AttackBuff:
                                return CriAtkBuffMultiply;
                            case SkillCategory.CriticalBuff:
                                return CriCriBuffMultiply;
                            case SkillCategory.HitBuff:
                                return CriHitBuffMultiply;
                            case SkillCategory.SpeedBuff:
                                return CriSpdBuffMultiply;
                            default:
                                return 1m;
                        }
                    case StatType.HIT:
                        switch (skillRow.SkillCategory)
                        {
                            case SkillCategory.AttackBuff:
                                return HitAtkBuffMultiply;
                            case SkillCategory.CriticalBuff:
                                return HitCriBuffMultiply;
                            case SkillCategory.HitBuff:
                                return HitHitBuffMultiply;
                            case SkillCategory.SpeedBuff:
                                return HitSpdBuffMultiply;
                            default:
                                return 1m;
                        }
                    case StatType.SPD:
                        switch (skillRow.SkillCategory)
                        {
                            case SkillCategory.AttackBuff:
                                return SpdAtkBuffMultiply;
                            case SkillCategory.CriticalBuff:
                                return SpdCriBuffMultiply;
                            case SkillCategory.HitBuff:
                                return SpdHitBuffMultiply;
                            case SkillCategory.SpeedBuff:
                                return SpdSpdBuffMultiply;
                            default:
                                return 1m;
                        }
                    default:
                        return 1m;
                }
            }
        }

        private static class ItemGradeSettings
        {
            public static decimal GetMultiply(int grade)
            {
                return 1m + grade * 0.05m;
            }
        }

        /// <summary>
        /// `AvatarState`의 CP를 반환한다.
        /// 레벨 스탯, 그리고 장착한 장비의 스탯과 스킬을 고려한다.
        /// </summary>
        /// <param name="avatarState"></param>
        /// <param name="characterSheet"></param>
        /// <returns></returns>
        public static int GetCP(AvatarState avatarState, CharacterSheet characterSheet)
        {
            var levelCP = LevelSettings.GetLevelCP(avatarState.level);

            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var levelStats = row.ToStats(avatarState.level);
            var levelStatsCP = GetStatsCP(levelStats);
            var equipmentsCP = avatarState.inventory.Items
                .Select(item => item.item)
                .OfType<Equipment>()
                .Where(equipment => equipment.equipped)
                .Sum(GetCP);

            return (int) (levelCP + levelStatsCP + equipmentsCP);
        }

        /// <summary>
        /// `Player`의 CP를 반환한다.
        /// 레벨 스탯, 그리고 장착한 장비의 스탯과 스킬을 고려한다.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetCP(Player player)
        {
            var levelCP = LevelSettings.GetLevelCP(player.Level);
            var levelStatsCP = GetStatsCP(player.Stats.LevelStats);
            var equipmentsCP = player.Equipments.Sum(GetCP);

            return (int) (levelCP + levelStatsCP + equipmentsCP);
        }

        /// <summary>
        /// `Enemy`의 CP를 반환한다.
        /// 레벨 스탯, 별도 설정한 스킬을 고려한다. 그리고 장비는 없는 것으로 간주한다.
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static int GetCP(Enemy enemy)
        {
            var levelCP = LevelSettings.GetLevelCP(enemy.Level);
            var levelStatsCP = GetStatsCP(enemy.Stats.LevelStats);
            var skills = enemy.Skills.Concat(enemy.BuffSkills).ToArray();
            var skillsMultiply = GetSkillsCPMultiply(skills);

            return (int) ((levelCP + levelStatsCP) * skillsMultiply);
        }

        /// <summary>
        /// `ItemUsable`의 CP를 반환한다.
        /// </summary>
        /// <param name="itemUsable"></param>
        /// <returns></returns>
        public static int GetCP(ItemUsable itemUsable)
        {
            var result = GetStatsCP(itemUsable.StatsMap);
            var statTypes = itemUsable.StatsMap.GetStats(true).Select(tuple => tuple.statType).ToArray();
            result *= StatSynergySettings.GetMultiply(statTypes);

            var skills = itemUsable.Skills.Concat(itemUsable.BuffSkills).ToArray();
            result *= GetSkillsCPMultiply(skills);
            result = statTypes.Aggregate(result, (current1, statType) =>
                skills.Aggregate(current1, (current, skill) => current * StatAndSkillSynergySettings.GetMultiply(statType, skill.SkillRow)));
            result *= ItemGradeSettings.GetMultiply(itemUsable.Data.Grade);
            return (int) result;
        }

        private static decimal GetStatsCP(IStats stats)
        {
            var statTuples = stats.GetStats(true);
            var part1 = 0m;
            var part2 = 1m;
            foreach (var (statType, value) in statTuples)
            {
                switch (statType)
                {
                    case StatType.NONE:
                        break;
                    case StatType.HP:
                    case StatType.ATK:
                    case StatType.DEF:
                        part1 += StatSettings.GetStatCP(statType, value);
                        break;
                    case StatType.CRI:
                    case StatType.HIT:
                    case StatType.SPD:
                        part2 *= StatSettings.GetStatCP(statType, value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (int) (part1 * part2);
        }

        private static decimal GetSkillsCPMultiply(params Skill[] skills)
        {
            var result = 1m;
            var skillCategories = skills.Select(skill => skill.SkillRow.SkillCategory).ToArray();
            result = skills.Aggregate(result, (current, skill) => current * SkillSettings.GetMultiply(skill));
            result *= SkillSynergySettings.GetMultiply(skillCategories);
            return result;
        }
    }
}
