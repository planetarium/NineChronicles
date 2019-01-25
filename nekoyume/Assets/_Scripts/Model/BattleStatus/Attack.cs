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

        public override void Execute(IStage stage)
        {
            stage.Attack(atk, character, target);

        }
    }
}
