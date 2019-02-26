using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;

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
        public readonly Inventory inventory;
        public List<Inventory.InventoryItem> Items => inventory.items;

        public Player(Avatar avatar, Simulator simulator = null)
        {
            name = avatar.Name;
            exp = avatar.EXP;
            level = avatar.Level;
            stage = avatar.WorldStage;
            this.simulator = simulator;
            inventory = new Inventory();
            var inventoryItems = avatar.Items;
            if (inventoryItems != null)
            {
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
            var stats = ActionManager.Instance.tables.Stats;
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
            simulator.Log.Add(levelUp);
        }

        public void GetItem(ItemBase item)
        {
            inventory.Add(item);
        }
    }
}
