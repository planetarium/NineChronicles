using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class IntStatWithCurrent : IntStat, ICloneable
    {
        public int Current { get; private set; }

        public IntStatWithCurrent(StatType type, int value = 0, int current = 0) : base(type, value)
        {
            Current = current;
        }

        protected IntStatWithCurrent(IntStatWithCurrent value) : base(value)
        {
            Current = value.Current;
        }

        public override void Reset()
        {
            base.Reset();
            Current = 0;
        }

        public void SetValueAndCurrent(int value)
        {
            SetValue(value);
            SetCurrent(value);
        }

        public void AddValueAndCurrent(int value)
        {
            SetValueAndCurrent(Value + value);
        }

        public void SetCurrent(int value)
        {
            Current = Math.Min(Math.Max(0, value), Value);
        }

        public void AddCurrent(int value)
        {
            SetCurrent(Current + value);
        }

        public void AddCurrent(float value)
        {
            AddCurrent((int)value);
        }

        public void EqualizeCurrentWithValue()
        {
            SetCurrent(Value);
        }

        public override object Clone()
        {
            return new IntStatWithCurrent(this);
        }
    }
}
