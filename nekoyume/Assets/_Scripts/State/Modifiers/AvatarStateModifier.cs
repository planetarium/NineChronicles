using System;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public abstract class AvatarStateModifier : IAccumulatableStateModifier<AvatarState>
    {
        public bool dirty { get; set; }
        public abstract bool IsEmpty { get; }
        public abstract void Add(IAccumulatableStateModifier<AvatarState> modifier);
        public abstract void Remove(IAccumulatableStateModifier<AvatarState> modifier);
        public abstract AvatarState Modify(AvatarState state);
    }
}
