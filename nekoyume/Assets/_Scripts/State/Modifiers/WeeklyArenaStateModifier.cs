using System;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public abstract class WeeklyArenaStateModifier : IAccumulatableStateModifier<WeeklyArenaState>
    {
        public bool dirty { get; set; }
        public abstract bool IsEmpty { get; }
        public abstract WeeklyArenaState Modify(WeeklyArenaState state);
        public abstract void Add(IAccumulatableStateModifier<WeeklyArenaState> modifier);
        public abstract void Remove(IAccumulatableStateModifier<WeeklyArenaState> modifier);
    }
}
