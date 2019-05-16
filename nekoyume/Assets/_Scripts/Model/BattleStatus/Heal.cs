using System.Collections;

namespace Nekoyume.Model
{
    public class Heal: Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoHeal(character, skillInfos);
        }

    }
}
