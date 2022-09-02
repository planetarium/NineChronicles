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

        public static int GetCPV2(
            AvatarState avatarState,
            CharacterSheet characterSheet,
            CostumeStatSheet costumeStatSheet)
        {
            var current = GetCP(avatarState, characterSheet);
            var costumeCP = avatarState.inventory.Costumes
                .Where(c => c.equipped)
                .Sum(c => GetCP(c, costumeStatSheet));

            return DecimalToInt(current + costumeCP);
        }

        /// <summary>
        /// return `Player` Combat Point.
        /// Calculate Level Stat, Equipment Stat, Equipment SKills, Costume Stat.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="costumeStatSheet"></param>
        /// <returns></returns>
        public static int GetCP(Player player, CostumeStatSheet costumeStatSheet)
        {
            var levelStatsCP = GetStatsCP(player.Stats.BaseStats, player.Level);
            var equipmentsCP = player.Equipments.Sum(GetCP);
            var costumeCP = player.Costumes.Sum(c => GetCP(c, costumeStatSheet));

            return DecimalToInt(levelStatsCP + equipmentsCP + costumeCP);
        }

        /// <summary>
        /// `Enemy`의 CP를 반환합니다.
        /// 레벨 스탯, 별도 설정한 스킬을 고려한다. 그리고 장비는 없는 것으로 간주합니다.
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public static int GetCP(Enemy enemy)
        {
            var levelStatsCP = GetStatsCP(enemy.Stats.BaseStats, enemy.Level);
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

        public static int GetCP(Costume costume, CostumeStatSheet sheet)
        {
            var statsMap = new StatsMap();
            foreach (var r in sheet.OrderedList.Where(r => r.CostumeId == costume.Id))
            {
                statsMap.AddStatValue(r.StatType, r.Stat);
            }

            return DecimalToInt(GetStatsCP(statsMap));
        }

        public static int GetCP(ITradableItem tradableItem, CostumeStatSheet sheet)
        {
            if (tradableItem is ItemUsable itemUsable)
            {
                return GetCP(itemUsable);
            }

            if (tradableItem is Costume costume)
            {
                return GetCP(costume, sheet);
            }

            return 0;
        }

        public static decimal GetStatsCP(IStats stats, int characterLevel = 1)
        {
            var statTuples = stats.GetStats(true);
            return statTuples.Sum(tuple => GetStatCP(tuple.statType, tuple.value, characterLevel));
        }

        public static decimal GetStatCP(StatType statType, decimal statValue, int characterLevel = 1)
        {
            switch (statType)
            {
                case StatType.NONE:
                    return 0m;
                case StatType.HP:
                    return GetCPOfHP(statValue);
                case StatType.ATK:
                    return GetCPOfATK(statValue);
                case StatType.DEF:
                    return GetCPOfDEF(statValue);
                case StatType.CRI:
                    return GetCPOfCRI(statValue, characterLevel);
                case StatType.HIT:
                    return GetCPOfHIT(statValue);
                case StatType.SPD:
                    return GetCPOfSPD(statValue);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static decimal GetCPOfHP(decimal value) => value * 0.7m;

        public static decimal GetCPOfATK(decimal value) => value * 10.5m;

        public static decimal GetCPOfDEF(decimal value) => value * 10.5m;

        public static decimal GetCPOfSPD(decimal value) => value * 3m;

        public static decimal GetCPOfHIT(decimal value) => value * 2.3m;

        public static decimal GetCPOfCRI(decimal value, int characterLevel) =>
            value * characterLevel * 20m;

        public static decimal GetSkillsMultiplier(int skillsCount)
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

        public static int DecimalToInt(decimal value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int) value;
        }
    }
}
