using System.IO;
using Nekoyume.Data.Table;

namespace Nekoyume.Model
{
    public class Player : CharacterBase
    {
        public string name;
        public int level;
        public long exp;
        public int stage;
        public string items;

        public Player(Avatar avatar)
        {
            var stats = new Table<Stats>();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/DataTable/stats.csv");
            stats.Load(File.ReadAllText(path));
            Stats data;
            stats.TryGetValue(avatar.Level, out data);
            if (data == null)
            {
                throw new InvalidActionException();
            }
            name = avatar.Name;
            exp = avatar.EXP;
            level = avatar.Level;
            stage = avatar.WorldStage;
            items = avatar.Items;
            hp = avatar.CurrentHP <= 0 ? data.Health : avatar.CurrentHP;
            atk = data.Attack;
            hpMax = data.Health;
        }

        public void GetExp(int exp)
        {
            this.exp += exp;
        }
    }
}
