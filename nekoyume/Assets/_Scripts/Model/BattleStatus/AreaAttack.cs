using System.Collections;

namespace Nekoyume.Model
{
    public class AreaAttack : Skill
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAreaAttack(character, skillInfos);
        }
    }
}
