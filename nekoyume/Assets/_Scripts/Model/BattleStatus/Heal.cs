using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Heal: Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoHeal(character, skillInfos);
        }

    }
}
