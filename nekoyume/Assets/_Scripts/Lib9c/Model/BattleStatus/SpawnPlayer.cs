using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnPlayer : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnPlayer((Player)Character);
        }

        public SpawnPlayer(CharacterBase character) : base(character)
        {
        }
    }
}
