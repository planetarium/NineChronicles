using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class AreaAttack : Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAreaAttack(character, skillInfos);
        }
    }
}
