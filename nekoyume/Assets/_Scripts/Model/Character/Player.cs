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
        public long expMax;
        public string items;
        public int level;
        public string name;
        public int stage;

        public Player(Avatar avatar, Simulator simulator)
        {
            name = avatar.Name;
            exp = avatar.EXP;
            level = avatar.Level;
            stage = avatar.WorldStage;
            items = avatar.Items;
            this.simulator = simulator;
            CalcStats();
        }

        public void GetExp(Monster monster)
        {
            exp += monster.rewardExp;
            while (expMax <= exp)
            {
                LevelUp();
            }
            targets.Remove(monster);
        }

        protected override void OnDead()
        {
            base.OnDead();
            simulator.isLose = true;
        }


        private void CalcStats()
        {
            var stats = new Table<Stats>();
            var path = Path.Combine(Directory.GetCurrentDirectory(), Simulator.StatsPath);
            stats.Load(File.ReadAllText(path));
            Stats data;
            stats.TryGetValue(level, out data);
            if (data == null)
            {
                throw new InvalidActionException();
            }
            hp = data.Health;
            atk = data.Attack;
            hpMax = data.Health;
            expMax = data.Exp;

        }
        private void LevelUp()
        {
            if (exp < expMax)
                return;

            exp -= expMax;
            level++;

            CalcStats();

            var levelUp = new LevelUp
            {
                character = Copy(this),
            };
            simulator.log.Add(levelUp);
        }
    }
}
