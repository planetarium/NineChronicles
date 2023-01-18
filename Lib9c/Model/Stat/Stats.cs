using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly IntStatWithCurrent hp = new IntStatWithCurrent(StatType.HP);
        protected readonly IntStat atk = new IntStat(StatType.ATK);
        protected readonly IntStat def = new IntStat(StatType.DEF);
        protected readonly DecimalStat cri = new DecimalStat(StatType.CRI);
        protected readonly DecimalStat hit = new DecimalStat(StatType.HIT);
        protected readonly DecimalStat spd = new DecimalStat(StatType.SPD);
        protected readonly IntStat drv = new IntStat(StatType.DRV);
        protected readonly IntStat drr = new IntStat(StatType.DRR);
        protected readonly IntStat cdmg = new IntStat(StatType.CDMG);

        public int HP => hp.Value;
        public int ATK => atk.Value;
        public int DEF => def.Value;
        public int CRI => cri.ValueAsInt;
        public int HIT => hit.ValueAsInt;
        public int SPD => spd.ValueAsInt;
        public int DRV => drv.Value;
        public int DRR => drr.Value;
        public int CDMG => cdmg.Value;

        public bool HasHP => HP > 0;
        public bool HasATK => ATK > 0;
        public bool HasDEF => DEF > 0;
        public bool HasCRI => CRI > 0;
        public bool HasHIT => HIT > 0;
        public bool HasSPD => SPD > 0;
        public bool HasDRV => DRV > 0;
        public bool HasDRR => DRR > 0;
        public bool HasCDMG => CDMG > 0;

        public int CurrentHP
        {
            get => hp.Current;
            set => hp.SetCurrent(value);
        }

        public Stats()
        {
        }

        public Stats(Stats value)
        {
            hp = (IntStatWithCurrent)value.hp.Clone();
            atk = (IntStat)value.atk.Clone();
            def = (IntStat)value.def.Clone();
            cri = (DecimalStat)value.cri.Clone();
            hit = (DecimalStat)value.hit.Clone();
            spd = (DecimalStat)value.spd.Clone();
            drv = (IntStat)value.drv.Clone();
            drr = (IntStat)value.drr.Clone();
            cdmg = (IntStat)value.cdmg.Clone();
        }

        public void Reset()
        {
            hp.Reset();
            atk.Reset();
            def.Reset();
            cri.Reset();
            hit.Reset();
            spd.Reset();
            drv.Reset();
            drr.Reset();
            cdmg.Reset();
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
            hit.SetValue(statsArray.Sum(stats => stats.hit.Value));
            spd.SetValue(statsArray.Sum(stats => stats.spd.Value));
            drv.SetValue(statsArray.Sum(stats => stats.drv.Value));
            drr.SetValue(statsArray.Sum(stats => stats.drr.Value));
            cdmg.SetValue(statsArray.Sum(stats => stats.cdmg.Value));
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
                    case StatType.HIT:
                        hit.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.hit.Value)));
                        break;
                    case StatType.SPD:
                        spd.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.spd.Value)));
                        break;
                    case StatType.DRV:
                        drv.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.drv.Value)));
                        break;
                    case StatType.DRR:
                        drr.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.drr.Value)));
                        break;
                    case StatType.CDMG:
                        cdmg.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.cdmg.Value)));
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
            hit.SetValue(value.HIT);
            spd.SetValue(value.SPD);
            drv.SetValue(value.DRV);
            drr.SetValue(value.DRR);
            cdmg.SetValue(value.CDMG);
        }

        /// <summary>
        /// Use this only for testing.
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
                case StatType.HIT:
                    hit.SetValue(value);
                    break;
                case StatType.SPD:
                    spd.SetValue(value);
                    break;
                case StatType.DRV:
                    drv.SetValue(value);
                    break;
                case StatType.DRR:
                    drr.SetValue(value);
                    break;
                case StatType.CDMG:
                    cdmg.SetValue(value);
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
                if (HasHIT)
                    yield return (StatType.HIT, HIT);
                if (HasSPD)
                    yield return (StatType.SPD, SPD);
                if (HasDRV)
                    yield return (StatType.DRV, DRV);
                if (HasDRR)
                    yield return (StatType.DRR, DRR);
                if (HasCDMG)
                    yield return (StatType.CDMG, CDMG);
            }
            else
            {
                yield return (StatType.HP, HP);
                yield return (StatType.ATK, ATK);
                yield return (StatType.DEF, DEF);
                yield return (StatType.CRI, CRI);
                yield return (StatType.HIT, HIT);
                yield return (StatType.SPD, SPD);
                yield return (StatType.DRV, DRV);
                yield return (StatType.DRR, DRR);
                yield return (StatType.CDMG, CDMG);
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
