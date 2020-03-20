using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Skill = Nekoyume.Model.Skill.Skill;

namespace Nekoyume.Battle
{
    public static class CPHelper
    {
        public const decimal CPNormalAttackMultiply = 1m;
        public const decimal CPBlowAttackMultiply = 1.1m;
        public const decimal CPBlowAllAttackMultiply = 1.15m;
        public const decimal CPDoubleAttackMultiply = 1.15m;
        public const decimal CPAreaAttackMultiply = 1.2m;
        public const decimal CPHealMultiply = 1.1m;
        public const decimal CPBuffMultiply = 1.1m;
        public const decimal CPDebuffMultiply = 1.1m;

        /// <summary>
        /// `AvatarState`의 CP를 리턴한다.
        /// </summary>
        /// <param name="avatarState"></param>
        /// <returns></returns>
        public static int GetCP(AvatarState avatarState)
        {
            // todo: 구현!
            return 100;
        }

        /// <summary>
        /// `Player`의 CP를 리턴한다.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetCP(Player player)
        {
            return GetCharacterCP(player.Stats.LevelStats.GetStats(true)) +
                   player.Equipments.Sum(GetCP);
        }

        public static int GetCP(Enemy enemy)
        {
            var result = (decimal) GetCharacterCP(enemy.Stats.LevelStats.GetStats(true));
            result = enemy.Skills.Aggregate(result, (current, skill) => current * GetCP(skill));
            return (int) enemy.BuffSkills.Aggregate(result,
                (current, skill) => current * GetCP(skill));
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
            var result = (decimal) GetCP(equipment.StatsMap, equipment.UniqueStatType);
            result = equipment.Skills.Aggregate(result, (current, skill) => current * GetCP(skill));
            return (int) equipment.BuffSkills.Aggregate(result,
                (current, buffSkill) => current * GetCP(buffSkill));
        }

        /// <summary>
        /// `StatsMap`의 CP를 리턴한다.
        /// </summary>
        /// <param name="statsMap"></param>
        /// <param name="uniqueStatType"></param>
        /// <returns></returns>
        private static int GetCP(StatsMap statsMap, StatType uniqueStatType)
        {
            return GetEquipmentCP(statsMap.GetBaseAndAdditionalStats(true), uniqueStatType);
        }

        private static decimal GetCP(Skill skill)
        {
            switch (skill.SkillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skill.SkillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return CPNormalAttackMultiply;
                        case SkillCategory.BlowAttack:
                            switch (skill.SkillRow.SkillTargetType)
                            {
                                case SkillTargetType.Enemies:
                                    return CPBlowAllAttackMultiply;
                                default:
                                    return CPBlowAttackMultiply;
                            }
                        case SkillCategory.DoubleAttack:
                            return CPDoubleAttackMultiply;
                        case SkillCategory.AreaAttack:
                            return CPAreaAttackMultiply;
                        default:
                            throw new ArgumentOutOfRangeException(
                                $"{nameof(skill.SkillRow.SkillType)}, {nameof(skill.SkillRow.SkillCategory)}");
                    }
                case SkillType.Heal:
                    return CPHealMultiply;
                case SkillType.Buff:
                    return CPBuffMultiply;
                case SkillType.Debuff:
                    return CPDebuffMultiply;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int GetCharacterCP(
            IEnumerable<(StatType statType, int value)> statTuples)
        {
            var part1 = 0m;
            var part2 = 1m;
            var part3 = 1m;
            foreach (var (statType, value) in statTuples)
            {
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
                    case StatType.HIT:
                    case StatType.SPD:
                        part3 *= value / 100m;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (int) (part1 * part2 * part3);
        }

        private static int GetEquipmentCP(
            IEnumerable<(StatType statType, int baseValue, int additionalValue)> statTuples,
            StatType uniqueStatType)
        {
            var part1 = 0m;
            var part2 = 1m;
            var part3 = 1m;
            foreach (var (statType, baseValue, additionalValue) in statTuples)
            {
                if (statType == uniqueStatType)
                {
                    switch (statType)
                    {
                        case StatType.CRI:
                        case StatType.HIT:
                        case StatType.SPD:
                            part1 += baseValue / 100m;
                            part2 += additionalValue / 100m;
                            part1 += 1m;
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
                    case StatType.HIT:
                    case StatType.SPD:
                        part3 *= 1 + (baseValue + additionalValue) / 100m;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (int) (part1 * part2 * part3);
        }

        // private static int GetCP(StatType statType, int statValue)
        // {
        //     switch (statType)
        //     {
        //         case StatType.NONE:
        //             break;
        //         case StatType.HP:
        //             return statValue;
        //         case StatType.ATK:
        //         case StatType.DEF:
        //             return (int) (statValue * 0.1f);
        //         case StatType.CRI:
        //         case StatType.HIT:
        //             return (int) (1 + statValue * 0.05f);
        //         case StatType.SPD:
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
        //     }
        // }
    }
}
