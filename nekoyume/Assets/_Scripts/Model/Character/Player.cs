using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase
    {
        public long exp;
        public long expMax;
        public int stage;
        public Weapon weapon;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public SetItem set;
        public int job;

        public readonly Inventory inventory;
        public List<Inventory.InventoryItem> Items => inventory.items;

        public Player(Avatar avatar, Simulator simulator = null)
        {
            exp = avatar.EXP;
            level = avatar.Level;
            stage = avatar.WorldStage;
            Simulator = simulator;
            job = avatar.id;
            inventory = new Inventory();
            var inventoryItems = avatar.Items;
            if (inventoryItems != null)
            {
                Equip(inventoryItems);
                inventory.Set(inventoryItems);
            }

            CalcStats(level);
        }

        public void RemoveTarget(Monster monster)
        {
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
            var stats = ActionManager.Instance.tables.Character;
            var levelTable = ActionManager.Instance.tables.Level;
            var setTable = ActionManager.Instance.tables.SetEffect;
            Character data;
            stats.TryGetValue(job, out data);
            if (data == null)
            {
                throw new InvalidActionException();
            }

            Level expData;
            levelTable.TryGetValue(lv, out expData);
            if (expData == null)
            {
                throw new InvalidActionException();
            }

            var statsData = data.GetStats(lv);
            hp = statsData.HP;
            atk = statsData.Damage;
            def = statsData.Defense;
            hpMax = statsData.HP;
            expMax = expData.exp + expData.expNeed;
            criticalChance = statsData.Luck;
            var equipments = Items.Select(i => i.Item).OfType<Equipment>().Where(e => e.equipped);
            var setMap = new Dictionary<int, int>();
            foreach (var equipment in equipments)
            {
                var key = equipment.equipData.setId;
                int count;
                if (!setMap.TryGetValue(key, out count))
                {
                    setMap[key] = 0;
                }

                setMap[key] += 1;
                equipment.UpdatePlayer(this);
            }

            foreach (var pair in setMap)
            {
                var effect = ActionManager.Instance.tables.GetSetEffect(pair.Key, pair.Value);
                foreach (var e in effect)
                {
                    e.UpdatePlayer(this);
                }
            }
        }
        public void GetExp(long waveExp)
        {
            exp += waveExp;

            var levelUp = new GetExp
            {
                exp = waveExp,
                character = Copy(this),
            };
            Simulator.Log.Add(levelUp);

            if (exp < expMax)
                return;

            exp -= expMax;
            level++;

            CalcStats(level);

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
                switch (equipment.Data.cls.ToEnumItemType())
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
                        atkElement = Game.Elemental.Create((Elemental.ElementalType) equipment?.equipData.elemental);
                        defElement = Game.Elemental.Create((Elemental.ElementalType) equipment?.equipData.elemental);
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
