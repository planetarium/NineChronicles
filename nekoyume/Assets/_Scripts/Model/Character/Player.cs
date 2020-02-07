using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Inventory = Nekoyume.Model.Item.Inventory;

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

            public void Set(CharacterLevelSheet.Row row)
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
        public WorldInformation worldInformation;
        
        public Weapon weapon;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public SetItem set;
        
        public CollectionMap monsterMap;
        public CollectionMap eventMap;

        // todo: `PlayerCostume` 정도의 객체로 분리.
        public int hairIndex;
        public int lensIndex;
        public int earIndex;
        public int tailIndex;

        private List<Equipment> _equipments;

        public IReadOnlyList<Equipment> Equipments => _equipments;

        public Player(AvatarState avatarState, Simulator simulator) 
            : base(
                  simulator, 
                  simulator.TableSheets,
                  avatarState.characterId, 
                  avatarState.level)
        {
            if (simulator is null)
                throw new ArgumentNullException(nameof(simulator));
            
            // FIXME 중복 코드 제거할 것
            Exp.Current = avatarState.exp;
            Inventory = avatarState.inventory;
            worldInformation = avatarState.worldInformation;
            weapon = null;
            armor = null;
            belt = null;
            necklace = null;
            ring = null;
            helm = null;
            set = null;
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = avatarState.hair;
            lensIndex = avatarState.lens;
            earIndex = avatarState.ear;
            tailIndex = avatarState.tail;
            PostConstruction(simulator.TableSheets);
        }

        public Player(AvatarState avatarState, TableSheets tableSheets) 
            : base (
                  null, 
                  tableSheets,
                  avatarState.characterId, 
                  avatarState.level)
        {
            // FIXME 중복 코드 제거할 것
            Exp.Current = avatarState.exp;
            Inventory = avatarState.inventory;
            worldInformation = avatarState.worldInformation;
            weapon = null;
            armor = null;
            belt = null;
            necklace = null;
            ring = null;
            helm = null;
            set = null;
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = avatarState.hair;
            lensIndex = avatarState.lens;
            earIndex = avatarState.ear;
            tailIndex = avatarState.tail;
            PostConstruction(tableSheets);
        }

        public Player(int level, TableSheets tableSheets) : 
            base(
                null,
                tableSheets,
                GameConfig.DefaultAvatarCharacterId, 
                level)
        {
            Exp.Current = 0;
            Inventory = new Inventory();
            worldInformation = null;
            weapon = null;
            armor = null;
            belt = null;
            necklace = null;
            ring = null;
            helm = null;
            set = null;
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = 0;
            lensIndex = 0;
            earIndex = 0;
            tailIndex = 0;
            PostConstruction(tableSheets);
        }

        protected Player(Player value) : base(value)
        {
            Exp = (ExpData) value.Exp.Clone();
            Inventory = value.Inventory;
            worldInformation = value.worldInformation;
            weapon = value.weapon;
            armor = value.armor;
            belt = value.belt;
            necklace = value.necklace;
            ring = value.ring;
            helm = value.helm;
            set = value.set;
            monsterMap = value.monsterMap;
            eventMap = value.eventMap;
            hairIndex = value.hairIndex;
            lensIndex = value.lensIndex;
            earIndex = value.earIndex;
            tailIndex = value.tailIndex;

            _equipments = value._equipments;
        }
        
        public override bool IsHit(CharacterBase caster)
        {
            return true;
        }

        private void PostConstruction(TableSheets sheets)
        {
            AttackCountMax = AttackCountHelper.GetCountMax(Level);
            UpdateExp(sheets);
            Equip(Inventory.Items, sheets.EquipmentItemSetEffectSheet);
        }

        private void UpdateExp(TableSheets sheets)
        {
            sheets.CharacterLevelSheet.TryGetValue(Level, out var row, true);
            Exp.Set(row);
        }

        public void RemoveTarget(Enemy enemy)
        {
            monsterMap.Add(new KeyValuePair<int, int>(enemy.RowData.Id, 1));
            Targets.Remove(enemy);
            Simulator.Characters.TryRemove(enemy);
        }

        public void RemoveTarget(EnemyPlayer enemy)
        {
            Targets.Remove(enemy);
            Simulator.Characters.TryRemove(enemy);
        }

        protected override void OnDead()
        {
            base.OnDead();
            eventMap.Add(new KeyValuePair<int, int>((int) QuestEventType.Die, 1));
            Simulator.Lose = true;
        }
        
        private void Equip(IEnumerable<Inventory.Item> items, EquipmentItemSetEffectSheet sheet)
        {
            _equipments = items.Select(i => i.item)
                .OfType<Equipment>()
                .Where(e => e.equipped)
                .ToList();
            foreach (var equipment in _equipments)
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
            
            Stats.SetEquipments(_equipments, sheet);

            foreach (var skill in _equipments.SelectMany(equipment => equipment.Skills))
            {
                Skills.Add(skill);
            }
            
            foreach (var buffSkill in _equipments.SelectMany(equipment => equipment.BuffSkills))
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

            var level = Level;
            Level = Simulator.TableSheets.CharacterLevelSheet.GetLevel(Exp.Current);
            // UI에서 레벨업 처리시 NRE 회피
            if (level < Level)
            {
                eventMap?.Add(new KeyValuePair<int, int>((int) QuestEventType.Level, Level - level));
            }
            UpdateExp(Simulator.TableSheets);
        }

        // ToDo. 지금은 스테이지에서 재료 아이템만 주고 있음. 추후 대체 불가능 아이템도 줄 경우 수정 대상.
        public CollectionMap GetRewards(List<ItemBase> items)
        {
            var map = new CollectionMap();
            foreach (var item in items)
            {
                map.Add(Inventory.AddItem(item));
            }

            return map;
        }

        public virtual void Spawn()
        {
            InitAI();
            var spawn = new SpawnPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
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

        public void OverrideSkill(Skill.Skill skill)
        {
            Skills.Clear();
            Skills.Add(skill);
        }

        public override object Clone()
        {
            return new Player(this);
        }

        protected override void EndTurn()
        {
            base.EndTurn();
            if (this is EnemyPlayer)
            {
                return;
            }
            Simulator.WaveTurn++;
            Simulator.Log.Add(new WaveTurnEnd(this, Simulator.WaveTurn, Simulator.Turn));
        }
    }

    public class InvalidEquipmentException : Exception
    {
    }
}
