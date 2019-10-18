using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.State;

namespace Nekoyume.Game
{
    [Serializable]
    public class StatMap : IState
    {
        private decimal _value;
        
        public StatType StatType { get; }
        
        public decimal Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueAsInt = (int) _value;
            }
        }
        
        public int ValueAsInt { get; private set; }

        public StatMap(StatType statType, decimal value = 0m)
        {
            StatType = statType;
            Value = value;
        }

        public StatMap(Bencodex.Types.Dictionary serialized)
            : this(
                StatTypeExtension.Deserialize((Binary) serialized[(Text) "statType"]),
                serialized[(Text) "value"].ToDecimal()
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
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StatMap) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_value.GetHashCode() * 397) ^ (int) StatType;
            }
        }

        public virtual IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "statType"] = StatType.Serialize(),
                [(Text) "value"] = Value.Serialize(),
            });
    }
}
