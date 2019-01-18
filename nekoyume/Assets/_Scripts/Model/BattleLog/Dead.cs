using System;

namespace Nekoyume.Model.BattleLog
{
    [Serializable]
    public class Dead : LogBase
    {
        public string character;

        public Dead(CharacterBase character)
        {
            this.character = character.ToString();
        }
    }
}
