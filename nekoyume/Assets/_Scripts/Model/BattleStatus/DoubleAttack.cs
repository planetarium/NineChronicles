using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class DoubleAttack : Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoDoubleAttack(character, skillInfos);
        }
    }
}
