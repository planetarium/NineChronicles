using System.Collections.Generic;
using System.Linq;
using Bencodex;
using Bencodex.Json;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Arena;
using Nekoyume.Blockchain;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class SimulationEditor : EditorWindow
    {
        private static string _avatarByteString;
        private static string _inventoryByteString;
        private static string _allRuneByteString;
        private static string _arenaRuneSlotByteString;
        private static string _collectionIdsString;

        [MenuItem("Tools/Show Simulation Editor")]
        private static void Init()
        {
            _avatarByteString = string.Empty;
            var window = GetWindow(typeof(SimulationEditor));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("my avatar state byte string");
            _avatarByteString = EditorGUILayout.TextField("avatar byte: ", _avatarByteString);
            _inventoryByteString = EditorGUILayout.TextField("inventory byte: ", _inventoryByteString);
            _allRuneByteString = EditorGUILayout.TextField("allRune byte: ", _allRuneByteString);
            _arenaRuneSlotByteString = EditorGUILayout.TextField("runeSlot byte: ", _arenaRuneSlotByteString);
            _collectionIdsString = EditorGUILayout.TextField("collection ids: ", _collectionIdsString);
            if (GUILayout.Button("try serialize"))
            {
                Debug.Log("Trying to serialize...");

                var binary = Binary.FromHex(_avatarByteString);
                var inventoryBinary = Binary.FromHex(_inventoryByteString);
                var allRuneBinary = Binary.FromHex(_allRuneByteString);
                var runeSlotBinary = Binary.FromHex(_arenaRuneSlotByteString);
                var collectionState = new CollectionState();
                foreach (var id in _collectionIdsString.Trim().Split(",").Select(int.Parse))
                {
                    collectionState.Ids.Add(id);
                }

                var inventory = new Inventory((List)new Codec().Decode(inventoryBinary.ToByteArray()));
                var allRuneState = new AllRuneState((List) new Codec().Decode(allRuneBinary.ToByteArray()));
                var runeSlotState =
                    new RuneSlotState((List) new Codec().Decode(runeSlotBinary.ToByteArray()));
                var avatar = new AvatarState((List) new Codec().Decode(binary.ToByteArray()))
                {
                    //inventory = inventory,
                };
                var secondAvatar = new AvatarState((List) new Codec().Decode(binary.ToByteArray()))
                {
                    //inventory = inventory,
                    address = new PrivateKey().Address,
                };
                var simulator = new ArenaSimulator(new Cheat.DebugRandom());
                var challArena = new ArenaPlayerDigest(avatar, inventory.Costumes.Where(eq => eq.equipped).ToList(),
                    inventory.Equipments.Where(eq => eq.equipped).ToList(), allRuneState,
                    runeSlotState);
                var secondArena = new ArenaPlayerDigest(secondAvatar, inventory.Costumes.Where(eq => eq.equipped).ToList(),
                    inventory.Equipments.Where(eq => eq.equipped).ToList(), allRuneState,
                    runeSlotState);
                var sheets = TableSheetsHelper.MakeTableSheets();
                var log = simulator.Simulate(challArena,
                    secondArena,
                    sheets.GetArenaSimulatorSheets(),
                    collectionState.GetModifiers(sheets.CollectionSheet),
                    collectionState.GetModifiers(sheets.CollectionSheet),
                    sheets.BuffLimitSheet,
                    sheets.BuffLinkSheet);
                SimulationTest.TestArena.Instance.TableSheets = sheets;
                SimulationTest.TestArena.Instance.Enter(log, new List<ItemBase>(), challArena,secondArena,avatar.address, secondAvatar.address);
            }
        }
    }
}
