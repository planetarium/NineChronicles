using System;
using System.Collections;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class Attack : EventBase
    {
        public int atk;
        public bool critical;

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAttack(atk, character, target, critical);
        }
    }
}
