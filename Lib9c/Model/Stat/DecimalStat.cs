using Bencodex.Types;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class DecimalStat : ICloneable, IState
    {
        private decimal _value;

        private decimal _additionalValue;

        public bool HasValue => TotalValue != decimal.Zero;

        public bool HasBaseValue => _value != decimal.Zero;

        public bool HasAdditionalValue => _additionalValue != decimal.Zero;


        public readonly StatType StatType;
        public decimal Value
        {
            get => _value;
            private set
            {
                _value = value;
                ValueAsInt = (int)_value;
            }
        }

        public decimal AdditionalValue
        {
            get => _additionalValue;
            set
            {
                _additionalValue = value;
                AdditionalValueAsInt = (int)_additionalValue;
            }
        }

        public decimal TotalValue => Value + AdditionalValue;
        public int TotalValueAsInt => ValueAsInt + AdditionalValueAsInt;
        public int ValueAsInt { get; private set; }
        public int AdditionalValueAsInt { get; private set; }

        public DecimalStat(StatType type, decimal value = 0m, decimal additionalValue = 0m)
        {
            StatType = type;
            Value = value;
            AdditionalValue = additionalValue;
        }

        public virtual void Reset()
        {
            Value = 0m;
        }

        protected DecimalStat(DecimalStat value)
        {
            StatType = value.StatType;
            Value = value.Value;
            AdditionalValue = value.AdditionalValue;
        }

        public DecimalStat(Dictionary serialized)
        {
            StatType = StatTypeExtension.Deserialize((Binary)serialized["statType"]);
            Value = serialized["value"].ToDecimal();
            // This field is added later.
            if (serialized.TryGetValue((Text)"additionalValue", out var additionalValue))
            {
                AdditionalValue = additionalValue.ToDecimal();
            }
        }

        public void SetValue(decimal value)
        {
            Value = value;
        }

        public void AddValue(decimal value)
        {
            SetValue(Value + value);
        }

        public void SetAdditionalValue(decimal value)
        {
            AdditionalValue = value;
        }

        public void AddAdditionalValue(decimal value)
        {
            SetAdditionalValue(AdditionalValue + value);
        }

        public virtual object Clone()
        {
            return new DecimalStat(this);
        }

        protected bool Equals(DecimalStat other)
        {
            return _value == other._value &&
                _additionalValue == other._additionalValue &&
                StatType == other.StatType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DecimalStat) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, _additionalValue, StatType);
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = Value.Serialize(),
                [(Text)"additionalValue"] = AdditionalValue.Serialize(),
            });
    }
}
