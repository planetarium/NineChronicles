using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Attack : EventBase
    {
        public int atk;
        public bool critical;

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAttack(atk, character, target, critical);
        }
    }
}
