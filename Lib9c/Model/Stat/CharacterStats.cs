using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// Stat is built with _baseStats based on level,
    /// _equipmentStats based on equipments,
    /// _consumableStats based on consumables,
    /// _buffStats based on buffs
    /// and _optionalStats for runes, etc...
    /// Stat of character is built with total of these stats.
    /// </summary>
    [Serializable]
    public class CharacterStats : Stats, IBaseAndAdditionalStats, ICloneable
    {
        private readonly CharacterSheet.Row _row;

        private readonly Stats _baseStats = new Stats();
        private readonly Stats _equipmentStats = new Stats();
        private readonly Stats _consumableStats = new Stats();
        private readonly Stats _buffStats = new Stats();
        private readonly Stats _optionalStats = new Stats();

        private readonly List<StatModifier> _equipmentStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _consumableStatModifiers = new List<StatModifier>();
        private readonly Dictionary<int, StatModifier> _buffStatModifiers = new Dictionary<int, StatModifier>();
        private readonly List<StatModifier> _optionalStatModifiers = new List<StatModifier>();

        public int Level { get; private set; }

        public IStats BaseStats => _baseStats;
        public IStats EquipmentStats => _equipmentStats;
        public IStats ConsumableStats => _consumableStats;
        public IStats BuffStats => _buffStats;
        public IStats OptionalStats => _optionalStats;

        public decimal BaseHP => BaseStats.HP;
        public decimal BaseATK => BaseStats.ATK;
        public decimal BaseDEF => BaseStats.DEF;
        public decimal BaseCRI => BaseStats.CRI;
        public decimal BaseHIT => BaseStats.HIT;
        public decimal BaseSPD => BaseStats.SPD;
        public decimal BaseDRV => BaseStats.DRV;
        public decimal BaseDRR => BaseStats.DRR;
        public decimal BaseCDMG => BaseStats.CDMG;

        public decimal AdditionalHP => HP - _baseStats.HP;
        public decimal AdditionalATK => ATK - _baseStats.ATK;
        public decimal AdditionalDEF => DEF - _baseStats.DEF;
        public decimal AdditionalCRI => CRI - _baseStats.CRI;
        public decimal AdditionalHIT => HIT - _baseStats.HIT;
        public decimal AdditionalSPD => SPD - _baseStats.SPD;
        public decimal AdditionalDRV => DRV - _baseStats.DRV;
        public decimal AdditionalDRR => DRR - _baseStats.DRR;
        public decimal AdditionalCDMG => CDMG - _baseStats.CDMG;

        public CharacterStats(
            CharacterSheet.Row row,
            int level
        )
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            SetStats(level);
        }

        public CharacterStats(WorldBossCharacterSheet.WaveStatData stat)
        {
            var stats = stat.ToStats();
            _baseStats.Set(stats);
            SetStats(stat.Level);
        }
            
        public CharacterStats(CharacterStats value) : base(value)
        {
            _row = value._row;

            _baseStats = new Stats(value._baseStats);
            _equipmentStats = new Stats(value._equipmentStats);
            _consumableStats = new Stats(value._consumableStats);
            _buffStats = new Stats(value._buffStats);
            _optionalStats = new Stats(value._optionalStats);

            _equipmentStatModifiers = value._equipmentStatModifiers;
            _consumableStatModifiers = value._consumableStatModifiers;
            _buffStatModifiers = value._buffStatModifiers;
            _optionalStatModifiers = value._optionalStatModifiers;

            Level = value.Level;
        }

        public CharacterStats SetAll(
            int level,
            IEnumerable<Equipment> equipments,
            IEnumerable<Costume> costumes,
            IEnumerable<Consumable> consumables,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet)
        {
            SetStats(level, false);
            SetEquipments(equipments, equipmentItemSetEffectSheet, false);
            SetCostumes(costumes, costumeStatSheet);
            SetConsumables(consumables, false);
            UpdateBaseStats();

            return this;
        }

        /// <summary>
        /// Set base stats based on character level.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetStats(int level, bool updateImmediate = true)
        {
            if (level == Level)
                return this;

            Level = level;

            if (updateImmediate)
            {
                UpdateBaseStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on equipments. Also recalculates stats from consumables and buffs.
        /// </summary>
        /// <param name="equipments"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetEquipments(
            IEnumerable<Equipment> equipments,
            EquipmentItemSetEffectSheet sheet,
            bool updateImmediate = true
        )
        {
            _equipmentStatModifiers.Clear();
            if (!(equipments is null))
            {
                foreach (var equipment in equipments)
                {
                    var statMap = equipment.StatsMap;
                    foreach (var (statType, value) in statMap.GetStats(true))
                    {
                        var statModifier = new StatModifier(
                            statType,
                            StatModifier.OperationType.Add,
                            value);
                        _equipmentStatModifiers.Add(statModifier);
                    }
                }

                // set effects.
                var setEffectRows = sheet.GetSetEffectRows(equipments);
                foreach (var statModifier in setEffectRows.SelectMany(row => row.StatModifiers.Values))
                {
                    _equipmentStatModifiers.Add(statModifier);
                }
            }

            if (updateImmediate)
            {
                UpdateEquipmentStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on consumables. Also recalculates stats from buffs.
        /// </summary>
        /// <param name="consumables"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetConsumables(
            IEnumerable<Consumable> consumables,
            bool updateImmediate = true)
        {
            _consumableStatModifiers.Clear();
            if (!(consumables is null))
            {
                foreach (var consumable in consumables)
                {
                    var statMap = consumable.StatsMap;
                    foreach (var (statType, value) in statMap.GetStats(true))
                    {
                        var statModifier = new StatModifier(
                            statType,
                            StatModifier.OperationType.Add,
                            value);
                        _equipmentStatModifiers.Add(statModifier);
                    }
                }
            }

            if (updateImmediate)
            {
                UpdateConsumableStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on buffs.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetBuffs(IEnumerable<Buff.StatBuff> value, bool updateImmediate = true)
        {
            _buffStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var buff in value)
                {
                    AddBuff(buff, false);
                }
            }

            if (updateImmediate)
            {
                UpdateBuffStats();
            }

            return this;
        }

        public void AddBuff(Buff.StatBuff buff, bool updateImmediate = true)
        {
            _buffStatModifiers[buff.RowData.GroupId] = buff.GetModifier();

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void RemoveBuff(Buff.StatBuff buff, bool updateImmediate = true)
        {
            if (!_buffStatModifiers.ContainsKey(buff.RowData.GroupId))
                return;

            _buffStatModifiers.Remove(buff.RowData.GroupId);

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void AddOptional(IEnumerable<StatModifier> statModifiers)
        {
            _optionalStatModifiers.AddRange(statModifiers);
            UpdateOptionalStats();
        }

        private void SetCostumes(IEnumerable<Costume> costumes, CostumeStatSheet costumeStatSheet)
        {
            var statModifiers = new List<StatModifier>();
            foreach (var costume in costumes)
            {
                var stat = costumeStatSheet.OrderedList
                    .Where(r => r.CostumeId == costume.Id)
                    .Select(row => new StatModifier(row.StatType, StatModifier.OperationType.Add,
                        (int)row.Stat));
                statModifiers.AddRange(stat);
            }
            SetOption(statModifiers);
        }

        public void SetOption(IEnumerable<StatModifier> statModifiers)
        {
            _optionalStatModifiers.Clear();
            AddOptional(statModifiers);
        }

        public void IncreaseHpForArena()
        {
            var originalHP = _statMap[StatType.HP];
            _statMap[StatType.HP].SetValue(Math.Max(0, originalHP.BaseValue * 2));
        }

        private void UpdateBaseStats()
        {
            if (_row != null)
            {
                var statsData = _row.ToStats(Level);
                _baseStats.Set(statsData);
            }

            UpdateEquipmentStats();
        }

        private void UpdateEquipmentStats()
        {
            _equipmentStats.Set(_equipmentStatModifiers, _baseStats);
            UpdateConsumableStats();
        }

        private void UpdateConsumableStats()
        {
            _consumableStats.Set(_consumableStatModifiers, _baseStats, _equipmentStats);
            UpdateBuffStats();
        }

        private void UpdateBuffStats()
        {
            _buffStats.Set(_buffStatModifiers.Values, _baseStats, _equipmentStats, _consumableStats);
            UpdateOptionalStats();
        }

        private void UpdateOptionalStats()
        {
            _optionalStats.Set(_optionalStatModifiers, _baseStats, _equipmentStats, _consumableStats, _buffStats);
            UpdateTotalStats();
        }

        private void UpdateTotalStats()
        {
            Set(_baseStats, _equipmentStats, _consumableStats, _buffStats, _optionalStats);

            foreach (var stat in _statMap.GetStats())
            {
                var value = Math.Max(0m, stat.BaseValue);
                stat.SetValue(value);
            }
        }

        public override object Clone()
        {
            return new CharacterStats(this);
        }

        public IEnumerable<(StatType statType, decimal baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            return _baseStats.GetStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, decimal additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            var baseStats = _baseStats.GetStats();
            foreach (var (statType, stat) in baseStats)
            {
                var value = _statMap[statType].BaseValue - stat;
                if (!ignoreZero || value != decimal.Zero)
                {
                    yield return (statType, value);
                }
            }
        }

        public IEnumerable<(StatType statType, decimal baseValue, decimal additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            var additionalStats = GetAdditionalStats();
            foreach (var (statType, additionalStat) in additionalStats)
            {
                var baseStat = _baseStats.GetStat(statType);
                if (!ignoreZero ||
                    (baseStat != decimal.Zero) || (additionalStat != decimal.Zero))
                {
                    yield return (statType, baseStat, additionalStat);
                }
            }
        }
    }
}
