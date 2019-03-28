using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class EventBase
    {
        public CharacterBase character;
        public CharacterBase target;
        public Guid characterId;
        public Guid targetId;

        public abstract IEnumerator CoExecute(IStage stage);
    }
}
