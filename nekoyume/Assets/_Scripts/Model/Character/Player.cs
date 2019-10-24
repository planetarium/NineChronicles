using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase, ICloneable
    {
        [Serializable]
        public class ExpData : ICloneable
        {
            public long Max { get; private set; }
            public long Need { get; private set; }
            public long Current { get; set; }

            public ExpData()
            {
            }

            protected ExpData(ExpData value)
            {
                Max = value.Max;
                Need = value.Need;
                Current = value.Current;
            }

            public void Set(LevelSheet.Row row)
            {
                Max = row.Exp + row.ExpNeed;
                Need = row.ExpNeed;
            }

            public object Clone()
            {
                return new ExpData(this);
            }
        }

        public readonly ExpData Exp = new ExpData();
        public readonly Inventory Inventory;
        public int worldStage;
        
        public Weapon weapon;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public SetItem set;
        public CollectionMap monsterMap;

        private List<Equipment> Equipments { get; set; }

        public Player(AvatarState avatarState, Simulator simulator = null) : base(simulator, avatarState.characterId, avatarState.level)
        {
            Exp.Current = avatarState.exp;
            Inventory = avatarState.inventory;
            worldStage = avatarState.worldStage;
            monsterMap = new CollectionMap();
            PostConstruction();
        }

        public Player(int level) : base(null, GameConfig.DefaultAvatarCharacterId, level)
        {
            Exp.Current = 0;
            Inventory = new Inventory();
            worldStage = 1;
            PostConstruction();
        }

        protected Player(Player value) : base(value)
        {
            Exp = (ExpData) value.Exp.Clone();
            Inventory = value.Inventory;
            worldStage = value.worldStage;
            
            weapon = value.weapon;
            armor = value.armor;
            belt = value.belt;
            necklace = value.necklace;
            ring = value.ring;
            helm = value.helm;
            set = value.set;

            Equipments = value.Equipments;
        }

        private void PostConstruction()
        {
            UpdateExp();
            Equip(Inventory.Items);
        }

        private void UpdateExp()
        {
            Game.Game.instance.TableSheets.LevelSheet.TryGetValue(Level, out var row, true);
            Exp.Set(row);
        }

        public void RemoveTarget(Enemy enemy)
        {
            monsterMap.Add(new KeyValuePair<int, int>(enemy.RowData.Id, 1));
            Targets.Remove(enemy);
            Simulator.Characters.TryRemove(enemy);
        }

        protected override void OnDead()
        {
            base.OnDead();
            Simulator.Lose = true;
        }
        
        private void Equip(IEnumerable<Inventory.Item> items)
        {
            Equipments = items.Select(i => i.item)
                .OfType<Equipment>()
                .Where(e => e.equipped)
                .ToList();
            foreach (var equipment in Equipments)
            {
                switch (equipment.Data.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        weapon = equipment as Weapon;
                        break;
                    case ItemSubType.RangedWeapon:
                        weapon = equipment as RangedWeapon;
                        break;
                    case ItemSubType.Armor:
                        armor = equipment as Armor;
                        defElementType = equipment.Data.ElementalType;
                        break;
                    case ItemSubType.Belt:
                        belt = equipment as Belt;
                        break;
                    case ItemSubType.Necklace:
                        necklace = equipment as Necklace;
                        break;
                    case ItemSubType.Ring:
                        ring = equipment as Ring;
                        break;
                    case ItemSubType.Helm:
                        helm = equipment as Helm;
                        break;
                    case ItemSubType.Set:
                        set = equipment as SetItem;
                        break;
                    default:
                        throw new InvalidEquipmentException();
                }
            }
            
            Stats.SetEquipments(Equipments);

            foreach (var skill in Equipments.SelectMany(equipment => equipment.Skills))
            {
                Skills.Add(skill);
            }
            
            foreach (var buffSkill in Equipments.SelectMany(equipment => equipment.BuffSkills))
            {
                Skills.Add(buffSkill);
            }
        }

        public void GetExp(long waveExp, bool log = false)
        {
            Exp.Current += waveExp;

            if (log)
            {
                var getExp = new GetExp((CharacterBase) Clone(), waveExp);
                Simulator.Log.Add(getExp);
            }

            if (Exp.Current < Exp.Max)
                return;

            Level = Game.Game.instance.TableSheets.LevelSheet.GetLevel(Exp.Current);
            UpdateExp();
        }

        // ToDo. 지금은 스테이지에서 재료 아이템만 주고 있음. 추후 대체 불가능 아이템도 줄 경우 수정 대상.
        public void GetRewards(List<ItemBase> items)
        {
            foreach (var item in items)
            {
                Inventory.AddFungibleItem(item);
            }
        }

        public void Spawn()
        {
            InitAI();
            var spawn = new SpawnPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        public IEnumerable<(StatType statType, int value, int additionalValue)> GetStatTuples()
        {
            if (Stats.HasHP)
                yield return (StatType.HP, Stats.LevelStats.HP, Stats.AdditionalHP);

            if (Stats.HasATK)
                yield return (StatType.ATK, Stats.LevelStats.ATK, Stats.AdditionalATK);
            
            if (Stats.HasDEF)
                yield return (StatType.DEF, Stats.LevelStats.DEF, Stats.AdditionalDEF);
            
            if (Stats.HasCRI)
                yield return (StatType.CRI, Stats.LevelStats.CRI, Stats.AdditionalCRI);
            
            if (Stats.HasDOG)
                yield return (StatType.DOG, Stats.LevelStats.DOG, Stats.AdditionalDOG);
            
            if (Stats.HasSPD)
                yield return (StatType.SPD, Stats.LevelStats.SPD, Stats.AdditionalSPD);
        }

        public IEnumerable<string> GetOptions()
        {
            var atkOptions = atkElementType.GetOptions(StatType.ATK);
            foreach (var atkOption in atkOptions)
            {
                yield return atkOption;
            }

            var defOptions = defElementType.GetOptions(StatType.DEF);
            foreach (var defOption in defOptions)
            {
                yield return defOption;
            }
        }

        public void Use(List<Consumable> foods)
        {
            Stats.SetConsumables(foods);
            foreach (var food in foods)
            {
                foreach (var skill in food.Skills)
                {
                    Skills.Add(skill);
                }

                foreach (var buffSkill in food.BuffSkills)
                {
                    BuffSkills.Add(buffSkill);
                }
                
                Inventory.RemoveNonFungibleItem(food);
            }
        }

        public void OverrideSkill(Game.Skill skill)
        {
            Skills.Clear();
            Skills.Add(skill);
        }

        public override object Clone()
        {
            return new Player(this);
        }
    }

    public class InvalidEquipmentException : Exception
    {
    }
}
