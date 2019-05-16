using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Attack : Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAttack(character, skillInfos);
        }
    }
}
