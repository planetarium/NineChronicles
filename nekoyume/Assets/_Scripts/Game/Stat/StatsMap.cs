using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.State;

namespace Nekoyume.Game
{
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
        
        public bool HasHP => StatMaps.ContainsKey(StatType.HP);
        public bool HasATK => StatMaps.ContainsKey(StatType.ATK);
        public bool HasDEF => StatMaps.ContainsKey(StatType.DEF);
        public bool HasCRI => StatMaps.ContainsKey(StatType.CRI);
        public bool HasDOG => StatMaps.ContainsKey(StatType.DOG);
        public bool HasSPD => StatMaps.ContainsKey(StatType.SPD);

        public int AdditionalHP => HasAdditionalHP ? StatMaps[StatType.HP].AdditionalValueAsInt : 0;
        public int AdditionalATK => HasAdditionalATK ? StatMaps[StatType.ATK].AdditionalValueAsInt : 0;
        public int AdditionalDEF => HasAdditionalDEF ? StatMaps[StatType.DEF].AdditionalValueAsInt : 0;
        public int AdditionalCRI => HasAdditionalCRI ? StatMaps[StatType.CRI].AdditionalValueAsInt : 0;
        public int AdditionalDOG => HasAdditionalDOG ? StatMaps[StatType.DOG].AdditionalValueAsInt : 0;
        public int AdditionalSPD => HasAdditionalSPD ? StatMaps[StatType.SPD].AdditionalValueAsInt : 0;
        
        public bool HasAdditionalHP => StatMaps.ContainsKey(StatType.HP);
        public bool HasAdditionalATK => StatMaps.ContainsKey(StatType.ATK);
        public bool HasAdditionalDEF => StatMaps.ContainsKey(StatType.DEF);
        public bool HasAdditionalCRI => StatMaps.ContainsKey(StatType.CRI);
        public bool HasAdditionalDOG => StatMaps.ContainsKey(StatType.DOG);
        public bool HasAdditionalSPD => StatMaps.ContainsKey(StatType.SPD);

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
        }

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].AdditionalValue += additionalValue;
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

        public void GetInformation(out string keys, out string values)
        {
            var sbKeys = new StringBuilder();
            var sbValues = new StringBuilder();
            foreach (var pair in _statMaps)
            {
                pair.Value.GetInformation(out var key, out var value);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                sbKeys.AppendLine(key);
                sbValues.AppendLine(value);
            }

            keys = sbKeys.ToString().Trim();
            values = sbValues.ToString().Trim();
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
    }
}
