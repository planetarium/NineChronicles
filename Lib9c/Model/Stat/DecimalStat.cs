using Bencodex.Types;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class DecimalStat : ICloneable, IState
    {
        private decimal _baseValue;

        private decimal _additionalValue;

        public bool HasTotalValue => TotalValue != decimal.Zero;

        public bool HasBaseValue => _baseValue != decimal.Zero;

        public bool HasAdditionalValue => _additionalValue != decimal.Zero;


        public StatType StatType;
        public decimal BaseValue
        {
            get => _baseValue;
            private set
            {
                _baseValue = value;
                BaseValueAsInt = (int)_baseValue;
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

        public decimal TotalValue => BaseValue + AdditionalValue;
        public int TotalValueAsInt => BaseValueAsInt + AdditionalValueAsInt;
        public int BaseValueAsInt { get; private set; }
        public int AdditionalValueAsInt { get; private set; }

        public DecimalStat(StatType type, decimal value = 0m, decimal additionalValue = 0m)
        {
            StatType = type;
            BaseValue = value;
            AdditionalValue = additionalValue;
        }

        public virtual void Reset()
        {
            BaseValue = 0m;
            AdditionalValue = 0m;
        }

        protected DecimalStat(DecimalStat value)
        {
            StatType = value.StatType;
            BaseValue = value.BaseValue;
            AdditionalValue = value.AdditionalValue;
        }

        public DecimalStat(Dictionary serialized)
        {
            StatType = StatTypeExtension.Deserialize((Binary)serialized["statType"]);
            BaseValue = serialized["value"].ToDecimal();
            // This field is added later.
            if (serialized.TryGetValue((Text)"additionalValue", out var additionalValue))
            {
                AdditionalValue = additionalValue.ToDecimal();
            }
        }

        public void SetValue(decimal value)
        {
            BaseValue = value;
        }

        public void AddValue(decimal value)
        {
            SetValue(BaseValue + value);
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
            return _baseValue == other._baseValue &&
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
            return HashCode.Combine(_baseValue, _additionalValue, StatType);
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = BaseValue.Serialize(),
                [(Text)"additionalValue"] = AdditionalValue.Serialize(),
            });

        public virtual void Deserialize(Dictionary serialized)
        {
            var deserialized = serialized.ToDecimalStat();
            StatType = deserialized.StatType;
            BaseValue = deserialized.BaseValue;
            AdditionalValue = deserialized.AdditionalValue;
        }
    }
}
