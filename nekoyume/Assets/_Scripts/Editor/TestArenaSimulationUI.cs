using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
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

        [SerializeField]
        private int enemyLevel;

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
            var jsonPath = Platform.GetStreamingAssetsPath("avatar.json");
            var fileStream = new FileStream(jsonPath, FileMode.Open);
            var data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();
            var jsonData = Encoding.UTF8.GetString(data);
            var result = JsonConvert.DeserializeObject<AvatarModel>(jsonData);
            var avatar1 = GetAvatarState(result.Level, "me", avatarSheets);
            var avatar2 = GetAvatarState(enemyLevel, "enemy", avatarSheets);

            var inventory1 = new Inventory();
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var skillSheet = tableSheets.SkillSheet;
            foreach (var equipmentItem in result.EquipmentItems)
            {
                var row = equipmentItemSheet[equipmentItem.Id];
                var equipment = (Equipment)ItemFactory.CreateItemUsable(row, new Guid(), 0L);
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
                equipment.SetLevel(new RandomImpl(), equipmentItem.Level, tableSheets.EnhancementCostSheetV3);
                equipment.equipped = true;
                inventory1.AddItem(equipment);
            }
            var costumeSheet = tableSheets.CostumeItemSheet;
            foreach (var costumeId in result.CostumeIds)
            {
                var costume = new Costume(costumeSheet[costumeId], new Guid())
                {
                    equipped = true,
                };
                inventory1.AddItem(costume);
            }
            var inventory2 = new Inventory();

            var allRune1 = new AllRuneState();
            var runeSlotInfos = new List<RuneSlotInfo>();
            foreach (var runeItem in result.RuneItems)
            {
                var runeId = runeItem.Id;
                allRune1.AddRuneState(new RuneState(runeId, runeItem.Level));
                if (runeItem.SlotIndex.HasValue)
                {
                    runeSlotInfos.Add(new RuneSlotInfo(runeItem.SlotIndex.Value, runeId));
                }
            }
            var allRune2 = new AllRuneState();
            var runeSlot1 = new RuneSlotState(BattleType.Arena);
            runeSlot1.UpdateSlot(runeSlotInfos, tableSheets.RuneListSheet);
            var runeSlot2 = new RuneSlotState(BattleType.Arena);

            CollectionState collection1 = new CollectionState();
            var collectionIds = result.CollectionIds.Distinct();
            foreach (var collectionId in collectionIds)
            {
                collection1.Ids.Add(collectionId);
            }
            CollectionState collection2 = new CollectionState();
            _myCollectionState = collection1;
            _enemyCollectionState = collection2;
            _me = new ArenaPlayerDigest(avatar1,
                inventory1.Costumes.Where(eq => eq.equipped).ToList(),
                inventory1.Equipments.Where(eq => eq.equipped).ToList(),
                allRune1,
                runeSlot1);
            _myAddress = avatar1.address;
            _enemy = new ArenaPlayerDigest(avatar2,
                inventory2.Costumes.Where(eq => eq.equipped).ToList(),
                inventory2.Equipments.Where(eq => eq.equipped).ToList(),
                allRune2,
                runeSlot2);
            _enemyAddress = avatar2.address;

            NcDebug.Log($"name: {avatar1.name}\n" + StatStringFromDigest(_me,
                _myCollectionState.GetModifiers(TestArena.Instance.TableSheets.CollectionSheet)),
                "TestArenaSimulation");
            NcDebug.Log($"name: {avatar2.name}\n" + StatStringFromDigest(_enemy,
                    _enemyCollectionState.GetModifiers(TestArena.Instance.TableSheets
                        .CollectionSheet)),
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
                TestArena.Instance.TableSheets.BuffLinkSheet);
            TestArena.Instance.Enter(log,
                new List<ItemBase>(),
                _me,
                _enemy,
                _myAddress,
                _enemyAddress);
        }

        private string GetTrimmedStringFromPath(string path)
        {
            return File.ReadAllText(Platform.GetStreamingAssetsPath(path)).Trim();
        }

        private static IValue GetStateFromRawState(string rawBinaryString)
        {
            var binary = Binary.FromHex(rawBinaryString);
            return new Codec().Decode(binary.ToByteArray());
        }

        private static AvatarState GetAvatarStateFromRawState(string rawBinaryString)
        {
            return new AvatarState((List)GetStateFromRawState(rawBinaryString));
        }

        private static AvatarState GetAvatarState(int level, string name, AvatarSheets avatarSheets)
        {
            var avatarState = AvatarState.Create(new Address(), new Address(), 0, avatarSheets, new Address(), name);
            avatarState.level = level;
            return avatarState;
        }

        private static Inventory GetInventoryFromRawState(string rawBinaryString)
        {
            return new Inventory((List)GetStateFromRawState(rawBinaryString));
        }

        private static AllRuneState GetAllRuneStateFromRawState(string rawBinaryString)
        {
            return new AllRuneState((List)GetStateFromRawState(rawBinaryString));
        }

        private static RuneSlotState GetRuneSlotStateStateFromRawState(string rawBinaryString)
        {
            return new RuneSlotState((List)GetStateFromRawState(rawBinaryString));
        }

        private static CollectionState GetCollectionStateFromRawState(string rawBinaryString)
        {
            return new CollectionState((List)GetStateFromRawState(rawBinaryString));
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
