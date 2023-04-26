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
        public decimal BaseValue { get; private set; }

        public decimal AdditionalValue { get; private set; }

        public bool HasTotalValueAsInt => HasBaseValueAsInt || HasAdditionalValueAsInt;

        public bool HasBaseValueAsInt => BaseValue > 0;

        public bool HasAdditionalValueAsInt => AdditionalValue > 0;

        public bool HasBaseValue => BaseValue > 0m;

        public bool HasAdditionalValue => AdditionalValue > 0m;


        public StatType StatType;

        public int BaseValueAsInt => (int)BaseValue;

        public int AdditionalValueAsInt => (int)AdditionalValue;

        [Obsolete("For legacy equipments. (Before world 7 patch)")]
        public int TotalValueAsInt => BaseValueAsInt + AdditionalValueAsInt;

        public decimal TotalValue => BaseValue + AdditionalValue;

        public DecimalStat(StatType type, decimal value = 0m, decimal additionalValue = 0m)
        {
            StatType = type;
            BaseValue = value;
            AdditionalValue = additionalValue;
        }

        public virtual void Reset()
        {
            BaseValue = 0;
            AdditionalValue = 0;
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

        public void SetBaseValue(decimal value)
        {
            BaseValue = value;
        }

        public void AddBaseValue(decimal value)
        {
            SetBaseValue(BaseValue + value);
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
            return BaseValue == other.BaseValue &&
                AdditionalValue == other.AdditionalValue &&
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
            return HashCode.Combine(BaseValue, AdditionalValue, StatType);
        }

        public virtual IValue Serialize()
        {
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = BaseValue.Serialize(),
                [(Text)"additionalValue"] = AdditionalValue.Serialize(),
            });
        }

        public IValue SerializeWithoutAdditional()
        {
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = TotalValue.Serialize(),
            });
        }

        public virtual void Deserialize(Dictionary serialized)
        {
            var (statType, baseValue, additionalValue) = serialized.GetStat();
            StatType = statType;
            BaseValue = baseValue;
            AdditionalValue = additionalValue;
        }
    }
}
