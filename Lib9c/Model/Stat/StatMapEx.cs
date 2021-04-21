using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    // todo: 없어질 대상.
    [Serializable]
    public class StatMapEx : StatMap
    {
        private decimal _additionalValue;

        public bool HasAdditionalValue => AdditionalValue > 0m;

        public decimal AdditionalValue
        {
            get => _additionalValue;
            set
            {
                _additionalValue = value;
                AdditionalValueAsInt = (int)_additionalValue;
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

        public StatMapEx(Dictionary serialized) : base(serialized)
        {
            AdditionalValue = serialized["additionalValue"].ToDecimal();
        }

        protected bool Equals(StatMapEx other)
        {
            return base.Equals(other) && _additionalValue == other._additionalValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StatMapEx)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return base.GetHashCode() * 397 ^ _additionalValue.GetHashCode();
            }
        }


        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"additionalValue"] = (Text)AdditionalValue.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
    }
}
