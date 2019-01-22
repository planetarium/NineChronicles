using System;
using System.IO;
using Nekoyume.Action;
using Nekoyume.Data.Table;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase
    {
        public long exp;
        public string items;
        public int level;
        public string name;
        public int stage;

        public Player(Avatar avatar, Simulator simulator)
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
            this.simulator = simulator;
        }

        public void GetExp(Monster monster)
        {
            exp += monster.rewardExp;
            targets.Remove(monster);
        }

        protected override void OnDead()
        {
            base.OnDead();
            simulator.isLose = true;
        }
    }
}
