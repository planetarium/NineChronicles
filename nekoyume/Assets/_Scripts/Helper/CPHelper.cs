using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Skill = Nekoyume.Game.Skill;

namespace Nekoyume.Helper
{
    public static class CPHelper
    {
        /// <summary>
        /// `Player`의 CP를 리턴한다.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetCP(Player player)
        {
            return GetCP(player.Stats.LevelStats, StatType.ATK) + player.Equipments.Sum(GetCP);
        }

        public static int GetCP(Enemy enemy)
        {
            var result = (float) GetCP(enemy.Stats.LevelStats, StatType.ATK);
            result = enemy.Skills.Aggregate(result, (current, skill) => current * GetCP(skill));
            return (int) enemy.BuffSkills.Aggregate(result, (current, buffSkill) => current * GetCP(buffSkill));
        }

        /// <summary>
        /// `Equipment`의 CP를 리턴한다.
        /// `ItemUsable`을 받지 않는 이유는 대표 스탯이 없기 때문이다.
        /// 같은 이유로 `Consumable`을 받지 않는다.
        /// `Equipment`에는 `UniqueStatType`을 통해서 대표 속성을 얻을 수 있다.
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        public static int GetCP(Equipment equipment)
        {
            var result = (float) GetCP(equipment.StatsMap, equipment.UniqueStatType, false);
            result = equipment.Skills.Aggregate(result, (current, skill) => current * GetCP(skill));
            return (int) equipment.BuffSkills.Aggregate(result, (current, buffSkill) => current * GetCP(buffSkill));
        }

        /// <summary>
        /// `Stats`의 CP를 리턴한다.
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="uniqueStatType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static int GetCP(IStats stats, StatType uniqueStatType)
        {
            return GetCP(stats.GetStats(true), uniqueStatType);
        }

        /// <summary>
        /// `StatsMap`의 CP를 리턴한다.
        /// </summary>
        /// <param name="statsMap"></param>
        /// <param name="uniqueStatType"></param>
        /// <returns></returns>
        private static int GetCP(StatsMap statsMap, StatType uniqueStatType, bool isCharacter = true)
        {
            return GetCP(statsMap.GetBaseAndAdditionalStats(true), uniqueStatType, isCharacter);
        }

        private static float GetCP(Skill skill)
        {
            switch (skill.skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skill.skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return GameConfig.CPNormalAttackMultiply;
                        case SkillCategory.BlowAttack:
                            switch (skill.skillRow.SkillTargetType)
                            {
                                case SkillTargetType.Enemies:
                                    return GameConfig.CPBlowAllAttackMultiply;
                                default:
                                    return GameConfig.CPBlowAttackMultiply;
                            }
                        case SkillCategory.DoubleAttack:
                            return GameConfig.CPDoubleAttackMultiply;
                        case SkillCategory.AreaAttack:
                            return GameConfig.CPAreaAttackMultiply;
                        default:
                            throw new ArgumentOutOfRangeException(
                                $"{nameof(skill.skillRow.SkillType)}, {nameof(skill.skillRow.SkillCategory)}");
                    }
                case SkillType.Heal:
                    return GameConfig.CPHealMultiply;
                case SkillType.Buff:
                    return GameConfig.CPBuffMultiply;
                case SkillType.Debuff:
                    return GameConfig.CPDebuffMultiply;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int GetCP(
            IEnumerable<(StatType statType, int value)> baseAndAdditionalStats,
            StatType uniqueStatType, bool isCharacter = true)
        {
            var part1 = 0f;
            var part2 = 1f;
            var part3 = 1f;
            foreach (var (statType, value) in baseAndAdditionalStats)
            {
                if (statType == uniqueStatType)
                {
                    switch (statType)
                    {
                        case StatType.CRI:
                        case StatType.DOG:
                        case StatType.SPD:
                            part1 += value / 100f;
                            if (isCharacter)
                                break;

                            part1 += 1f;
                            break;
                        default:
                            part1 += value;
                            break;
                    }

                    continue;
                }

                switch (statType)
                {
                    case StatType.NONE:
                        break;
                    case StatType.HP:
                    case StatType.ATK:
                    case StatType.DEF:
                        part2 += value;
                        break;
                    case StatType.CRI:
                    case StatType.DOG:
                    case StatType.SPD:
                        if (isCharacter)
                        {
                            part3 *= value / 100f;
                        }
                        else
                        {
                            part3 *= 1f + value / 100f;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (int) (part1 * part2 * part3);
        }

        private static int GetCP(
            IEnumerable<(StatType statType, int baseValue, int additionalValue)> baseAndAdditionalStats,
            StatType uniqueStatType, bool isCharacter = true)
        {
            var part1 = 0f;
            var part2 = 1f;
            var part3 = 1f;
            foreach (var (statType, baseValue, additionalValue) in baseAndAdditionalStats)
            {
                if (statType == uniqueStatType)
                {
                    switch (statType)
                    {
                        case StatType.CRI:
                        case StatType.DOG:
                        case StatType.SPD:
                            part1 += baseValue / 100f;
                            part2 += additionalValue / 100f;
                            if (isCharacter)
                                break;

                            part1 += 1f;
                            break;
                        default:
                            part1 += baseValue;
                            part2 += additionalValue;
                            break;
                    }

                    continue;
                }

                switch (statType)
                {
                    case StatType.NONE:
                        break;
                    case StatType.HP:
                    case StatType.ATK:
                    case StatType.DEF:
                        part2 += baseValue + additionalValue;
                        break;
                    case StatType.CRI:
                    case StatType.DOG:
                    case StatType.SPD:
                        if (isCharacter)
                        {
                            part3 *= (baseValue + additionalValue) / 100f;
                        }
                        else
                        {
                            part3 *= 1 + (baseValue + additionalValue) / 100f;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (int) (part1 * part2 * part3);
        }
    }
}
