using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model
{
    [Serializable]
    public class AreaAttack : EventBase
    {
        public List<Attack.AttackInfo> infos;
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAreaAttack(character, infos);
        }
    }

}
