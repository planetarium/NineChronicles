using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class LevelUp : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return null;
        }
    }
}
