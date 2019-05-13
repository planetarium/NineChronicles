using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class EventBase
    {
        public CharacterBase character;

        public abstract IEnumerator CoExecute(IStage stage);
    }
}
