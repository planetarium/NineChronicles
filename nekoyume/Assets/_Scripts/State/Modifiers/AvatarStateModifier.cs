using System;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public abstract class AvatarStateModifier : IStateModifier<AvatarState>
    {
        public abstract bool IsEmpty { get; }
        
        public abstract void Add(IStateModifier<AvatarState> modifier);
        
        public abstract void Remove(IStateModifier<AvatarState> modifier);

        public abstract AvatarState Modify(AvatarState state);
    }
}
