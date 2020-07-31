using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    // todo: `DecimalStat`나 `StatModifier`으로 대체되어야 함.
    [Serializable]
    public class StatMap : IState
    {
        private decimal _value;

        public StatType StatType { get; }

        public bool HasValue => Value > 0m;

        public decimal Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueAsInt = (int)_value;
            }
        }

        public int ValueAsInt { get; private set; }

        public StatMap(StatType statType, decimal value = 0m)
        {
            StatType = statType;
            Value = value;
        }

        public StatMap(Dictionary serialized)
            : this(
                StatTypeExtension.Deserialize((Binary)serialized["statType"]),
                serialized["value"].ToDecimal()
            )
        {
        }

        protected bool Equals(StatMap other)
        {
            return _value == other._value && StatType == other.StatType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StatMap)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return _value.GetHashCode() * 397 ^ (int)StatType;
            }
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = Value.Serialize(),
            });
    }
}
