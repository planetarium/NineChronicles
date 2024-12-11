using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Arena;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
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
            var avatar1 = GetAvatarStateFromRawState(GetTrimmedStringFromPath("serialized_avatar1.txt"));
            var avatar2 = GetAvatarStateFromRawState(GetTrimmedStringFromPath("serialized_avatar2.txt"));

            var inventory1 = GetInventoryFromRawState(GetTrimmedStringFromPath("serialized_inventory1.txt"));
            var inventory2 = GetInventoryFromRawState(GetTrimmedStringFromPath("serialized_inventory2.txt"));

            var allRune1 = GetAllRuneStateFromRawState(GetTrimmedStringFromPath("serialized_allrune1.txt"));
            var allRune2 = GetAllRuneStateFromRawState(GetTrimmedStringFromPath("serialized_allrune2.txt"));

            var runeSlot1 = GetRuneSlotStateStateFromRawState(GetTrimmedStringFromPath("serialized_runeslot1.txt"));
            var runeSlot2 = GetRuneSlotStateStateFromRawState(GetTrimmedStringFromPath("serialized_runeslot2.txt"));

            var collection1Str =
                GetTrimmedStringFromPath("serialized_collection1.txt");
            var collection2Str =
                GetTrimmedStringFromPath("serialized_collection1.txt");
            CollectionState collection1;
            CollectionState collection2;
            try
            {
                collection1 = GetCollectionStateFromRawState(collection1Str);
            }
            catch
            {
                collection1 = new CollectionState();
                foreach (var id in collection1Str.Trim().Split(",").Select(int.Parse))
                {
                    collection1.Ids.Add(id);
                }
            }

            try
            {
                collection2 = GetCollectionStateFromRawState(collection2Str);
            }
            catch
            {
                collection2 = new CollectionState();
                foreach (var id in collection2Str.Trim().Split(",").Select(int.Parse))
                {
                    collection2.Ids.Add(id);
                }
            }

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
    }
}
