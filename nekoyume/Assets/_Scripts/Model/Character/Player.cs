using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.State;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase
    {
        public long exp;
        public long expMax;
        public long expNeed;
        public int stage;
        public Weapon weapon;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public SetItem set;
        public int job;
        public sealed override float TurnSpeed { get; set; }

        public readonly Inventory inventory;
        public List<Inventory.InventoryItem> Items => inventory.items;
        public List<Equipment> Equipments =>
            inventory.items.Select(i => i.Item).OfType<Equipment>().Where(e => e.equipped).ToList();
        
        public Player(AvatarState avatarState, Simulator simulator = null)
        {
            Simulator = simulator;
            
            level = avatarState.level;
            exp = avatarState.exp;
            job = avatarState.id;
            stage = avatarState.worldStage;
            
            inventory = new Inventory();
            atkElement = Game.Elemental.Create(Elemental.ElementalType.Normal);
            defElement = Game.Elemental.Create(Elemental.ElementalType.Normal);
            TurnSpeed = 1.8f;

            var inventoryItems = avatarState.items;
            if (inventoryItems != null)
            {
                Equip(inventoryItems);
                inventory.Set(inventoryItems);
            }

            CalcStats(level);
        }

        public Player()
        {
            level = 1;
            exp = 0;
            job = 100010;
            stage = 1;
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

        private void CalcStats(int lv)
        {
            var stats = Tables.instance.Character;
            var levelTable = Tables.instance.Level;
            stats.TryGetValue(job, out var data);
            if (data == null)
            {
                throw new InvalidActionException();
            }

            levelTable.TryGetValue(lv, out var expData);
            if (expData == null)
            {
                throw new InvalidActionException();
            }

            var statsData = data.GetStats(lv);
            currentHP = statsData.HP;
            atk = statsData.Damage;
            def = statsData.Defense;
            hp = statsData.HP;
            expMax = expData.exp + expData.expNeed;
            expNeed = expData.expNeed;
            luck = statsData.Luck;
            attackRange = data.attackRange;
            runSpeed = data.runSpeed;
            var setMap = new Dictionary<int, int>();
            foreach (var equipment in Equipments)
            {
                var key = equipment.Data.setId;
                if (!setMap.TryGetValue(key, out _))
                {
                    setMap[key] = 0;
                }

                setMap[key] += 1;
                equipment.UpdatePlayer(this);
            }

            foreach (var pair in setMap)
            {
                var effect = Tables.instance.GetSetEffect(pair.Key, pair.Value);
                foreach (var e in effect)
                {
                    e.UpdatePlayer(this);
                }
            }
        }
        public void GetExp(long waveExp, bool log = false)
        {
            exp += waveExp;

            if (log)
            {
                var getExp = new GetExp
                {
                    exp = waveExp,
                    character = (CharacterBase) Clone(),
                };
                Simulator.Log.Add(getExp);
            }

            if (exp < expMax)
                return;

            LevelUp();
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
                        atkElement = Game.Elemental.Create((Elemental.ElementalType) equipment?.Data.elemental);
                        defElement = Game.Elemental.Create((Elemental.ElementalType) equipment?.Data.elemental);
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
                character = (CharacterBase) Clone(),
            };
            Simulator.Log.Add(spawn);
        }

        private void LevelUp()
        {
            var levelTable = Tables.instance.Level;
            Level expData;
            var row = levelTable.First(r => r.Value.exp + r.Value.expNeed > exp);
            levelTable.TryGetValue(row.Key, out expData);
            if (expData == null)
            {
                throw new InvalidActionException();
            }

            level = expData.level;
        }

        public float GetAdditionalStatus(string key)
        {
            var stats = Tables.instance.Character;
            var levelTable = Tables.instance.Level;
            Character data;
            stats.TryGetValue(job, out data);
            if (data == null)
            {
                throw new KeyNotFoundException($"invalid character id: `{job}`.");
            }

            Level expData;
            levelTable.TryGetValue(level, out expData);
            if (expData == null)
            {
                throw new KeyNotFoundException("invalid character level.");
            }

            var statsData = data.GetStats(level);

            float value;
            switch (key)
            {
                case "atk":
                    value = atk - statsData.Damage;
                    break;
                case "def":
                    value = def - statsData.Defense;
                    break;
                case "hp":
                    value = hp - statsData.HP;
                    break;
                case "luck":
                    value = (luck - statsData.Luck) * 100;
                    break;
                default:
                    throw new InvalidCastException($"invalid status key: `{key}`.");
            }

            return value;
        }

        public IEnumerable<string> GetOptions()
        {
            if (set != null)
            {
                var aStrong = atkElement.Data.strong;
                var aWeak = atkElement.Data.weak;
                var dStrong = defElement.Data.strong;
                var dWeak = defElement.Data.weak;
                var aMultiply = atkElement.Data.multiply * 100;
                var dMultiply = defElement.Data.multiply * 100;
                if (aMultiply > 0)
                {
                    yield return $"{Elemental.GetDescription(aStrong)} 공격 +{aMultiply}%";
                    yield return $"{Elemental.GetDescription(aWeak)} 공격 -{aMultiply}%";
                }

                if (dMultiply > 0)
                {
                    yield return $"{Elemental.GetDescription(dStrong)} 방어 +{dMultiply}%";
                    yield return $"{Elemental.GetDescription(dWeak)} 방어 -{dMultiply}%";
                }
            }
        }

        public void Use(List<Food> foods)
        {
            foreach (var food in foods)
            {
                food.Use(this);
                inventory.Remove(food);
            }
        }
    }

    public class InvalidEquipmentException : Exception
    {
    }
}
