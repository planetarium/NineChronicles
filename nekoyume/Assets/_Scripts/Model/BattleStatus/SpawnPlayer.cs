using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class SpawnPlayer : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnPlayer((Player) character);
        }
    }
}
