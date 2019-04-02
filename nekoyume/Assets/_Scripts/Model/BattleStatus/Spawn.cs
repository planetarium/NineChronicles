using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Spawn : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            if (character is Player)
            {
                yield return stage.CoSpawnPlayer((Player)character);
            }
            else if (character is Monster)
            {
                yield return stage.CoSpawnMonster((Monster)character);
            }
        }
    }
}
