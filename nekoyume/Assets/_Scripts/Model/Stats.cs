using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.EnumType;

namespace Nekoyume.Model
{
    [Serializable]
    public class StatData
    {
        public StatType StatType { get; }
        public decimal Value { get; set; }
        public int ValueAsInt => (int) Value;

        public StatData(StatType statType, decimal value = 0m)
        {
            StatType = statType;
            Value = value;
        }

        protected bool Equals(StatData other)
        {
            return StatType == other.StatType && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StatData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) StatType * 397) ^ Value.GetHashCode();
            }
        }
    }

    [Serializable]
    public class StatMap : StatData
    {
        public decimal AdditionalValue { get; set; }
        public decimal TotalValue => Value + AdditionalValue;
        public int TotalValueAsInt => (int) (Value + AdditionalValue);

        public StatMap(StatType statType, decimal value = 0m, decimal additionalValue = 0m) : base(statType, value)
        {
            AdditionalValue = additionalValue;
        }

        public StatMap(StatData statData) : this(statData.StatType, statData.Value)
        {
        }

        protected bool Equals(StatMap other)
        {
            return base.Equals(other) && AdditionalValue == other.AdditionalValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StatMap) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ AdditionalValue.GetHashCode();
            }
        }

        public string GetInformation()
        {
            if (StatType == StatType.SPD)
            {
                return "";
            }

            var translatedText = StatType.GetLocalizedString();

            if (Value > 0m)
            {
                return AdditionalValue > 0m
                    ? $"{translatedText} {Value} <color=#00FF00>(+{AdditionalValue})</color>"
                    : $"{translatedText} {Value}";
            }

            return AdditionalValue > 0m
                ? $"{translatedText} <color=#00FF00>(+{AdditionalValue})</color>"
                : null;
        }

        public void GetInformation(out string key, out string value)
        {
            if (StatType == StatType.SPD)
            {
                key = "";
                value = "";

                return;
            }

            key = StatType.GetLocalizedString();

            if (Value > 0m)
            {
                value = AdditionalValue > 0m
                    ? $"{Value} <color=#00FF00>(+{AdditionalValue})</color>"
                    : $"{Value}";

                return;
            }

            value = AdditionalValue > 0m
                ? $"<color=#00FF00>(+{AdditionalValue})</color>"
                : "";
        }
    }

    [Serializable]
    public class Stats
    {
        public IReadOnlyDictionary<StatType, StatMap> StatMaps => _statMaps;
        public int ATK => StatMaps.ContainsKey(StatType.ATK) ? StatMaps[StatType.ATK].TotalValueAsInt : 0;
        public int DEF => StatMaps.ContainsKey(StatType.DEF) ? StatMaps[StatType.DEF].TotalValueAsInt : 0;
        public int HP => StatMaps.ContainsKey(StatType.HP) ? StatMaps[StatType.HP].TotalValueAsInt : 0;
        public decimal CRI => StatMaps.ContainsKey(StatType.CRI) ? StatMaps[StatType.CRI].TotalValue : 0m;

        private readonly Dictionary<StatType, StatMap> _statMaps =
            new Dictionary<StatType, StatMap>(StatTypeComparer.Instance);

        private bool Equals(Stats other)
        {
            if (_statMaps.Count != other._statMaps.Count)
            {
                return false;
            }

            foreach (var pair in _statMaps)
            {
                if (!other._statMaps.ContainsKey(pair.Key) ||
                    !other._statMaps[pair.Key].Equals(pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Stats) obj);
        }

        public override int GetHashCode()
        {
            return (_statMaps != null ? _statMaps.GetHashCode() : 0);
        }

        public void AddStatValue(StatType key, decimal value)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMap(key));
            }

            _statMaps[key].Value += value;
        }

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMap(key));
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
    }
}
