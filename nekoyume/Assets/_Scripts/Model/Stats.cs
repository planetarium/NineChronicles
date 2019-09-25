using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    public interface IStatMap
    {
        string Key { get; }
        decimal Value { get; set; }
        decimal AdditionalValue { get; set; }
        int TotalValue { get; }
        decimal TotalValueRaw { get; }
        string GetInformation();
        void GetInformation(out string key, out string value);
        void UpdatePlayer(Player player);
    }

    [Serializable]
    public class StatMap : IStatMap
    {
        public string Key { get; }
        public decimal Value { get; set; }
        public decimal AdditionalValue { get; set; }
        public int TotalValue => (int) (Value + AdditionalValue);
        public decimal TotalValueRaw => Value + AdditionalValue;

        public StatMap(string key)
        {
            Key = key;
            Value = 0m;
            AdditionalValue = 0m;
        }

        public StatMap(string key, decimal value)
        {
            Key = key;
            Value = value;
            AdditionalValue = 0m;
        }

        public StatMap(string key, decimal value, decimal additionalValue)
        {
            Key = key;
            Value = value;
            AdditionalValue = additionalValue;
        }

        private bool Equals(StatMap other)
        {
            return string.Equals(Key, other.Key) &&
                   Value.Equals(other.Value) &&
                   AdditionalValue.Equals(other.AdditionalValue);
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
                var hashCode = (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ AdditionalValue.GetHashCode();
                return hashCode;
            }
        }

        public string GetInformation()
        {
            if (Key == "turnSpeed" || Key == "attackRange")
            {
                return "";
            }

            var translatedText = TranslateKeyToString();
            if (string.IsNullOrEmpty(translatedText))
            {
                return "";
            }

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
            if (Key == "turnSpeed" || Key == "attackRange")
            {
                key = "";
                value = "";

                return;
            }

            key = TranslateKeyToString();
            if (string.IsNullOrEmpty(key))
            {
                value = "";

                return;
            }

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

        public string TranslateKeyToString()
        {
            switch (Key)
            {
                case "damage":
                    return "공격력";
                case "defense":
                    return "방어력";
                case "health":
                    return "체력";
                case "luck":
                    return "행운";
                case "turnSpeed":
                    return "행동력";
                case "attackRange":
                    return "공격 거리";
                default:
                    return "";
            }
        }

        public void UpdatePlayer(Player player)
        {
            switch (Key)
            {
                case "damage":
                    player.atk += TotalValue;
                    break;
                case "defense":
                    player.def += TotalValue;
                    break;
                case "health":
                    player.currentHP += TotalValue;
                    player.hp += TotalValue;
                    break;
                case "luck":
                    player.luck += TotalValue * 0.01m;
                    break;
                case "turnSpeed":
                    player.TurnSpeed = TotalValue;
                    break;
                case "attackRange":
                    player.attackRange = TotalValue;
                    break;
            }
        }
    }

    [Serializable]
    public class Stats
    {
        public IReadOnlyDictionary<string, IStatMap> StatMaps => _statMaps;
        public int Damage => StatMaps.ContainsKey("damage") ? StatMaps["damage"].TotalValue : 0;
        public int Defense => StatMaps.ContainsKey("defense") ? StatMaps["defense"].TotalValue : 0;
        public int HP => StatMaps.ContainsKey("health") ? StatMaps["health"].TotalValue : 0;
        public decimal Luck => StatMaps.ContainsKey("luck") ? StatMaps["luck"].TotalValueRaw : 0m;

        private readonly Dictionary<string, IStatMap> _statMaps = new Dictionary<string, IStatMap>();

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

        public void AddStatValue(string key, decimal value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMap(key));
            }

            _statMaps[key].Value += value;
        }

        public void AddStatAdditionalValue(string key, decimal additionalValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

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

        public void UpdatePlayer(Player player)
        {
            foreach (var stat in _statMaps)
            {
                stat.Value.UpdatePlayer(player);
            }
        }
    }
}
