using System;

namespace Nekoyume.Model
{
    [Serializable]
    public class Spawn : EventBase
    {
        public override bool skip => false;

        public override void Execute(IStage stage)
        {
            if (character is Player)
            {
                stage.SpawnPlayer((Player)character);
            }
            else if (character is Monster)
            {
                stage.SpawnMonster((Monster)character);
            }
        }
    }
}
