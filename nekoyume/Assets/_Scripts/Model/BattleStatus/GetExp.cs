using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class GetExp : EventBase
    {
        public long exp;
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetExp(exp);
        }
    }
}
