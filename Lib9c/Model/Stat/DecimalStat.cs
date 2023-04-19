using Bencodex.Types;
using DecimalMath;
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

        public bool HasTotalValue => TotalValue != default;

        public bool HasBaseValue => _baseValue != default;

        public bool HasAdditionalValue => _additionalValue != default;

        public StatType StatType;

        public decimal BaseValue => _baseValue;

        public decimal AdditionalValue => _additionalValue;

        public int BaseValueAsInt => (int)BaseValue;

        public int AdditionalValueAsInt => (int)AdditionalValue;

        [Obsolete("For legacy equipments. (Before world 7 patch)")]
        public int TotalValueAsInt => BaseValueAsInt + AdditionalValueAsInt;

        public decimal TotalValue => _baseValue + _additionalValue;

        public DecimalStat(StatType type, decimal value = 0m, decimal additionalValue = 0m)
        {
            StatType = type;
            _baseValue = value;
            _additionalValue = additionalValue;
        }

        public virtual void Reset()
        {
            _baseValue = 0;
            _additionalValue = 0;
        }

        protected DecimalStat(DecimalStat value)
        {
            StatType = value.StatType;
            _baseValue = value._baseValue;
            _additionalValue = value._additionalValue;
        }

        public DecimalStat(Dictionary serialized)
        {
            StatType = StatTypeExtension.Deserialize((Binary)serialized["statType"]);

            _baseValue = serialized["value"].ToDecimal();
            // This field is added later.
            if (serialized.TryGetValue((Text)"additionalValue", out var additionalValue))
            {
                _additionalValue = additionalValue.ToDecimal();
            }
        }

        public void SetBaseValue(decimal value)
        {
            _baseValue = value;
        }

        public void AddBaseValue(decimal value)
        {
            SetBaseValue(_baseValue + value);
        }

        public void SetAdditionalValue(decimal value)
        {
            _additionalValue = value;
        }

        public void AddAdditionalValue(decimal value)
        {
            SetAdditionalValue(_additionalValue + value);
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
            return Equals((DecimalStat)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_baseValue, _additionalValue, StatType);
        }

        public IValue SerializeForLegacyEquipmentStat() =>
            Dictionary.Empty
                .Add("type", StatTypeExtension.Serialize(StatType))
                .Add("value", BaseValueAsInt.Serialize());

        public virtual IValue Serialize()
        {
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = BaseValue.Serialize(),
                [(Text)"additionalValue"] = AdditionalValue.Serialize(),
            });
        }

        public virtual void Deserialize(Dictionary serialized)
        {
            var (statType, baseValue, additionalValue) = serialized.GetStat();
            StatType = statType;
            _baseValue = baseValue;
            _additionalValue = additionalValue;
        }
    }
}
