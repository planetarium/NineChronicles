using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class DecimalStat : ICloneable
    {
        private decimal _value;

        public readonly StatType Type;
        public decimal Value
        {
            get => _value;
            private set
            {
                _value = value;
                ValueAsInt = (int)_value;
            }
        }
        public int ValueAsInt { get; private set; }

        public DecimalStat(StatType type, decimal value = 0m)
        {
            Type = type;
            Value = value;
        }

        public virtual void Reset()
        {
            Value = 0m;
        }

        protected DecimalStat(DecimalStat value)
        {
            Type = value.Type;
            Value = value.Value;
        }

        public void SetValue(decimal value)
        {
            Value = value;
        }

        public void AddValue(decimal value)
        {
            SetValue(Value + value);
        }

        public virtual object Clone()
        {
            return new DecimalStat(this);
        }

        protected bool Equals(DecimalStat other)
        {
            return _value == other._value && Type == other.Type;
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
            unchecked
            {
                return (_value.GetHashCode() * 397) ^ (int) Type;
            }
        }
    }
}
