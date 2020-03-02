using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class StatModifier
    {
        public enum OperationType
        {
            Add,
            Percentage
        }

        public StatType StatType { get; }
        public OperationType Operation { get; }
        public int Value { get; private set; }

        public StatModifier(StatType statType, OperationType operation, int value)
        {
            StatType = statType;
            Operation = operation;
            Value = value;
        }

        public StatModifier(StatMap statMap) : this(statMap.StatType, OperationType.Add, statMap.ValueAsInt)
        {
        }

        public StatModifier(StatMapEx statMapEx) : this(statMapEx.StatType, OperationType.Add,
            statMapEx.TotalValueAsInt)
        {
        }

        /// <summary>
        /// value와 함께 value를 바탕으로 변경시킨 값의 합을 리턴한다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetModifiedAll(int value)
        {
            return value + GetModifiedPart(value);
        }

        /// <summary>
        /// value와 함께 value를 바탕으로 변경시킨 값의 합을 리턴한다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public decimal GetModifiedAll(decimal value)
        {
            return value + GetModifiedPart(value);
        }

        /// <summary>
        /// value를 바탕으로 변경시킨 값만을 리턴한다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int GetModifiedPart(int value)
        {
            switch (Operation)
            {
                case OperationType.Add:
                    return Value;
                case OperationType.Percentage:
                    return (int) (value * Value / 100m);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// value를 바탕으로 변경시킨 값만을 리턴한다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public decimal GetModifiedPart(decimal value)
        {
            switch (Operation)
            {
                case OperationType.Add:
                    return Value;
                case OperationType.Percentage:
                    return value * Value / 100m;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// value를 변경시킨다.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Modify(IntStat value)
        {
            value.AddValue(GetModifiedPart(Value));
        }

        /// <summary>
        /// value를 변경시킨다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ignoreCurrent"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Modify(IntStatWithCurrent value, bool ignoreCurrent)
        {
            Modify(value);

            if (ignoreCurrent)
                return;

            value.AddCurrent(GetModifiedPart(Value));
        }

        /// <summary>
        /// value를 변경시킨다.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Modify(DecimalStat value)
        {
            value.AddValue(GetModifiedPart(Value));
        }

        /// <summary>
        /// value를 변경시킨다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ignoreCurrent"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Modify(DecimalStatWithCurrent value, bool ignoreCurrent)
        {
            Modify(value);

            if (ignoreCurrent)
                return;

            value.AddCurrent(GetModifiedPart(Value));
        }

        public override string ToString() =>
            (Value >= 0 ? "+" : string.Empty) +
            Value +
            (Operation == OperationType.Percentage ? "%" : string.Empty);

        #region PlayModeTest

        public void SetForTest(int value)
        {
            Value = value;
        }

        #endregion
    }
}
