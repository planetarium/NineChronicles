using System;

namespace Nekoyume.Model.BattleLog
{
    [Serializable]
    public class Spawn : LogBase
    {
        public string character;

        public Spawn(CharacterBase charcater)
        {
            this.character = charcater.ToString();
        }

    }
}
