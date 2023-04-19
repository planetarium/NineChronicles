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
        public const long DecimalDivider = 100;

        private long _baseValueInternal;

        private long _additionalValueInternal;

        public bool HasTotalValue => TotalValue != default;

        public bool HasBaseValue => _baseValueInternal != default;

        public bool HasAdditionalValue => _additionalValueInternal != default;

        public StatType StatType;

        public decimal BaseValue => (decimal)_baseValueInternal / DecimalDivider;

        public decimal AdditionalValue => (decimal)_additionalValueInternal / DecimalDivider;

        public int BaseValueAsInt => (int)BaseValue;

        public int AdditionalValueAsInt => (int)AdditionalValue;

        public int TotalValueAsInt => BaseValueAsInt + AdditionalValueAsInt;

        public decimal TotalValue => (_baseValueInternal + _additionalValueInternal) / (decimal)DecimalDivider;

        public DecimalStat(StatType type, decimal value = 0m, decimal additionalValue = 0m)
        {
            StatType = type;
            _baseValueInternal = ToInternalValue(value);
            _additionalValueInternal = ToInternalValue(additionalValue);
        }

        public virtual void Reset()
        {
            _baseValueInternal = 0;
            _additionalValueInternal = 0;
        }

        protected DecimalStat(DecimalStat value)
        {
            StatType = value.StatType;
            _baseValueInternal = value._baseValueInternal;
            _additionalValueInternal = value._additionalValueInternal;
        }

        public DecimalStat(Dictionary serialized)
        {
            StatType = StatTypeExtension.Deserialize((Binary)serialized["statType"]);

            _baseValueInternal = ToInternalValue(serialized["value"].ToDecimal());
            // This field is added later.
            if (serialized.TryGetValue((Text)"additionalValue", out var additionalValue))
            {
                _additionalValueInternal = ToInternalValue(additionalValue.ToDecimal());
            }
        }

        public void SetBaseValue(decimal value)
        {
            var internalValue = ToInternalValue(value);
            SetBaseValueInternal(internalValue);
        }

        private void SetBaseValueInternal(long internalValue)
        {
            _baseValueInternal = internalValue;
        }

        public void AddBaseValue(decimal value)
        {
            var internalValue = ToInternalValue(value);
            AddBaseValueInternal(internalValue);
        }

        private void AddBaseValueInternal(long internalValue)
        {
            SetBaseValueInternal(_baseValueInternal + internalValue);
        }

        public void SetAdditionalValue(decimal value)
        {
            var internalValue = ToInternalValue(value);
            SetAdditionalValueInternal(internalValue);
        }

        private void SetAdditionalValueInternal(long internalValue)
        {
            _additionalValueInternal = internalValue;
        }

        public void AddAdditionalValue(decimal value)
        {
            var internalValue = ToInternalValue(value);
            AddAdditionalValueInternal(internalValue);
        }

        private void AddAdditionalValueInternal(long internalValue)
        {
            SetAdditionalValueInternal(_additionalValueInternal + internalValue);
        }

        private long ToInternalValue(decimal value)
        {
            return (long)(value * DecimalDivider);
        }

        public virtual object Clone()
        {
            return new DecimalStat(this);
        }

        protected bool Equals(DecimalStat other)
        {
            return _baseValueInternal == other._baseValueInternal &&
                _additionalValueInternal == other._additionalValueInternal &&
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
            return HashCode.Combine(_baseValueInternal, _additionalValueInternal, StatType);
        }

        public IValue SerializeForLegacyEquipmentStat() =>
            Dictionary.Empty
                .Add("type", StatTypeExtension.Serialize(StatType))
                .Add("value", BaseValueAsInt.Serialize());

        public virtual IValue Serialize()
        {
            var baseValue = _baseValueInternal / (decimal)DecimalDivider;
            var additionValue = _additionalValueInternal / (decimal)DecimalDivider;

            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = baseValue.Serialize(),
                [(Text)"additionalValue"] = additionValue.Serialize(),
            });
        }

        public virtual void Deserialize(Dictionary serialized)
        {
            var (statType, baseValue, additionalValue) = serialized.GetStat();
            StatType = statType;
            _baseValueInternal = ToInternalValue(baseValue);
            _additionalValueInternal = ToInternalValue(additionalValue);
        }
    }
}
