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
        public static Avatar Get(byte[] address, IEnumerable<Move.Move> moves)
        {
            if (moves == null)
            {
                throw new System.Exception();
            }
            var associatedMoves = moves.Where(m => m.UserAddress.SequenceEqual(address));
            associatedMoves = associatedMoves.SkipWhile(m => !(m is CreateNovice));
            var createMove = associatedMoves.FirstOrDefault() as CreateNovice;
            if (createMove == null)
            {
                return null;
            }

            var avatar = createMove.Execute(null).Item1;

            foreach (var move in associatedMoves.Skip(1))
            {
                avatar = move.Execute(avatar).Item1;
            }

            return avatar;
        }
    }

    public static class ClassEnum
    {
        public static string novice = "novice";
        public static string swordman = "swordman";
        public static string mage = "mage";
        public static string archer = "archer";
        public static string acolyte = "acolyte";
    }
}
