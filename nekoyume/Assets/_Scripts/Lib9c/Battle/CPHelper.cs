using System;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Battle
{
    public static class CPHelper
    {
        #region Public getter

        /// <summary>
        /// `AvatarState`의 CP를 반환한다.
        /// 레벨 스탯, 그리고 장착한 장비의 스탯과 스킬을 고려합니다.
        /// </summary>
        /// <param name="avatarState"></param>
        /// <param name="characterSheet"></param>
        /// <returns></returns>
        public static int GetCP(AvatarState avatarState, CharacterSheet characterSheet)
        {
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var levelStats = row.ToStats(avatarState.level);
            var levelStatsCP = GetStatsCP(levelStats, avatarState.level);
            var equipmentsCP = avatarState.inventory.Items
                .Select(item => item.item)
                .OfType<Equipment>()
                .Where(equipment => equipment.equipped)
                .Sum(GetCP);

            return DecimalToInt(levelStatsCP + equipmentsCP);
        }

        /// <summary>
        /// `Player`의 CP를 반환합니다.
        /// 레벨 스탯, 그리고 장착한 장비의 스탯과 스킬을 고려합니다.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetCP(Player player)
        {
            var levelStatsCP = GetStatsCP(player.Stats.LevelStats, player.Level);
            var equipmentsCP = player.Equipments.Sum(GetCP);

            return DecimalToInt(levelStatsCP + equipmentsCP);
        }

        /// <summary>
        /// `Enemy`의 CP를 반환합니다.
        /// 레벨 스탯, 별도 설정한 스킬을 고려한다. 그리고 장비는 없는 것으로 간주합니다.
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static int GetCP(Enemy enemy)
        {
            var levelStatsCP = GetStatsCP(enemy.Stats.LevelStats, enemy.Level);
            var skills = enemy.Skills.Concat(enemy.BuffSkills).ToArray();
            return DecimalToInt(levelStatsCP * GetSkillsMultiplier(skills.Length));
        }

        /// <summary>
        /// `ItemUsable`의 CP를 반환합니다.
        /// </summary>
        /// <param name="itemUsable"></param>
        /// <returns></returns>
        public static int GetCP(ItemUsable itemUsable)
        {
            var statsCP = GetStatsCP(itemUsable.StatsMap);
            var skills = itemUsable.Skills.Concat(itemUsable.BuffSkills).ToArray();
            return DecimalToInt(statsCP * GetSkillsMultiplier(skills.Length));
        }

        #endregion

        #region Private getter

        private static decimal GetStatsCP(IStats stats, int characterLevel = 1)
        {
            var statTuples = stats.GetStats(true);
            return statTuples.Sum(tuple =>
            {
                var (statType, value) = tuple;
                switch (statType)
                {
                    case StatType.NONE:
                        return 0m;
                    case StatType.HP:
                        return GetCPOfHP(value);
                    case StatType.ATK:
                        return GetCPOfATK(value);
                    case StatType.DEF:
                        return GetCPOfDEF(value);
                    case StatType.CRI:
                        return GetCPOfCRI(value, characterLevel);
                    case StatType.HIT:
                        return GetCPOfHIT(value);
                    case StatType.SPD:
                        return GetCPOfSPD(value);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private static decimal GetCPOfHP(decimal value) => value * 0.7m;

        private static decimal GetCPOfATK(decimal value) => value * 10.5m;

        private static decimal GetCPOfDEF(decimal value) => value * 10.5m;

        private static decimal GetCPOfSPD(decimal value) => value * 3m;

        private static decimal GetCPOfHIT(decimal value) => value * 2.3m;

        private static decimal GetCPOfCRI(decimal value, int characterLevel) =>
            value * characterLevel * 20m;

        private static decimal GetSkillsMultiplier(int skillsCount)
        {
            switch (skillsCount)
            {
                case 0:
                    return 1m;
                case 1:
                    return 1.15m;
                default:
                    return 1.35m;
            }
        }

        #endregion

        private static int DecimalToInt(decimal value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int) value;
        }
    }
}
