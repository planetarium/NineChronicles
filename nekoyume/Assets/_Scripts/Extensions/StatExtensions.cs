using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class StatExtensions
    {
        public static string DecimalStatToString(this DecimalStat stat)
        {
            var isPermyriadPercent =
                stat.StatType == StatType.DRR ||
                stat.StatType == StatType.CDMG;
            var value = isPermyriadPercent ? stat.BaseValue / 100m : stat.BaseValue;
            var suffix = isPermyriadPercent ? "%" : string.Empty;

            return $"{stat.StatType} +{value:0.##}{suffix}";
        }

        /// <param name="statModifier"> StatModifier contains StatType, Operation, Value
        /// <br/> ex1. SPD, Add, 314
        /// <br/> ex2. SPD, Percentage, 314
        /// <br/> ex3. SPD, Percentage, -50
        /// </param>
        /// <returns> Formatted string of StatModifier
        /// <br/> ex1. "SPD +3.14"
        /// <br/> ex2. "SPD +314%"
        /// <br/> ex3. "SPD -50%"
        /// </returns>
        public static string StatModifierToString(this StatModifier statModifier)
        {
            string value;
            if (statModifier.Operation == StatModifier.OperationType.Percentage)
            {
                // Percentage인 경우: 값이 음수면 - 기호가 자동으로 붙고, 양수면 + 기호를 붙임
                value = statModifier.Value >= 0
                    ? $"+{statModifier.Value:0.#\\%}"
                    : $"{statModifier.Value:0.#\\%}";
            }
            else
            {
                // Add인 경우: ValueToString에 isSigned: true를 전달하여 자동으로 + 또는 - 기호가 붙도록 함
                value = statModifier.StatType.ValueToString(statModifier.Value, isSigned: true);
            }

            return $"{statModifier.StatType} {value}";
        }

        public static string OptionRowToString(
            this EquipmentItemOptionSheet.Row optionRow, decimal ratio, bool showRatio = true)
        {
            var statMin = ValueToString(optionRow.StatType, optionRow.StatMin);
            var statMax = ValueToString(optionRow.StatType, optionRow.StatMax);

            var description = $"{optionRow.StatType} {statMin}~{statMax}";
            if (showRatio && ratio < 100)
            {
                description += $" ({ratio:0%})";
            }

            return description;
        }

        public static string ValueToString(this StatType statType, decimal value, bool isSigned = false)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.DRV:
                case StatType.ArmorPenetration:
                case StatType.Thorn:
                case StatType.SPD:
                    return isSigned
                        ? value.ToString("+0.##;-0.##")
                        : value.ToString();
                case StatType.CRI:
                    return isSigned
                        ? value.ToString("+0.##\\%;-0.##\\%")
                        : $"{value:0.##\\%}";
                case StatType.DRR:
                case StatType.CDMG:
                    return isSigned
                        ? (value / 100m).ToString("+0.##\\%;-0.##\\%", CultureInfo.InvariantCulture)
                        : (value / 100m).ToString("0.##\\%", CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static string ValueToShortString(this StatType statType, long value)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.DRV:
                case StatType.ArmorPenetration:
                case StatType.Thorn:
                case StatType.SPD:
                    return value.ToCurrencyNotation();
                case StatType.CRI:
                    return $"{value:0.##\\%}";
                case StatType.DRR:
                case StatType.CDMG:
                    return $"{((long)(value / 100m)).ToCurrencyNotation()}%";
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static string GetAcronym(this StatType type)
        {
            switch (type)
            {
                case StatType.NONE:
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.CRI:
                case StatType.HIT:
                case StatType.SPD:
                case StatType.DRV:
                case StatType.DRR:
                case StatType.CDMG:
                    return type.ToString();
                case StatType.ArmorPenetration:
                    return "A.PEN";
                case StatType.Thorn:
                    return "Thorn";
                default:
                    return "NONE";
            }
        }

        public static void SetAll(
            this CharacterStats stats,
            int level,
            IReadOnlyCollection<Equipment> equipments,
            IReadOnlyCollection<Costume> costumes,
            IReadOnlyCollection<Consumable> consumables,
            IReadOnlyCollection<StatModifier> runeStats,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet,
            IEnumerable<StatModifier> collectionStatModifiers)
        {
            stats.SetStats(level);
            stats.SetEquipments(equipments, equipmentItemSetEffectSheet);
            stats.SetCostumeStat(costumes, costumeStatSheet);
            stats.SetConsumables(consumables);
            stats.SetRunes(runeStats);
            stats.SetCollections(collectionStatModifiers);
        }

        public static List<StatModifier> GetEffects(
            this CollectionState collectionState,
            CollectionSheet collectionSheet)
        {
            var result = new List<StatModifier>();
            foreach (var id in collectionState.Ids)
            {
                if (collectionSheet.TryGetValue(id, out var row))
                {
                    result.AddRange(row.StatModifiers);
                }
            }

            return result;
        }
    }
}
