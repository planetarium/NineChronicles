using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.State;

namespace Nekoyume.Game
{
    // todo: `Stats`나 `StatModifier`로 대체되어야 함.
    [Serializable]
    public class StatsMap : IStats, IAdditionalStats, IState
    {
        public IReadOnlyDictionary<StatType, StatMapEx> StatMaps => _statMaps;

        public int HP => HasHP ? StatMaps[StatType.HP].TotalValueAsInt : 0;
        public int ATK => HasATK ? StatMaps[StatType.ATK].TotalValueAsInt : 0;
        public int DEF => HasDEF ? StatMaps[StatType.DEF].TotalValueAsInt : 0;
        public int CRI => HasCRI ? StatMaps[StatType.CRI].TotalValueAsInt : 0;
        public int DOG => HasDOG ? StatMaps[StatType.DOG].TotalValueAsInt : 0;
        public int SPD => HasSPD ? StatMaps[StatType.SPD].TotalValueAsInt : 0;
        
        public bool HasHP => StatMaps.ContainsKey(StatType.HP) && StatMaps[StatType.HP].HasValue || HasAdditionalHP;
        public bool HasATK => StatMaps.ContainsKey(StatType.ATK) && StatMaps[StatType.ATK].HasValue || HasAdditionalATK;
        public bool HasDEF => StatMaps.ContainsKey(StatType.DEF) && StatMaps[StatType.DEF].HasValue || HasAdditionalDEF;
        public bool HasCRI => StatMaps.ContainsKey(StatType.CRI) && StatMaps[StatType.CRI].HasValue || HasAdditionalCRI;
        public bool HasDOG => StatMaps.ContainsKey(StatType.DOG) && StatMaps[StatType.DOG].HasValue || HasAdditionalDOG;
        public bool HasSPD => StatMaps.ContainsKey(StatType.SPD) && StatMaps[StatType.SPD].HasValue || HasAdditionalSPD;

        public int AdditionalHP => HasAdditionalHP ? StatMaps[StatType.HP].AdditionalValueAsInt : 0;
        public int AdditionalATK => HasAdditionalATK ? StatMaps[StatType.ATK].AdditionalValueAsInt : 0;
        public int AdditionalDEF => HasAdditionalDEF ? StatMaps[StatType.DEF].AdditionalValueAsInt : 0;
        public int AdditionalCRI => HasAdditionalCRI ? StatMaps[StatType.CRI].AdditionalValueAsInt : 0;
        public int AdditionalDOG => HasAdditionalDOG ? StatMaps[StatType.DOG].AdditionalValueAsInt : 0;
        public int AdditionalSPD => HasAdditionalSPD ? StatMaps[StatType.SPD].AdditionalValueAsInt : 0;
        
        public bool HasAdditionalHP => StatMaps.ContainsKey(StatType.HP) && StatMaps[StatType.HP].HasAdditionalValue;
        public bool HasAdditionalATK => StatMaps.ContainsKey(StatType.ATK) && StatMaps[StatType.ATK].HasAdditionalValue;
        public bool HasAdditionalDEF => StatMaps.ContainsKey(StatType.DEF) && StatMaps[StatType.DEF].HasAdditionalValue;
        public bool HasAdditionalCRI => StatMaps.ContainsKey(StatType.CRI) && StatMaps[StatType.CRI].HasAdditionalValue;
        public bool HasAdditionalDOG => StatMaps.ContainsKey(StatType.DOG) && StatMaps[StatType.DOG].HasAdditionalValue;
        public bool HasAdditionalSPD => StatMaps.ContainsKey(StatType.SPD) && StatMaps[StatType.SPD].HasAdditionalValue;

        public bool HasAdditionalStats => HasAdditionalHP || HasAdditionalATK || HasAdditionalDEF || HasAdditionalCRI ||
                                          HasAdditionalDOG || HasAdditionalSPD;

        private readonly Dictionary<StatType, StatMapEx> _statMaps =
            new Dictionary<StatType, StatMapEx>(StatTypeComparer.Instance);

        public StatsMap()
        {
        }

        protected bool Equals(StatsMap other)
        {
            return Equals(_statMaps, other._statMaps);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StatsMap) obj);
        }

        public override int GetHashCode()
        {
            return (_statMaps != null ? _statMaps.GetHashCode() : 0);
        }

        public void AddStatValue(StatType key, decimal value)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].Value += value;
            PostStatValueChanged(key);
        }

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].AdditionalValue += additionalValue;
            PostStatValueChanged(key);
        }

        public void AddStatAdditionalValue(StatModifier statModifier)
        {
            AddStatAdditionalValue(statModifier.StatType, statModifier.Value);
        }

        public void SetStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].AdditionalValue = additionalValue;
            PostStatValueChanged(key);
        }

        private void PostStatValueChanged(StatType key)
        {
            if (!_statMaps.ContainsKey(key))
                return;

            var statMap = _statMaps[key];
            if (statMap.HasValue ||
                statMap.HasAdditionalValue)
                return;

            _statMaps.Remove(key);
        }
        
        public int GetValue(StatType statType, bool ignoreAdditional = false)
        {
            switch (statType)
            {
                case StatType.HP:
                    return ignoreAdditional ? HP - AdditionalHP : HP;
                case StatType.ATK:
                    return ignoreAdditional ? ATK - AdditionalATK : ATK;
                case StatType.DEF:
                    return ignoreAdditional ? DEF - AdditionalDEF : DEF;
                case StatType.CRI:
                    return ignoreAdditional ? CRI - AdditionalCRI : CRI;
                case StatType.DOG:
                    return ignoreAdditional ? DOG - AdditionalDOG : DOG;
                case StatType.SPD:
                    return ignoreAdditional ? SPD - AdditionalSPD : SPD;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public string GetInformation()
        {
            var sb = new StringBuilder();
            foreach (var pair in _statMaps)
            {
                var information = pair.Value.GetInformation();
                if (string.IsNullOrEmpty(information))
                {
                    continue;
                }

                sb.AppendLine(information);
            }

            return sb.ToString().Trim();
        }

        public IValue Serialize() =>
            new Bencodex.Types.Dictionary(
                StatMaps.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        kv.Key.Serialize(),
                        kv.Value.Serialize()
                    )
                )
            );

        public void Deserialize(Bencodex.Types.Dictionary serialized)
        {
            foreach (KeyValuePair<IKey, IValue> kv in serialized)
            {
                _statMaps[StatTypeExtension.Deserialize((Binary) kv.Key)] =
                    new StatMapEx((Bencodex.Types.Dictionary) kv.Value);
            }
        }

        public IEnumerable<(StatType, int)> GetStats(bool ignoreZero = true)
        {
            if (ignoreZero)
            {
                if (HasHP)
                    yield return (StatType.HP, HP);
                if (HasATK)
                    yield return (StatType.ATK, ATK);
                if (HasDEF)
                    yield return (StatType.DEF, DEF);
                if (HasDOG)
                    yield return (StatType.DOG, DOG);
                if (HasCRI)
                    yield return (StatType.CRI, CRI);
                if (HasSPD)
                    yield return (StatType.SPD, SPD);
            }
            else
            {
                yield return (StatType.HP, HP);
                yield return (StatType.ATK, ATK);
                yield return (StatType.DEF, DEF);
                yield return (StatType.DOG, DOG);
                yield return (StatType.CRI, CRI);
                yield return (StatType.SPD, SPD);
            }
        }
        
        public IEnumerable<(StatType, int)> GetAdditionalStats(bool ignoreZero = true)
        {
            if (ignoreZero)
            {
                if (HasAdditionalHP)
                    yield return (StatType.HP, AdditionalHP);
                if (HasAdditionalATK)
                    yield return (StatType.ATK, AdditionalATK);
                if (HasAdditionalDEF)
                    yield return (StatType.DEF, AdditionalDEF);
                if (HasAdditionalDOG)
                    yield return (StatType.DOG, AdditionalDOG);
                if (HasAdditionalCRI)
                    yield return (StatType.CRI, AdditionalCRI);
                if (HasAdditionalSPD)
                    yield return (StatType.SPD, AdditionalSPD);
            }
            else
            {
                yield return (StatType.HP, AdditionalHP);
                yield return (StatType.ATK, AdditionalATK);
                yield return (StatType.DEF, AdditionalDEF);
                yield return (StatType.DOG, AdditionalDOG);
                yield return (StatType.CRI, AdditionalCRI);
                yield return (StatType.SPD, AdditionalSPD);
            }
        }

        public void ClearAdditionalStats()
        {
            if (HasAdditionalHP)
            {
                SetStatAdditionalValue(StatType.HP, 0);
            }

            if (HasAdditionalATK)
            {
                SetStatAdditionalValue(StatType.ATK, 0);
            }

            if (HasAdditionalCRI)
            {
                SetStatAdditionalValue(StatType.CRI, 0);
            }

            if (HasAdditionalDEF)
            {
                SetStatAdditionalValue(StatType.DEF, 0);
            }

            if (HasAdditionalDOG)
            {
                SetStatAdditionalValue(StatType.DOG, 0);
            }

            if (HasAdditionalSPD)
            {
                SetStatAdditionalValue(StatType.SPD, 0);
            }
        }
    }
}
