using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly IntStatWithCurrent hp = new IntStatWithCurrent(StatType.HP);
        protected readonly IntStat atk = new IntStat(StatType.ATK);
        protected readonly IntStat def = new IntStat(StatType.DEF);
        protected readonly DecimalStat cri = new DecimalStat(StatType.CRI);
        protected readonly DecimalStat dog = new DecimalStat(StatType.DOG);
        protected readonly DecimalStat spd = new DecimalStat(StatType.SPD);

        public int HP => hp.Value;
        public int ATK => atk.Value;
        public int DEF => def.Value;
        public int CRI => cri.ValueAsInt;
        public int DOG => dog.ValueAsInt;
        public int SPD => spd.ValueAsInt;

        public bool HasHP => HP > 0;
        public bool HasATK => ATK > 0;
        public bool HasDEF => DEF > 0;
        public bool HasCRI => CRI > 0;
        public bool HasDOG => DOG > 0;
        public bool HasSPD => SPD > 0;

        public int CurrentHP
        {
            get => hp.Current;
            set => hp.SetCurrent(value);
        }

        public Stats()
        {
        }

        protected Stats(Stats value)
        {
            hp = (IntStatWithCurrent)value.hp.Clone();
            atk = (IntStat)value.atk.Clone();
            def = (IntStat)value.def.Clone();
            cri = (DecimalStat)value.cri.Clone();
            dog = (DecimalStat)value.dog.Clone();
            spd = (DecimalStat)value.spd.Clone();
        }

        public void Reset()
        {
            hp.Reset();
            atk.Reset();
            def.Reset();
            cri.Reset();
            dog.Reset();
            spd.Reset();
        }

        /// <summary>
        /// statsArray의 모든 능력치의 합으로 초기화한다. 이때, decimal 값을 그대로 더한다.
        /// </summary>
        /// <param name="statsArray"></param>
        public void Set(params Stats[] statsArray)
        {
            hp.SetValue(statsArray.Sum(stats => stats.hp.Value));
            atk.SetValue(statsArray.Sum(stats => stats.atk.Value));
            def.SetValue(statsArray.Sum(stats => stats.def.Value));
            cri.SetValue(statsArray.Sum(stats => stats.cri.Value));
            dog.SetValue(statsArray.Sum(stats => stats.dog.Value));
            spd.SetValue(statsArray.Sum(stats => stats.spd.Value));
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
                        hp.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.hp.Value)));
                        break;
                    case StatType.ATK:
                        atk.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.atk.Value)));
                        break;
                    case StatType.DEF:
                        def.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.def.Value)));
                        break;
                    case StatType.CRI:
                        cri.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.cri.Value)));
                        break;
                    case StatType.DOG:
                        dog.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.dog.Value)));
                        break;
                    case StatType.SPD:
                        spd.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.spd.Value)));
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
            hp.SetValue(value.HP);
            atk.SetValue(value.ATK);
            def.SetValue(value.DEF);
            cri.SetValue(value.CRI);
            dog.SetValue(value.DOG);
            spd.SetValue(value.SPD);
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
                    hp.SetValue(value);
                    break;
                case StatType.ATK:
                    atk.SetValue(value);
                    break;
                case StatType.DEF:
                    def.SetValue(value);
                    break;
                case StatType.CRI:
                    cri.SetValue(value);
                    break;
                case StatType.DOG:
                    dog.SetValue(value);
                    break;
                case StatType.SPD:
                    spd.SetValue(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasHP)
                    yield return (StatType.HP, HP);
                if (HasATK)
                    yield return (StatType.ATK, ATK);
                if (HasDEF)
                    yield return (StatType.DEF, DEF);
                if (HasCRI)
                    yield return (StatType.CRI, CRI);
                if (HasDOG)
                    yield return (StatType.DOG, DOG);
                if (HasSPD)
                    yield return (StatType.SPD, SPD);
            }
            else
            {
                yield return (StatType.HP, HP);
                yield return (StatType.ATK, ATK);
                yield return (StatType.DEF, DEF);
                yield return (StatType.CRI, CRI);
                yield return (StatType.DOG, DOG);
                yield return (StatType.SPD, SPD);
            }
        }

        public void EqualizeCurrentHPWithHP()
        {
            hp.EqualizeCurrentWithValue();
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
