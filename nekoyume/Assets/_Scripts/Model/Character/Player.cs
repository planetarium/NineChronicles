using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Data.Table;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase
    {
        public long exp;
        public long expMax;
        public int level;
        public int stage;
        public Weapon weapon;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public SetItem set;

        public readonly Inventory inventory;
        public List<Inventory.InventoryItem> Items => inventory.items;

        public Player(Avatar avatar, Simulator simulator = null)
        {
            exp = avatar.EXP;
            level = avatar.Level;
            stage = avatar.WorldStage;
            Simulator = simulator;
            inventory = new Inventory();
            var inventoryItems = avatar.Items;
            if (inventoryItems != null)
            {
                Equip(inventoryItems);
                inventory.Set(inventoryItems);
            }

            var elemental = set?.Data.elemental ?? Data.Table.Elemental.ElementalType.Normal;
            atkElement = Game.Elemental.Create(elemental);
            defElement = Game.Elemental.Create(elemental);
            CalcStats(level);
        }

        public void GetExp(Monster monster)
        {
            exp += monster.rewardExp;
            while (expMax <= exp)
            {
                LevelUp();
            }
            targets.Remove(monster);
            Simulator.Characters.Remove(monster);
        }

        protected override void OnDead()
        {
            base.OnDead();
            Simulator.Lose = true;
        }

        public void CalcStats(int lv)
        {
            var stats = ActionManager.Instance.tables.Stats;
            Stats data;
            stats.TryGetValue(lv, out data);
            if (data == null)
            {
                throw new InvalidActionException();
            }
            hp = data.Health;
            atk = data.Attack;
            def = data.Defense;
            hpMax = data.Health;
            expMax = data.Exp;
            criticalChance = data.critical;
            var equipments = Items.Select(i => i.Item).OfType<Equipment>().Where(e => e.equipped);
            foreach (var equipment in equipments) equipment.UpdatePlayer(this);
        }
        private void LevelUp()
        {
            if (exp < expMax)
                return;

            exp -= expMax;
            level++;

            CalcStats(level);

            var levelUp = new LevelUp
            {
                character = Copy(this),
            };
            Simulator.Log.Add(levelUp);
        }

        public void GetRewards(List<ItemBase> items)
        {
            foreach (var item in items)
            {
                inventory.Add(item);
            }
        }

        public void Equip(List<Inventory.InventoryItem> items)
        {
            var equipments = items.Select(i => i.Item).OfType<Equipment>().Where(e => e.equipped);
            foreach (var equipment in equipments)
            {
                switch (equipment.Data.Cls.ToEnumItemType())
                {
                    case ItemBase.ItemType.Weapon:
                        weapon = equipment as Weapon;
                        break;
                    case ItemBase.ItemType.RangedWeapon:
                        weapon = equipment as RangedWeapon;
                        break;
                    case ItemBase.ItemType.Armor:
                        armor = equipment as Armor;
                        break;
                    case ItemBase.ItemType.Belt:
                        belt = equipment as Belt;
                        break;
                    case ItemBase.ItemType.Necklace:
                        necklace = equipment as Necklace;
                        break;
                    case ItemBase.ItemType.Ring:
                        ring = equipment as Ring;
                        break;
                    case ItemBase.ItemType.Helm:
                        helm = equipment as Helm;
                        break;
                    case ItemBase.ItemType.Set:
                        set = equipment as SetItem;
                        break;
                    default:
                        throw new InvalidEquipmentException();
                }
            }
        }
        public void Spawn()
        {
            InitAI();
            var spawn = new SpawnPlayer
            {
                character = Copy(this),
            };
            Simulator.Log.Add(spawn);
        }
    }

    public class InvalidEquipmentException : Exception
    {
    }
}
