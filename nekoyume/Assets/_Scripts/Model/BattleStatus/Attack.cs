using System;
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

        public override bool skip => false;

        public override void Execute(IStage stage)
        {
            stage.Attack(atk, character, target, critical);

        }
    }
}
