using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Dead : EventBase
    {
        public Dead(CharacterBase character) : base(character)
        {
        }
        
        public override IEnumerator CoExecute(IStage stage)
        {
            yield break;
        }
    }
}
