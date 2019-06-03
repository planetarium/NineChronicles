using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Blow : Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBlow(character, skillInfos);
        }
    }
}
