using System;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class EventBase
    {
        public CharacterBase character;
        public CharacterBase target;
        public Guid characterId;
        public Guid targetId;
        public abstract bool skip { get; }

        public abstract void Execute(IStage stage);
    }
}
