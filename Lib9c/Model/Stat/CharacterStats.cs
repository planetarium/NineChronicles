using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// 캐릭터의 스탯을 관리한다.
    /// 스탯은 레벨에 의한 _baseStats를 기본으로 하고
    /// > 장비에 의한 _equipmentStats
    /// > 소모품에 의한 _consumableStats
    /// > 버프에 의한 _buffStats
    /// > 옵션에 의한 _optionalStats
    /// 마지막으로 모든 스탯을 합한 CharacterStats 순서로 계산한다.
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


        public int BaseHP => BaseStats.HP;
        public int BaseATK => BaseStats.ATK;
        public int BaseDEF => BaseStats.DEF;
        public int BaseCRI => BaseStats.CRI;
        public int BaseHIT => BaseStats.HIT;
        public int BaseSPD => BaseStats.SPD;
        public int BaseDRV => BaseStats.DRV;
        public int BaseDRR => BaseStats.DRR;
        public int BaseCDMG => BaseStats.CDMG;

        public bool HasBaseHP => BaseStats.HasHP;
        public bool HasBaseATK => BaseStats.HasATK;
        public bool HasBaseDEF => BaseStats.HasDEF;
        public bool HasBaseCRI => BaseStats.HasCRI;
        public bool HasBaseHIT => BaseStats.HasHIT;
        public bool HasBaseSPD => BaseStats.HasSPD;
        public bool HasBaseDRV => BaseStats.HasDRV;
        public bool HasBaseDRR => BaseStats.HasDRR;
        public bool HasBaseCDMG => BaseStats.HasCDMG;

        public int AdditionalHP => HP - _baseStats.HP;
        public int AdditionalATK => ATK - _baseStats.ATK;
        public int AdditionalDEF => DEF - _baseStats.DEF;
        public int AdditionalCRI => CRI - _baseStats.CRI;
        public int AdditionalHIT => HIT - _baseStats.HIT;
        public int AdditionalSPD => SPD - _baseStats.SPD;
        public int AdditionalDRV => DRV - _baseStats.DRV;
        public int AdditionalDRR => DRR - _baseStats.DRR;
        public int AdditionalCDMG => CDMG - _baseStats.CDMG;

        public bool HasAdditionalHP => AdditionalHP > 0;
        public bool HasAdditionalATK => AdditionalATK > 0;
        public bool HasAdditionalDEF => AdditionalDEF > 0;
        public bool HasAdditionalCRI => AdditionalCRI > 0;
        public bool HasAdditionalHIT => AdditionalHIT > 0;
        public bool HasAdditionalSPD => AdditionalSPD > 0;
        public bool HasAdditionalDRV => AdditionalDRV > 0;
        public bool HasAdditionalDRR => AdditionalDRR > 0;
        public bool HasAdditionalCDMG => AdditionalCDMG > 0;

        public bool HasAdditionalStats => HasAdditionalHP || HasAdditionalATK || HasAdditionalDEF || HasAdditionalCRI ||
                                          HasAdditionalHIT || HasAdditionalSPD || HasAdditionalDRV || HasAdditionalDRR ||
                                          HasAdditionalCDMG;

        public CharacterStats(
            CharacterSheet.Row row,
            int level
        )
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            SetStats(level);
            EqualizeCurrentHPWithHP();
        }

        public CharacterStats(WorldBossCharacterSheet.WaveStatData stat)
        {
            var stats = stat.ToStats();
            _baseStats.Set(stats);
            SetStats(stat.Level);
            EqualizeCurrentHPWithHP();
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
            EqualizeCurrentHPWithHP();

            return this;
        }

        /// <summary>
        /// 생성자에서 받은 캐릭터 정보와 레벨을 바탕으로 모든 스탯을 재설정한다.
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
        /// 장비들을 바탕으로 장비 스탯을 재설정한다. 또한 소모품 스탯과 버프 스탯을 다시 계산한다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetEquipments(
            IEnumerable<Equipment> value,
            EquipmentItemSetEffectSheet sheet,
            bool updateImmediate = true
        )
        {
            _equipmentStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var equipment in value)
                {
                    var statMap = equipment.StatsMap;
                    if (statMap.HasHP)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Add,
                            statMap.HP));
                    }

                    if (statMap.HasATK)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Add,
                            statMap.ATK));
                    }

                    if (statMap.HasDEF)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Add,
                            statMap.DEF));
                    }

                    if (statMap.HasCRI)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.CRI, StatModifier.OperationType.Add,
                            statMap.CRI));
                    }

                    if (statMap.HasHIT)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Add,
                            statMap.HIT));
                    }

                    if (statMap.HasSPD)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Add,
                            statMap.SPD));
                    }

                    if (statMap.HasDRV)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.DRV, StatModifier.OperationType.Add,
                            statMap.DRV));
                    }

                    if (statMap.HasDRR)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.DRR, StatModifier.OperationType.Add,
                            statMap.DRR));
                    }

                    if (statMap.HasCDMG)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.CDMG, StatModifier.OperationType.Add,
                            statMap.CDMG));
                    }
                }

                // set effects.
                var setEffectRows = sheet.GetSetEffectRows(value);
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
        /// 소모품들을 바탕으로 소모품 스탯을 재설정한다. 또한 버프 스탯을 다시 계산한다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetConsumables(IEnumerable<Consumable> value, bool updateImmediate = true)
        {
            _consumableStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var consumable in value)
                {
                    var statMap = consumable.StatsMap;
                    if (statMap.HasHP)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Add,
                            statMap.HP));
                    }

                    if (statMap.HasATK)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Add,
                            statMap.ATK));
                    }

                    if (statMap.HasDEF)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Add,
                            statMap.DEF));
                    }

                    if (statMap.HasCRI)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.CRI, StatModifier.OperationType.Add,
                            statMap.CRI));
                    }

                    if (statMap.HasHIT)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Add,
                            statMap.HIT));
                    }

                    if (statMap.HasSPD)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Add,
                            statMap.SPD));
                    }

                    if (statMap.HasDRV)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.DRV, StatModifier.OperationType.Add,
                            statMap.DRV));
                    }

                    if (statMap.HasDRR)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.DRR, StatModifier.OperationType.Add,
                            statMap.DRR));
                    }

                    if (statMap.HasCDMG)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.CDMG, StatModifier.OperationType.Add,
                            statMap.CDMG));
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
        /// 버프들을 바탕으로 버프 스탯을 재설정한다.
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

        public void AddOption(IEnumerable<StatModifier> statModifiers)
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
            AddOption(statModifiers);
        }

        public void IncreaseHpForArena()
        {
            hp.SetValue(Math.Max(0, hp.Value * 2));
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
            // 최소값 보정
            hp.SetValue(Math.Max(0, hp.Value));
            atk.SetValue(Math.Max(0, atk.Value));
            def.SetValue(Math.Max(0, def.Value));
            cri.SetValue(Math.Max(0, cri.Value));
            hit.SetValue(Math.Max(0, hit.Value));
            spd.SetValue(Math.Max(0, spd.Value));
            drv.SetValue(Math.Max(0, drv.Value));
            drr.SetValue(Math.Max(0, drr.Value));
            cdmg.SetValue(Math.Max(0, cdmg.Value));
        }

        public override object Clone()
        {
            return new CharacterStats(this);
        }

        public IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasBaseHP)
                    yield return (StatType.HP, BaseHP);
                if (HasBaseATK)
                    yield return (StatType.ATK, BaseATK);
                if (HasBaseDEF)
                    yield return (StatType.DEF, BaseDEF);
                if (HasBaseCRI)
                    yield return (StatType.CRI, BaseCRI);
                if (HasBaseHIT)
                    yield return (StatType.HIT, BaseHIT);
                if (HasBaseSPD)
                    yield return (StatType.SPD, BaseSPD);
                if (HasBaseDRV)
                    yield return (StatType.DRV, BaseDRV);
                if (HasBaseDRR)
                    yield return (StatType.DRR, BaseDRR);
                if (HasBaseCDMG)
                    yield return (StatType.CDMG, BaseCDMG);
            }
            else
            {
                yield return (StatType.HP, BaseHP);
                yield return (StatType.ATK, BaseATK);
                yield return (StatType.DEF, BaseDEF);
                yield return (StatType.CRI, BaseCRI);
                yield return (StatType.HIT, BaseHIT);
                yield return (StatType.SPD, BaseSPD);
                yield return (StatType.DRV, BaseDRV);
                yield return (StatType.DRR, BaseDRR);
                yield return (StatType.CDMG, BaseCDMG);
            }
        }

        public IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasAdditionalHP)
                    yield return (StatType.HP, AdditionalHP);
                if (HasAdditionalATK)
                    yield return (StatType.ATK, AdditionalATK);
                if (HasAdditionalDEF)
                    yield return (StatType.DEF, AdditionalDEF);
                if (HasAdditionalCRI)
                    yield return (StatType.CRI, AdditionalCRI);
                if (HasAdditionalHIT)
                    yield return (StatType.HIT, AdditionalHIT);
                if (HasAdditionalSPD)
                    yield return (StatType.SPD, AdditionalSPD);
                if (HasAdditionalDRV)
                    yield return (StatType.DRV, AdditionalDRV);
                if (HasAdditionalDRR)
                    yield return (StatType.DRR, AdditionalDRR);
                if (HasAdditionalCDMG)
                    yield return (StatType.CDMG, AdditionalCDMG);
            }
            else
            {
                yield return (StatType.HP, AdditionalHP);
                yield return (StatType.ATK, AdditionalATK);
                yield return (StatType.DEF, AdditionalDEF);
                yield return (StatType.CRI, AdditionalCRI);
                yield return (StatType.HIT, AdditionalHIT);
                yield return (StatType.SPD, AdditionalSPD);
                yield return (StatType.DRV, AdditionalDRV);
                yield return (StatType.DRR, AdditionalDRR);
                yield return (StatType.CDMG, AdditionalCDMG);
            }
        }

        public IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasBaseHP || HasAdditionalHP)
                    yield return (StatType.HP, BaseHP, AdditionalHP);
                if (HasBaseATK || HasAdditionalATK)
                    yield return (StatType.ATK, BaseATK, AdditionalATK);
                if (HasBaseDEF || HasAdditionalDEF)
                    yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                if (HasBaseCRI || HasAdditionalCRI)
                    yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                if (HasBaseHIT || HasAdditionalHIT)
                    yield return (StatType.HIT, BaseHIT, AdditionalHIT);
                if (HasBaseSPD || HasAdditionalSPD)
                    yield return (StatType.SPD, BaseSPD, AdditionalSPD);
                if (HasBaseDRV || HasAdditionalDRV)
                    yield return (StatType.DRV, BaseDRV, AdditionalDRV);
                if (HasBaseDRR || HasAdditionalDRR)
                    yield return (StatType.DRR, BaseDRR, AdditionalDRR);
                if (HasBaseCDMG || HasAdditionalCDMG)
                    yield return (StatType.CDMG, BaseCDMG, AdditionalCDMG);
            }
            else
            {
                yield return (StatType.HP, BaseHP, AdditionalHP);
                yield return (StatType.ATK, BaseATK, AdditionalATK);
                yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                yield return (StatType.HIT, BaseHIT, AdditionalHIT);
                yield return (StatType.SPD, BaseSPD, AdditionalSPD);
                yield return (StatType.DRV, BaseDRV, AdditionalDRV);
                yield return (StatType.DRR, BaseDRR, AdditionalDRR);
                yield return (StatType.CDMG, BaseCDMG, AdditionalCDMG);
            }
        }
    }
}
