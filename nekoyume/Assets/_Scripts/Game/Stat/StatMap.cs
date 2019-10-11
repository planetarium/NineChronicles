using System;
using Nekoyume.EnumType;

namespace Nekoyume.Game
{
    [Serializable]
    public class StatMap
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
    }
}
