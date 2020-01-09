using System;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public abstract class AgentStateModifier : IStateModifier<AgentState>
    {
        public abstract bool IsEmpty { get; }
        
        public abstract void Add(IStateModifier<AgentState> modifier);
        
        public abstract void Remove(IStateModifier<AgentState> modifier);

        public abstract AgentState Modify(AgentState state);
    }
}
