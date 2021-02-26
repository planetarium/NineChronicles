using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class IntStat : ICloneable
    {
        public readonly StatType Type;
        public int Value { get; private set; }

        public IntStat()
        {
        }

        protected IntStat(IntStat value)
        {
            Type = value.Type;
            Value = value.Value;
        }

        public IntStat(StatType type, int value = 0)
        {
            Type = type;
            Value = value;
        }

        public virtual void Reset()
        {
            Value = 0;
        }

        public void SetValue(int value)
        {
            Value = value;
        }

        public void AddValue(int value)
        {
            SetValue(Value + value);
        }

        public void AddValue(float value)
        {
            AddValue((int)value);
        }

        public virtual object Clone()
        {
            return new IntStat(this);
        }
    }
}
