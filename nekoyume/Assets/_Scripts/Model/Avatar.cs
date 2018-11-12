using System.Collections.Generic;
using System.Linq;
using Nekoyume.Move;

namespace Nekoyume.Model
{
    [System.Serializable]
    public class Avatar
    {
        public string name;
        public string class_;
        public int level;
        public int gold;
        public int exp;
        public int exp_max;
        public int hp;
        public int hp_max;
        public int strength;
        public int dexterity;
        public int intelligence;
        public int constitution;
        public int luck;
        public string[] items;
        public string zone;
        public byte[] user;
        public bool dead
        {
            get
            {
                return hp <= 0;
            }
        }
    }

    public enum CharacterClass
    {
        Novice,
        Swordman,
        Mage,
        Archer,
        Acolyte
    }
}
