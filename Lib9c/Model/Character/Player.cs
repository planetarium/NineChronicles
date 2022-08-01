using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase
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

        public CollectionMap monsterMap;
        public CollectionMap eventMap;

        // todo: `PlayerCostume` 정도의 객체로 분리.
        public int hairIndex;
        public int lensIndex;
        public int earIndex;
        public int tailIndex;
        public CharacterLevelSheet characterLevelSheet;

        protected List<Costume> costumes;
        protected List<Equipment> equipments;

        public IReadOnlyList<Costume> Costumes => costumes;
        public IReadOnlyList<Equipment> Equipments => equipments;

        public Player(AvatarState avatarState, Simulator simulator)
            : base(
                simulator,
                simulator.CharacterSheet,
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
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = avatarState.hair;
            lensIndex = avatarState.lens;
            earIndex = avatarState.ear;
            tailIndex = avatarState.tail;
            PostConstruction(simulator.CharacterLevelSheet, simulator.EquipmentItemSetEffectSheet);
        }

        public Player(
            AvatarState avatarState,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        ) : base(
            null,
            characterSheet,
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
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = avatarState.hair;
            lensIndex = avatarState.lens;
            earIndex = avatarState.ear;
            tailIndex = avatarState.tail;
            PostConstruction(characterLevelSheet, equipmentItemSetEffectSheet);
        }

        public Player(
            int level,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        ) : base(
            null,
            characterSheet,
            GameConfig.DefaultAvatarCharacterId,
            level)
        {
            Exp.Current = characterLevelSheet[level].Exp;
            Inventory = new Inventory();
            worldInformation = null;
            weapon = null;
            armor = null;
            belt = null;
            necklace = null;
            ring = null;
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = 0;
            lensIndex = 0;
            earIndex = 0;
            tailIndex = 0;
            PostConstruction(characterLevelSheet, equipmentItemSetEffectSheet);
        }

        public Player(AvatarState avatarState, SimulatorSheets simulatorSheets) : this(avatarState,
            simulatorSheets.CharacterSheet, simulatorSheets.CharacterLevelSheet,
            simulatorSheets.EquipmentItemSetEffectSheet)
        {
        }

        public Player(ArenaPlayerDigest enemyArenaPlayerDigest, ArenaSimulatorSheets simulatorSheets)
             : base(null,
                 simulatorSheets.CharacterSheet,
                 enemyArenaPlayerDigest.CharacterId,
                 enemyArenaPlayerDigest.Level)
        {
            Inventory = new Inventory();
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = enemyArenaPlayerDigest.HairIndex;
            lensIndex = enemyArenaPlayerDigest.LensIndex;
            earIndex = enemyArenaPlayerDigest.EarIndex;
            tailIndex = enemyArenaPlayerDigest.TailIndex;
            AttackCountMax = AttackCountHelper.GetCountMax(Level);
            characterLevelSheet = simulatorSheets.CharacterLevelSheet;
            UpdateExp();
            SetItems(enemyArenaPlayerDigest.Costumes, enemyArenaPlayerDigest.Equipments,
                simulatorSheets.EquipmentItemSetEffectSheet, simulatorSheets.CostumeStatSheet);
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
            monsterMap = value.monsterMap;
            eventMap = value.eventMap;
            hairIndex = value.hairIndex;
            lensIndex = value.lensIndex;
            earIndex = value.earIndex;
            tailIndex = value.tailIndex;
            characterLevelSheet = value.characterLevelSheet;

            costumes = value.costumes;
            equipments = value.equipments;
        }

        public override bool IsHit(CharacterBase caster)
        {
            return true;
        }

        private void PostConstruction(CharacterLevelSheet levelSheet, EquipmentItemSetEffectSheet equipmentItemSetEffectSheet)
        {
            AttackCountMax = AttackCountHelper.GetCountMax(Level);
            characterLevelSheet = levelSheet;
            UpdateExp();

            if (Inventory != null)
            {
                Equip(Inventory.Items, equipmentItemSetEffectSheet);
            }
        }

        private void UpdateExp()
        {
            characterLevelSheet.TryGetValue(Level, out var row, true);
            Exp.Set(row);
        }

        public void RemoveTarget(Enemy enemy)
        {
            monsterMap.Add(new KeyValuePair<int, int>(enemy.CharacterId, 1));
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
        }

        private void Equip(IReadOnlyList<Inventory.Item> items, EquipmentItemSetEffectSheet sheet)
        {
            costumes = items.Select(i => i.item)
                .OfType<Costume>()
                .Where(e => e.equipped)
                .ToList();
            equipments = items.Select(i => i.item)
                .OfType<Equipment>()
                .Where(e => e.equipped)
                .ToList();
            SetEquipmentStat(sheet);
        }

        private void SetItems(
            List<Costume> arenaCostumes,
            List<Equipment> arenaEquipments,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet)
        {
            costumes = arenaCostumes;
            equipments = arenaEquipments;
            SetEquipmentStat(equipmentItemSetEffectSheet);
            SetCostumeStat(costumeStatSheet);
        }

        protected void SetEquipmentStat(EquipmentItemSetEffectSheet sheet)
        {
            foreach (var equipment in equipments)
            {
                switch (equipment.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        weapon = equipment as Weapon;
                        break;
                    case ItemSubType.Armor:
                        armor = equipment as Armor;
                        defElementType = equipment.ElementalType;
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
                    default:
                        throw new RequiredBlockIndexException();
                }
            }

            Stats.SetEquipments(equipments, sheet);

            foreach (var skill in equipments.SelectMany(equipment => equipment.Skills))
            {
                Skills.Add(skill);
            }

            foreach (var buffSkill in equipments.SelectMany(equipment => equipment.BuffSkills))
            {
                Skills.Add(buffSkill);
            }
        }

        public void GetExp(long waveExp, bool log = false)
        {
            if (!characterLevelSheet.TryGetLevel(Exp.Current + waveExp, out var newLevel))
            {
                waveExp = Exp.Max - Exp.Current - 1;
                newLevel = Level;
            }

            Exp.Current += waveExp;

            if (log)
            {
                var getExp = new GetExp((CharacterBase) Clone(), waveExp);
                Simulator.Log.Add(getExp);
            }

            if (Level == newLevel)
            {
                return;
            }

            if (Level < newLevel)
            {
                eventMap?.Add(new KeyValuePair<int, int>((int) QuestEventType.Level, newLevel - Level));
            }
            Level = newLevel;

            UpdateExp();
        }

        [Obsolete("Use GetExp")]
        public void GetExp2(long waveExp, bool log = false)
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
            Level = characterLevelSheet.GetLevel(Exp.Current);
            // UI에서 레벨업 처리시 NRE 회피
            if (level < Level)
            {
                eventMap?.Add(new KeyValuePair<int, int>((int) QuestEventType.Level, Level - level));
            }

            UpdateExp();
        }

        [Obsolete("Use GetExp")]
        public void GetExp3(long waveExp, bool log = false)
        {
            if (!characterLevelSheet.TryGetLevel(Exp.Current + waveExp, out var newLevel))
            {
                waveExp = Exp.Max - Exp.Current - 1;
                newLevel = Level;
            }

            Exp.Current += waveExp;

            if (log)
            {
                var getExp = new GetExp((CharacterBase) Clone(), waveExp);
                Simulator.Log.Add(getExp);
            }

            if (Level == newLevel)
            {
                return;
            }

            Level = newLevel;
            if (Level < newLevel)
            {
                eventMap?.Add(new KeyValuePair<int, int>((int) QuestEventType.Level, newLevel - Level));
            }

            UpdateExp();
        }

        // // todo : Only material items are provided on stage. If NFT items are provided in the future, they need to be modified.
        public CollectionMap GetRewards(List<ItemBase> items)
        {
            var map = new CollectionMap();
            foreach (var item in items)
            {
                map.Add(Inventory.AddItem(item));
            }

            return map;
        }

        [Obsolete("Use GetRewards")]
        public CollectionMap GetRewards2(List<ItemBase> items)
        {
            var map = new CollectionMap();
            foreach (var item in items)
            {
                map.Add(Inventory.AddItem2(item));
            }

            return map;
        }

        public virtual void Spawn()
        {
            InitAI();
            var spawn = new SpawnPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        [Obsolete("Use Spawn")]
        public virtual void SpawnV1()
        {
            InitAIV1();
            var spawn = new SpawnPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        [Obsolete("Use Spawn")]
        public virtual void SpawnV2()
        {
            InitAIV2();
            var spawn = new SpawnPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        public void Use(List<Guid> consumableIds)
        {
            var consumables = Inventory.Items
                .Select(i => i.item)
                .OfType<Consumable>()
                .Where(i => consumableIds.Contains(i.ItemId))
                .ToList();
            Stats.SetConsumables(consumables);
            foreach (var food in consumables)
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

        public void AddSkill(Skill.Skill skill)
        {
            Skills.Add(skill);
        }

        public void SetCostumeStat(CostumeStatSheet costumeStatSheet)
        {
            var statModifiers = new List<StatModifier>();
            foreach (var itemId in costumes.Select(costume => costume.Id))
            {
                statModifiers.AddRange(
                    costumeStatSheet.OrderedList
                        .Where(r => r.CostumeId == itemId)
                        .Select(row => new StatModifier(row.StatType, StatModifier.OperationType.Add, (int) row.Stat))
                );
            }
            Stats.SetOption(statModifiers);
            Stats.EqualizeCurrentHPWithHP();
        }

        public override object Clone()
        {
            return new Player(this);
        }

        protected override void EndTurn()
        {
            base.EndTurn();
            if (this is EnemyPlayer)
                return;

            Simulator.TurnNumber++;
            Simulator.WaveTurn++;
            Simulator.Log.Add(new WaveTurnEnd(this, Simulator.TurnNumber, Simulator.WaveTurn));
        }
    }
}
