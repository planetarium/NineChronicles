using System;
using Nekoyume.EnumType;

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
    }
}
