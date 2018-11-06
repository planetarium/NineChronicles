using System;
using System.Linq;
using System.Runtime.InteropServices;


namespace Nekoyume.Model
{
    [System.Serializable]
    public class BattleStatus
    {
        // common
        public string type = "";
        public string id_ = "";
        public int time = 0;

        public string name = "";

        // spawn
        public string class_ = "";
        public int character_type = 0;
        public int level = 0;
        public int hp = 0;
        public int hp_max = 0;
        public string armor = "";
        public string head = "";
        public string weapon = "";

        // skill
        public string target_id = "";
        public int target_hp = 0;
        public int target_remain = 0;
        public int tick_remain = 0;

        // exp
        public int exp = 0;

        public Type GetStatusType()
        {
            string typestr = type.Split(new [] {"_"}, StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
            .Aggregate(string.Empty, (s1, s2) => s1 + s2);
            return Type.GetType(string.Format("Nekoyume.Game.Status.{0}", typestr));
        }
    }
}
