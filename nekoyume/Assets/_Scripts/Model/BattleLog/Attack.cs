using System;

namespace Nekoyume.Model.BattleLog
{
    [Serializable]
    public class Attack : LogBase
    {
        public string from;
        public string to;
        public int dmg;

        public Attack(CharacterBase from, CharacterBase target, int atk)
        {
            this.from = from.ToString();
            to = target.ToString();
            dmg = atk;
        }
    }
}
