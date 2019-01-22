using System;

namespace Nekoyume.Model.BattleLog
{
    [Serializable]
    public class Spawn : LogBase
    {
        public CharacterBase character;

        public Spawn(CharacterBase character)
        {
            this.character = character;
        }
    }
}
