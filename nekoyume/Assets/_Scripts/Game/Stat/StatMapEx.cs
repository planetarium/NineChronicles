using System;
using Nekoyume.EnumType;

namespace Nekoyume.Game
{
    [Serializable]
    public class StatMapEx : StatMap
    {
        private decimal _additionalValue;

        public decimal AdditionalValue
        {
            get => _additionalValue;
            set
            {
                _additionalValue = value;
                AdditionalValueAsInt = (int) _additionalValue;
            }
        }
        
        public int AdditionalValueAsInt { get; private set; }

        public decimal TotalValue => Value + AdditionalValueAsInt;
        public int TotalValueAsInt => ValueAsInt + AdditionalValueAsInt;

        public StatMapEx(StatType statType, decimal value = 0m, decimal additionalValue = 0m) : base(statType, value)
        {
            AdditionalValue = additionalValue;
        }

        public StatMapEx(StatMap statMap) : this(statMap.StatType, statMap.Value)
        {
        }

        protected bool Equals(StatMapEx other)
        {
            return base.Equals(other) && _additionalValue == other._additionalValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StatMapEx) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ _additionalValue.GetHashCode();
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
                return AdditionalValueAsInt > 0m
                    ? $"{translatedText} {Value} <color=#00FF00>(+{AdditionalValueAsInt})</color>"
                    : $"{translatedText} {Value}";
            }

            return AdditionalValueAsInt > 0m
                ? $"{translatedText} <color=#00FF00>(+{AdditionalValueAsInt})</color>"
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
                value = AdditionalValueAsInt > 0m
                    ? $"{Value} <color=#00FF00>(+{AdditionalValueAsInt})</color>"
                    : $"{Value}";

                return;
            }

            value = AdditionalValueAsInt > 0m
                ? $"<color=#00FF00>(+{AdditionalValueAsInt})</color>"
                : "";
        }
    }
}
