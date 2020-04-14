using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnEnemyPlayer : EventBase
    {
        public SpawnEnemyPlayer(CharacterBase character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnEnemyPlayer((EnemyPlayer)Character);
        }
    }
}
