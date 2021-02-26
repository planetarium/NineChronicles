using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class DecimalStatWithCurrent : DecimalStat, ICloneable
    {
        private decimal _current;

        public decimal Current
        {
            get => _current;
            set
            {
                _current = value;
                CurrentAsInt = (int)_current;
            }
        }
        public int CurrentAsInt { get; private set; }

        public DecimalStatWithCurrent(StatType type, decimal value = 0m, decimal current = 0m) : base(type, value)
        {
            Current = current;
        }

        protected DecimalStatWithCurrent(DecimalStatWithCurrent value) : base(value)
        {
            Current = value.Current;
        }

        public override void Reset()
        {
            base.Reset();
            Current = 0m;
        }

        public void SetValueAndCurrent(decimal value)
        {
            SetValue(value);
            SetCurrent(value);
        }

        public void AddValueAndCurrent(decimal value)
        {
            SetValueAndCurrent(Current + value);
        }

        public void SetCurrent(decimal value)
        {
            Current = Math.Min(Math.Max(0, value), Value);
        }

        public void AddCurrent(decimal value)
        {
            SetCurrent(Current + value);
        }

        public void EqualizeCurrentWithValue()
        {
            SetCurrent(Value);
        }

        public override object Clone()
        {
            return new DecimalStatWithCurrent(this);
        }
    }
}
