using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace SimulationTest
{
    public class TestArenaSimulationUI : MonoBehaviour
    {
        [SerializeField]
        private Button serializeButton;

        [SerializeField]
        private Button simulationButton;

        private ArenaPlayerDigest _me;
        private ArenaPlayerDigest _enemy;
        private Address _myAddress;
        private Address _enemyAddress;
        private CollectionState _myCollectionState;
        private CollectionState _enemyCollectionState;
        private void Awake()
        {
            serializeButton.onClick.AddListener(LoadFileAndSerialize);
            simulationButton.onClick.AddListener(Simulate);
        }

        private void Start()
        {
            var sheets = TableSheetsHelper.MakeTableSheets();
            TestArena.Instance.TableSheets = sheets;
        }

        private void LoadFileAndSerialize()
        {
            var tableSheets = TestArena.Instance.TableSheets;
            var avatarSheets = tableSheets.GetAvatarSheets();
            LoadAvatar(tableSheets, avatarSheets, "avatar.json", true);
            LoadAvatar(tableSheets, avatarSheets, "avatar-enemy.json", false);
        }

        private void LoadAvatar(TableSheets tableSheets, AvatarSheets avatarSheets, string fileName, bool me)
        {
            var jsonPath = Platform.GetStreamingAssetsPath(fileName);
            var fileStream = new FileStream(jsonPath, FileMode.Open);
            var data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();
            var jsonData = Encoding.UTF8.GetString(data);
            var result = JsonConvert.DeserializeObject<AvatarModel>(jsonData);
            var avatarName = me ? "me" : "enemy";
            var avatar = GetAvatarState(result.Level, avatarName, avatarSheets);
            var inventory = new Inventory();
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var skillSheet = tableSheets.SkillSheet;
            foreach (var equipmentItem in result.EquipmentItems)
            {
                var row = equipmentItemSheet[equipmentItem.Id];
                var equipment = (Equipment)ItemFactory.CreateItemUsable(row, new Guid(), 0L, equipmentItem.Level);
                foreach (var statOption in equipmentItem.StatOptions)
                {
                    var statMap = new DecimalStat(statOption.StatType, statOption.Value);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.TotalValue);
                }

                foreach (var skillOption in equipmentItem.SkillOptions)
                {
                    var skillRow = skillSheet.Values.First(r => r.Id == skillOption.Id);
                    var skill = SkillFactory.Get(skillRow, skillOption.Power, skillOption.Chance, skillOption.StatPowerRatio, skillOption.StatType);
                    equipment.Skills.Add(skill);
                }
                equipment.equipped = true;
                inventory.AddItem(equipment);
            }
            var costumeSheet = tableSheets.CostumeItemSheet;
            foreach (var costumeId in result.CostumeIds)
            {
                var costume = new Costume(costumeSheet[costumeId], new Guid())
                {
                    equipped = true,
                };
                inventory.AddItem(costume);
            }
            var allRuneState = new AllRuneState();
            var runeSlotInfos = new List<RuneSlotInfo>();
            foreach (var runeItem in result.RuneItems)
            {
                var runeId = runeItem.Id;
                allRuneState.AddRuneState(new RuneState(runeId, runeItem.Level));
                if (runeItem.SlotIndex.HasValue)
                {
                    runeSlotInfos.Add(new RuneSlotInfo(runeItem.SlotIndex.Value, runeId));
                }
            }
            var runeSlotState = new RuneSlotState(BattleType.Arena);
            runeSlotState.UpdateSlot(runeSlotInfos, tableSheets.RuneListSheet);

            CollectionState collectionState = new CollectionState();
            var collectionIds = result.CollectionIds.Distinct();
            foreach (var collectionId in collectionIds)
            {
                collectionState.Ids.Add(collectionId);
            }
            if (me)
            {
                _myCollectionState = collectionState;
                _me = new ArenaPlayerDigest(avatar,
                    inventory.Costumes.Where(eq => eq.equipped).ToList(),
                    inventory.Equipments.Where(eq => eq.equipped).ToList(),
                    allRuneState,
                    runeSlotState);
                _myAddress = avatar.address;
            }
            else
            {
                _enemyCollectionState = collectionState;
                _enemy = new ArenaPlayerDigest(avatar,
                    inventory.Costumes.Where(eq => eq.equipped).ToList(),
                    inventory.Equipments.Where(eq => eq.equipped).ToList(),
                    allRuneState,
                    runeSlotState);
                _enemyAddress = avatar.address;
            }

            NcDebug.Log($"name: {avatar.name}\n" + StatStringFromDigest(_me,
                    _myCollectionState.GetModifiers(TestArena.Instance.TableSheets.CollectionSheet)),
                "TestArenaSimulation");
        }

        private void Simulate()
        {
            var simulator = new ArenaSimulator(new Cheat.DebugRandom());
            var log = simulator.Simulate(_me,
                _enemy,
                TestArena.Instance.TableSheets.GetArenaSimulatorSheets(),
                _myCollectionState.GetModifiers(TestArena.Instance.TableSheets.CollectionSheet),
                _enemyCollectionState.GetModifiers(TestArena.Instance.TableSheets.CollectionSheet),
                TestArena.Instance.TableSheets.BuffLimitSheet,
                TestArena.Instance.TableSheets.BuffLinkSheet,
                true);
            TestArena.Instance.Enter(log,
                new List<ItemBase>(),
                _me,
                _enemy,
                _myAddress,
                _enemyAddress);
        }

        private static AvatarState GetAvatarState(int level, string name, AvatarSheets avatarSheets)
        {
            var avatarState = AvatarState.Create(new Address(), new Address(), 0, avatarSheets, new Address(), name);
            avatarState.level = level;
            return avatarState;
        }

        public static string StatStringFromDigest(ArenaPlayerDigest avatar, List<StatModifier> modifier)
        {
            var simulator = new ArenaSimulator(new Cheat.DebugRandom());
            var arenaCharacter = new ArenaCharacter(
                new ArenaSimulator(new Cheat.DebugRandom()),
                avatar,
                TestArena.Instance.TableSheets.GetArenaSimulatorSheets(),
                simulator.HpModifier,
                modifier);
            return StatStringFromArenaCharacter(arenaCharacter);
        }

        public static string StatStringFromArenaCharacter(ArenaCharacter arenaCharacter)
        {
            return $@"level: {arenaCharacter.Level}
hp: {arenaCharacter.CurrentHP}
atk: {arenaCharacter.ATK}
def: {arenaCharacter.DEF}
spd: {arenaCharacter.SPD}
hit: {arenaCharacter.HIT}
cri: {arenaCharacter.CRI}
cdmg: {arenaCharacter.CDMG}";
        }

        [Serializable]
        public class AvatarModel
        {
            public int Level;
            public int[] CollectionIds;
            public RuneItem[] RuneItems;
            public int[] CostumeIds;
            public EquipmentItem[] EquipmentItems;
        }

        [Serializable]
        public class RuneItem
        {
            public int Id;
            public int Level;
            public int? SlotIndex;
        }

        [Serializable]
        public class EquipmentItem
        {
            public int Id;
            public int Level;
            public StatOptions[] StatOptions;
            public SkillOptions[] SkillOptions;
        }

        [Serializable]
        public class StatOptions
        {
            public StatType StatType;
            public int Value;
        }

        [Serializable]
        public class SkillOptions
        {
            public int Id;
            public int Power;
            public int Chance;
            public int StatPowerRatio;
            public StatType StatType;
        }
    }
}
