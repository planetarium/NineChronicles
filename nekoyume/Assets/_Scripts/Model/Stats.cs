using System;
using System.Collections.Generic;
using System.Text;

namespace Nekoyume.Model
{
    public interface IStatMap
    {
        string GetInformation();
        void UpdatePlayer(Player player);
    }

    [Serializable]
    public class StatMap : IStatMap
    {
        public string Key { get; }
        public float Value { get; set; }
        public float AdditionalValue { get; set; }
        public int TotalValue => (int) (Value + AdditionalValue);

        public StatMap(string key)
        {
            Key = key;
            Value = 0f;
            AdditionalValue = 0f;
        }

        public StatMap(string key, float value)
        {
            Key = key;
            Value = value;
            AdditionalValue = 0f;
        }

        public StatMap(string key, float value, float additionalValue)
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

            if (Value > 0f)
            {
                return AdditionalValue > 0f
                    ? $"{translatedText} {Value} <color=#00FF00>(+{AdditionalValue})</color>"
                    : $"{translatedText} {Value}";
            }

            return AdditionalValue > 0f
                ? $"{translatedText} <color=#00FF00>(+{AdditionalValue})</color>"
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
                    return "공격";
                default:
                    return null;
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
                    player.luck += TotalValue;
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
    public class Stats : IStatMap
    {
        private Dictionary<string, StatMap> StatMaps { get; }

        public Stats()
        {
            StatMaps = new Dictionary<string, StatMap>();
        }
        
        private bool Equals(Stats other)
        {
            if (StatMaps.Count != other.StatMaps.Count)
            {
                return false;
            }
            
            foreach (var pair in StatMaps)
            {
                if (!other.StatMaps.ContainsKey(pair.Key) ||
                    !other.StatMaps[pair.Key].Equals(pair.Value))
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
            return (StatMaps != null ? StatMaps.GetHashCode() : 0);
        }

        public void SetStatValue(string key, float value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!StatMaps.ContainsKey(key))
            {
                StatMaps.Add(key, new StatMap(key));
            }

            StatMaps[key].Value = value;
        }

        public void SetStatAdditionalValue(string key, float additionalValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!StatMaps.ContainsKey(key))
            {
                StatMaps.Add(key, new StatMap(key));
            }

            StatMaps[key].AdditionalValue = additionalValue;
        }

        public string GetInformation()
        {
            var sb = new StringBuilder();
            foreach (var pair in StatMaps)
            {
                var information = pair.Value.GetInformation();
                if (string.IsNullOrEmpty(information))
                {
                    continue;
                }

                sb.AppendLine(pair.Value.GetInformation());
            }

            return sb.ToString().TrimEnd();
        }

        public void UpdatePlayer(Player player)
        {
            foreach (var stat in StatMaps)
            {
                stat.Value.UpdatePlayer(player);
            }
        }
    }
}
