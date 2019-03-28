using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class EventBase
    {
        public CharacterBase character;
        public CharacterBase target;

        public abstract IEnumerator CoExecute(IStage stage);
    }
}
