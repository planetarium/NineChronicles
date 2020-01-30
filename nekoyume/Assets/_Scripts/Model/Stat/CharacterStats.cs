using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// 캐릭터의 스탯을 관리한다.
    /// 스탯은에 레벨에 의한 _levelStats를 기본으로 하고
    /// > 장비에 의한 _equipmentStats
    /// > 소모품에 의한 _consumableStats
    /// > 버프에 의한 _buffStats
    /// 마지막으로 모든 스탯을 합한 CharacterStats 순서로 계산한다.
    /// </summary>
    [Serializable]
    public class CharacterStats : Stats, IBaseAndAdditionalStats, ICloneable
    {
        private readonly CharacterSheet.Row _row;

        private readonly Stats _levelStats = new Stats();
        private readonly Stats _equipmentStats = new Stats();
        private readonly Stats _consumableStats = new Stats();
        private readonly Stats _buffStats = new Stats();

        private readonly List<StatModifier> _equipmentStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _consumableStatModifiers = new List<StatModifier>();
        private readonly Dictionary<int, StatModifier> _buffStatModifiers = new Dictionary<int, StatModifier>();

        public int Level { get; private set; }

        public IStats LevelStats => _levelStats;
        public IStats EquipmentStats => _equipmentStats;
        public IStats ConsumableStats => _consumableStats;
        public IStats BuffStats => _buffStats;

        public int BaseHP => LevelStats.HP;
        public int BaseATK => LevelStats.ATK;
        public int BaseDEF => LevelStats.DEF;
        public int BaseCRI => LevelStats.CRI;
        public int BaseDOG => LevelStats.DOG;
        public int BaseSPD => LevelStats.SPD;

        public bool HasBaseHP => LevelStats.HasHP;
        public bool HasBaseATK => LevelStats.HasATK;
        public bool HasBaseDEF => LevelStats.HasDEF;
        public bool HasBaseCRI => LevelStats.HasCRI;
        public bool HasBaseDOG => LevelStats.HasDOG;
        public bool HasBaseSPD => LevelStats.HasSPD;

        public int AdditionalHP => HP - _levelStats.HP;
        public int AdditionalATK => ATK - _levelStats.ATK;
        public int AdditionalDEF => DEF - _levelStats.DEF;
        public int AdditionalCRI => CRI - _levelStats.CRI;
        public int AdditionalDOG => DOG - _levelStats.DOG;
        public int AdditionalSPD => SPD - _levelStats.SPD;

        public bool HasAdditionalHP => AdditionalHP > 0;
        public bool HasAdditionalATK => AdditionalATK > 0;
        public bool HasAdditionalDEF => AdditionalDEF > 0;
        public bool HasAdditionalCRI => AdditionalCRI > 0;
        public bool HasAdditionalDOG => AdditionalDOG > 0;
        public bool HasAdditionalSPD => AdditionalSPD > 0;

        public bool HasAdditionalStats => HasAdditionalHP || HasAdditionalATK || HasAdditionalDEF || HasAdditionalCRI ||
                                          HasAdditionalDOG || HasAdditionalSPD;

        public CharacterStats(
            CharacterSheet.Row row,
            int level,
            TableSheets sheets
        )
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            SetAll(level, null, null, null, sheets);
        }

        protected CharacterStats(CharacterStats value) : base(value)
        {
            _row = value._row;

            _levelStats = (Stats)value._levelStats.Clone();
            _equipmentStats = (Stats)value._equipmentStats.Clone();
            _consumableStats = (Stats)value._consumableStats.Clone();
            _buffStats = (Stats)value._buffStats.Clone();

            _equipmentStatModifiers = value._equipmentStatModifiers;
            _consumableStatModifiers = value._consumableStatModifiers;
            _buffStatModifiers = value._buffStatModifiers;

            Level = value.Level;
        }

        public CharacterStats SetAll(
            int level,
            IReadOnlyList<Equipment> equipments,
            IReadOnlyList<Consumable> consumables, 
            IReadOnlyList<Buff.Buff> buffs,
            TableSheets sheets
        )
        {
            SetLevel(level, false);
            SetEquipments(equipments, sheets.EquipmentItemSetEffectSheet, false);
            SetConsumables(consumables, false);
            SetBuffs(buffs, false);
            UpdateLevelStats();
            EqualizeCurrentHPWithHP();

            return this;
        }

        /// <summary>
        /// 레벨을 설정하고, 생성자에서 받은 캐릭터 정보와 레벨을 바탕으로 모든 스탯을 재설정한다.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetLevel(int level, bool updateImmediate = true)
        {
            if (level == Level)
                return this;

            Level = level;

            if (updateImmediate)
            {
                UpdateLevelStats();
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
            IReadOnlyList<Equipment> value,
            EquipmentItemSetEffectSheet sheet,
            bool updateImmediate=true
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

                    if (statMap.HasDOG)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.DOG, StatModifier.OperationType.Add,
                            statMap.DOG));
                    }

                    if (statMap.HasSPD)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Add,
                            statMap.SPD));
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
        public CharacterStats SetConsumables(IReadOnlyList<Consumable> value, bool updateImmediate = true)
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

                    if (statMap.HasDOG)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.DOG, StatModifier.OperationType.Add,
                            statMap.DOG));
                    }

                    if (statMap.HasSPD)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Add,
                            statMap.SPD));
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
        public CharacterStats SetBuffs(IEnumerable<Buff.Buff> value, bool updateImmediate = true)
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

        public void AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            _buffStatModifiers[buff.RowData.GroupId] = buff.RowData.StatModifier;

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void RemoveBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (!_buffStatModifiers.ContainsKey(buff.RowData.GroupId))
                return;

            _buffStatModifiers.Remove(buff.RowData.GroupId);

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        private void UpdateLevelStats()
        {
            var statsData = _row.ToStats(Level);
            _levelStats.Set(statsData);
            UpdateEquipmentStats();
        }

        private void UpdateEquipmentStats()
        {
            _equipmentStats.Set(_equipmentStatModifiers, _levelStats);
            UpdateConsumableStats();
        }

        private void UpdateConsumableStats()
        {
            _consumableStats.Set(_consumableStatModifiers, _levelStats, _equipmentStats);
            UpdateBuffStats();
        }

        private void UpdateBuffStats()
        {
            _buffStats.Set(_buffStatModifiers.Values, _levelStats, _equipmentStats, _consumableStats);
            UpdateTotalStats();
        }

        private void UpdateTotalStats()
        {
            Set(_levelStats, _equipmentStats, _consumableStats, _buffStats);
            // 최소값 보정
            hp.SetValue(Math.Max(0, hp.Value));
            atk.SetValue(Math.Max(0, atk.Value));
            def.SetValue(Math.Max(0, def.Value));
            cri.SetValue(Math.Max(0, cri.Value));
            dog.SetValue(Math.Max(0, dog.Value));
            spd.SetValue(Math.Max(0, spd.Value));
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
                if (HasBaseDOG)
                    yield return (StatType.DOG, BaseDOG);
                if (HasBaseSPD)
                    yield return (StatType.SPD, BaseSPD);
            }
            else
            {
                yield return (StatType.HP, BaseHP);
                yield return (StatType.ATK, BaseATK);
                yield return (StatType.DEF, BaseDEF);
                yield return (StatType.CRI, BaseCRI);
                yield return (StatType.DOG, BaseDOG);
                yield return (StatType.SPD, BaseSPD);
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
                if (HasAdditionalDOG)
                    yield return (StatType.DOG, AdditionalDOG);
                if (HasAdditionalSPD)
                    yield return (StatType.SPD, AdditionalSPD);
            }
            else
            {
                yield return (StatType.HP, AdditionalHP);
                yield return (StatType.ATK, AdditionalATK);
                yield return (StatType.DEF, AdditionalDEF);
                yield return (StatType.CRI, AdditionalCRI);
                yield return (StatType.DOG, AdditionalDOG);
                yield return (StatType.SPD, AdditionalSPD);
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
                if (HasBaseDOG || HasAdditionalDOG)
                    yield return (StatType.DOG, BaseDOG, AdditionalDOG);
                if (HasBaseSPD || HasAdditionalSPD)
                    yield return (StatType.SPD, BaseSPD, AdditionalSPD);
            }
            else
            {
                yield return (StatType.HP, BaseHP, AdditionalHP);
                yield return (StatType.ATK, BaseATK, AdditionalATK);
                yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                yield return (StatType.DOG, BaseDOG, AdditionalDOG);
                yield return (StatType.SPD, BaseSPD, AdditionalSPD);
            }
        }
    }
}
