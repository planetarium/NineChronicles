using System;
using System.Collections.Generic;
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
        public static int TotalCP(
            IEnumerable<Equipment> equipments,
            IEnumerable<Costume> costumes,
            IEnumerable<RuneOptionSheet.Row.RuneOptionInfo> runeOptions,
            int level,
            CharacterSheet.Row row,
            CostumeStatSheet costumeStatSheet)
        {
            var levelStatsCp = GetStatsCP(row.ToStats(level), level);
            var equipmentsCp = equipments.Sum(GetCP);
            var costumeCp = costumes.Sum(c => GetCP(c, costumeStatSheet));
            var runeCp = runeOptions.Sum(x => x.Cp);
            var totalCp = DecimalToInt(levelStatsCp + equipmentsCp + costumeCp + runeCp);
            return totalCp;
        }

        [Obsolete("Use TotalCP")]
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

        [Obsolete("Use TotalCP")]
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

        public static int GetCP(Player player, CostumeStatSheet costumeStatSheet)
        {
            var levelStatsCP = GetStatsCP(player.Stats.BaseStats, player.Level);
            var equipmentsCP = player.Equipments.Sum(GetCP);
            var costumeCP = player.Costumes.Sum(c => GetCP(c, costumeStatSheet));

            return DecimalToInt(levelStatsCP + equipmentsCP + costumeCP);
        }

        public static int GetCP(Enemy enemy)
        {
            var levelStatsCP = GetStatsCP(enemy.Stats.BaseStats, enemy.Level);
            var skills = enemy.Skills.Concat(enemy.BuffSkills).ToArray();
            return DecimalToInt(levelStatsCP * GetSkillsMultiplier(skills.Length));
        }

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

        [Obsolete("Use GetCp")]
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

        private static decimal GetStatsCP(IStats stats, int characterLevel = 1)
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
                case StatType.DRV:
                    return GetCPOfDRV(statValue);
                case StatType.DRR:
                    return GetCPOfDRR(statValue, characterLevel);
                case StatType.CDMG:
                    return GetCPOfCDMG(statValue, characterLevel);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static decimal GetCPOfHP(decimal value) => value * 0.7m;

        public static decimal GetCPOfATK(decimal value) => value * 10.5m;

        public static decimal GetCPOfDEF(decimal value) => value * 10.5m;

        public static decimal GetCPOfSPD(decimal value) => value * 3m;

        public static decimal GetCPOfHIT(decimal value) => value * 2.3m;

        // NOTE : Temp formula
        public static decimal GetCPOfDRV(decimal value) => value * 10.5m;

        // NOTE : Temp formula
        public static decimal GetCPOfDRR(decimal value, int characterLevel) =>
            value * characterLevel * 20m;

        public static decimal GetCPOfCRI(decimal value, int characterLevel) =>
            value * characterLevel * 20m;

        // NOTE : Temp formula
        public static decimal GetCPOfCDMG(decimal value, int characterLevel) =>
            value * characterLevel * 3m;

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
