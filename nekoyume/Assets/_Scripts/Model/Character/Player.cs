using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public Weapon weapon;
        [NonSerialized]
        public Inventory inventory;
        public string Items => JsonConvert.SerializeObject(inventory._items);

        public Player(Avatar avatar, Simulator simulator)
        {
            name = avatar.Name;
            exp = avatar.EXP;
            level = avatar.Level;
            stage = avatar.WorldStage;
            items = avatar.Items;
            this.simulator = simulator;
            inventory = new Inventory();
            if (!string.IsNullOrEmpty(avatar.Items))
            {
                var inventoryItems = JsonConvert.DeserializeObject<List<Inventory.InventoryItem>>(avatar.Items);
                foreach (var inventoryItem in inventoryItems)
                {
                    if (inventoryItem.Item is Weapon)
                    {
                        weapon = (Weapon) inventoryItem.Item;
                    }
                }
                inventory.Set(inventoryItems);
            }
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

        public void GetItem(ItemBase item)
        {
            inventory.Add(item);
        }
    }
}
