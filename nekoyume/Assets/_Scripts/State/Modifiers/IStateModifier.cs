namespace Nekoyume.State.Modifiers
{
    public interface IStateModifier<T> where T : State
    {
        bool IsEmpty { get; }
        
        void Add(IStateModifier<T> modifier);
        
        void Remove(IStateModifier<T> modifier);
        
        T Modify(T state);
    }
}
