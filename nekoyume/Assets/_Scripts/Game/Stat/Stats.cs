using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;

namespace Nekoyume.Game
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        private readonly IntStatWithCurrent _hp = new IntStatWithCurrent(StatType.HP);
        private readonly IntStat _atk = new IntStat(StatType.ATK);
        private readonly IntStat _def = new IntStat(StatType.DEF);
        private readonly DecimalStat _cri = new DecimalStat(StatType.CRI);
        private readonly DecimalStat _dog = new DecimalStat(StatType.DOG);
        private readonly DecimalStat _spd = new DecimalStat(StatType.SPD);

        public int HP => _hp.Value;
        public int ATK => _atk.Value;
        public int DEF => _def.Value;
        public int CRI => _cri.ValueAsInt;
        public int DOG => _dog.ValueAsInt;
        public int SPD => _spd.ValueAsInt;
        
        public bool HasHP => HP > 0;
        public bool HasATK => ATK > 0;
        public bool HasDEF => DEF > 0;
        public bool HasCRI => CRI > 0;
        public bool HasDOG => DOG > 0;
        public bool HasSPD => SPD > 0;

        public int CurrentHP
        {
            get => _hp.Current;
            set => _hp.SetCurrent(value);
        }

        public Stats()
        {
        }

        protected Stats(Stats value)
        {
            _hp = (IntStatWithCurrent) value._hp.Clone();
            _atk = (IntStat) value._atk.Clone();
            _def = (IntStat) value._def.Clone();
            _cri = (DecimalStat) value._cri.Clone();
            _dog = (DecimalStat) value._dog.Clone();
            _spd = (DecimalStat) value._spd.Clone();
        }

        public void Reset()
        {
            _hp.Reset();
            _atk.Reset();
            _def.Reset();
            _cri.Reset();
            _dog.Reset();
            _spd.Reset();
        }

        /// <summary>
        /// statsArray의 모든 능력치의 합으로 초기화한다. 이때, decimal 값을 그대로 더한다.
        /// </summary>
        /// <param name="statsArray"></param>
        public void Set(params Stats[] statsArray)
        {
            _hp.SetValue(statsArray.Sum(stats => stats._hp.Value));
            _atk.SetValue(statsArray.Sum(stats => stats._atk.Value));
            _def.SetValue(statsArray.Sum(stats => stats._def.Value));
            _cri.SetValue(statsArray.Sum(stats => stats._cri.Value));
            _dog.SetValue(statsArray.Sum(stats => stats._dog.Value));
            _spd.SetValue(statsArray.Sum(stats => stats._spd.Value));
        }

        /// <summary>
        /// baseStatsArray의 모든 능력치의 합을 바탕으로, statModifiers를 통해서 추가되는 부분 만으로 초기화한다. 
        /// </summary>
        /// <param name="statModifiers"></param>
        /// <param name="baseStatsArray"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Set(IEnumerable<StatModifier> statModifiers, params Stats[] baseStatsArray)
        {
            Reset();
            
            foreach (var statModifier in statModifiers)
            {
                switch (statModifier.StatType)
                {
                    case StatType.HP:
                        _hp.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats._hp.Value)));
                        break;
                    case StatType.ATK:
                        _atk.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats._atk.Value)));
                        break;
                    case StatType.DEF:
                        _def.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats._def.Value)));
                        break;
                    case StatType.CRI:
                        _cri.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats._cri.Value)));
                        break;
                    case StatType.DOG:
                        _dog.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats._dog.Value)));
                        break;
                    case StatType.SPD:
                        _spd.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats._spd.Value)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        /// <summary>
        /// value 값 그대로 초기화한다.
        /// </summary>
        /// <param name="value"></param>
        public void Set(StatsMap value)
        {
            _hp.SetValue(value.HP);
            _atk.SetValue(value.ATK);
            _def.SetValue(value.DEF);
            _cri.SetValue(value.CRI);
            _dog.SetValue(value.DOG);
            _spd.SetValue(value.SPD);
        }
        
        /// <summary>
        /// Please to use only test.
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetStatForTest(StatType statType, int value)
        {
            switch (statType)
            {
                case StatType.HP:
                    _hp.SetValue(value);
                    break;
                case StatType.ATK:
                    _atk.SetValue(value);
                    break;
                case StatType.DEF:
                    _def.SetValue(value);
                    break;
                case StatType.CRI:
                    _cri.SetValue(value);
                    break;
                case StatType.DOG:
                    _dog.SetValue(value);
                    break;
                case StatType.SPD:
                    _spd.SetValue(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public void Add(StatMap value)
        {
            switch (value.StatType)
            {
                case StatType.HP:
                    _hp.AddValue(value.ValueAsInt);
                    break;
                case StatType.ATK:
                    _atk.AddValue(value.ValueAsInt);
                    break;
                case StatType.DEF:
                    _def.AddValue(value.ValueAsInt);
                    break;
                case StatType.CRI:
                    _cri.AddValue(value.Value);
                    break;
                case StatType.DOG:
                    _dog.AddValue(value.Value);
                    break;
                case StatType.SPD:
                    _spd.AddValue(value.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Add(StatMapEx value)
        {
            switch (value.StatType)
            {
                case StatType.HP:
                    _hp.AddValue(value.TotalValueAsInt);
                    break;
                case StatType.ATK:
                    _atk.AddValue(value.TotalValueAsInt);
                    break;
                case StatType.DEF:
                    _def.AddValue(value.TotalValueAsInt);
                    break;
                case StatType.CRI:
                    _cri.AddValue(value.TotalValue);
                    break;
                case StatType.DOG:
                    _dog.AddValue(value.TotalValue);
                    break;
                case StatType.SPD:
                    _spd.AddValue(value.TotalValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Add(StatsMap value)
        {
            _hp.AddValue(value.HP);
            _atk.AddValue(value.ATK);
            _def.AddValue(value.DEF);
            _cri.AddValue(value.CRI);
            _dog.AddValue(value.DOG);
            _spd.AddValue(value.SPD);
        }

        public void Add(StatModifier value)
        {
            switch (value.StatType)
            {
                case StatType.HP:
                    value.Modify(_hp);
                    break;
                case StatType.ATK:
                    value.Modify(_atk);
                    break;
                case StatType.DEF:
                    value.Modify(_def);
                    break;
                case StatType.CRI:
                    value.Modify(_cri);
                    break;
                case StatType.DOG:
                    value.Modify(_dog);
                    break;
                case StatType.SPD:
                    value.Modify(_spd);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void EqualizeCurrentWithValue(StatType statType)
        {
            switch (statType)
            {
                case StatType.HP:
                    _hp.EqualizeCurrentWithValue();
                    break;
                case StatType.ATK:
                    break;
                case StatType.DEF:
                    break;
                case StatType.CRI:
                    break;
                case StatType.DOG:
                    break;
                case StatType.SPD:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public void EqualizeCurrentWithValueAll()
        {
            EqualizeCurrentWithValue(StatType.HP);
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
