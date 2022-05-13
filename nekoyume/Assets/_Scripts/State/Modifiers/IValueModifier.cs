namespace Nekoyume.State.Modifiers
{
    public interface IValueModifier<T> where T : struct
    {
        bool IsEmpty { get; }
        T Modify(T value);
    }

    public interface IAccumulatableValueModifier<T> : IValueModifier<T> where T : struct
    {
        void Add(IAccumulatableValueModifier<T> modifier);
        void Remove(IAccumulatableValueModifier<T> modifier);
    }
}
