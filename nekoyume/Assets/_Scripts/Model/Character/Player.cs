using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class Player : CharacterBase
    {
        public int characterId;
        public long exp;
        public long expMax;
        public long expNeed;
        public int worldStage;
        public Weapon weapon;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public SetItem set;
        public sealed override float TurnSpeed { get; set; }
        
        public readonly Inventory inventory;
        public readonly long blockIndex;
        
        private List<Equipment> Equipments { get; set; }

        public Player(AvatarState avatarState, Simulator simulator = null) : base(simulator)
        {
            characterId = avatarState.characterId;
            level = avatarState.level;
            exp = avatarState.exp;
            worldStage = avatarState.worldStage;
            inventory = avatarState.inventory;
            blockIndex = avatarState.BlockIndex;
            PostConstruction();
        }

        public Player(int level) : base(null)
        {
            characterId = GameConfig.DefaultAvatarCharacterId;
            this.level = level;
            exp = 0;
            worldStage = 1;
            inventory = new Inventory();
            PostConstruction();
        }

        private void PostConstruction()
        {
            atkElementType = ElementalType.Normal;
            defElementType = ElementalType.Normal;
            TurnSpeed = 1.8f;
            
            Equip(inventory.Items);
            CalcStats(level);
        }

        public void RemoveTarget(Monster monster)
        {
            targets.Remove(monster);
            Simulator.Characters.TryRemove(monster);
        }

        protected override void OnDead()
        {
            base.OnDead();
            Simulator.Lose = true;
        }

        private void CalcStats(int level)
        {
            var game = Game.Game.instance;
            if (!game.TableSheets.CharacterSheet.TryGetValue(characterId, out var characterRow))
            {
                throw new KeyNotFoundException(nameof(characterId));   
            }

            if (!game.TableSheets.LevelSheet.TryGetValue(level, out var levelRow))
            {
                throw new KeyNotFoundException(nameof(level));
            }

            var statsData = characterRow.ToStats(level);
            currentHP = statsData.HP;
            atk = statsData.Damage;
            def = statsData.Defense;
            hp = statsData.HP;
            expMax = levelRow.Exp + levelRow.ExpNeed;
            expNeed = levelRow.ExpNeed;
            luck = statsData.Luck;
            runSpeed = characterRow.RunSpeed;
            characterSize = characterRow.Size;
            var setMap = new Dictionary<int, int>();
            foreach (var equipment in Equipments)
            {
                var key = equipment.Data.SetId;
                if (!setMap.TryGetValue(key, out _))
                {
                    setMap[key] = 0;
                }

                setMap[key] += 1;
                equipment.UpdatePlayer(this);
            }

            // 플레이어 사거리가 장비에 영향을 안받도록 고정시킴.
            attackRange = characterRow.AttackRange;

            foreach (var pair in setMap)
            {
                var setEffect = Tables.instance.GetSetEffect(pair.Key, pair.Value);
                foreach (var statMap in setEffect)
                {
                    statMap.UpdatePlayer(this);
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

        // ToDo. 지금은 스테이지에서 재료 아이템만 주고 있음. 추후 대체 불가능 아이템도 줄 경우 수정 대상.
        public void GetRewards(List<ItemBase> items)
        {
            foreach (var item in items)
            {
                inventory.AddFungibleItem(item);
            }
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
            level = Game.Game.instance.TableSheets.LevelSheet.GetLevel(exp);
        }


        public IEnumerable<(string key, object value, float additional)> GetStatusRow()
        {
            var fields = GetType().GetFields();
            var tuples = fields
                .Where(field => field.IsDefined(typeof(InformationFieldAttribute), true))
                .Select(field => (field.Name, field.GetValue(this), decimal.ToSingle(GetAdditionalStatus(field.Name))));
            return tuples;
        }

        public decimal GetAdditionalStatus(string key)
        {
            var game = Game.Game.instance;
            if (!game.TableSheets.CharacterSheet.TryGetValue(characterId, out var characterRow))
            {
                throw new KeyNotFoundException($"invalid character id: `{characterId}`.");
            }

            var statsData = characterRow.ToStats(level);

            decimal value;
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
                    value = (luck - statsData.Luck);
                    break;
                default:
                    throw new InvalidCastException($"invalid status key: `{key}`.");
            }

            return value;
        }

        public IEnumerable<string> GetOptions()
        {
            var atkOptions = atkElementType.GetOptions("damage");
            foreach (var atkOption in atkOptions)
            {
                yield return atkOption;
            }
            
            var defOptions = defElementType.GetOptions("defense");
            foreach (var defOption in defOptions)
            {
                yield return defOption;
            }
        }

        public void Use(List<Consumable> foods)
        {
            foreach (var food in foods)
            {
                food.UpdatePlayer(this);
                inventory.RemoveNonFungibleItem(food);
            }
        }

        public void OverrideSkill(Game.Skill skill)
        {
            Skills.Clear();
            Skills.Add(skill);
        }
    }

    public class InvalidEquipmentException : Exception
    {
    }
}
