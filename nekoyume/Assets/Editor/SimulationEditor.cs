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
            if (GUILayout.Button("try serialize"))
            {
                Debug.Log("Trying to serialize...");

                var binary = Binary.FromHex(_avatarByteString);
                var inventoryBinary = Binary.FromHex(_inventoryByteString);
                var inventory = new Inventory((List)new Codec().Decode(inventoryBinary.ToByteArray()));
                var avatar = new AvatarState((List) new Codec().Decode(binary.ToByteArray()))
                {
                    inventory = inventory,
                };
                var secondAvatar = new AvatarState((List) new Codec().Decode(binary.ToByteArray()))
                {
                    inventory = inventory,
                    address = new PrivateKey().Address,
                };
                var simulator = new ArenaSimulator(new Cheat.DebugRandom());
                var challArena = new ArenaPlayerDigest(avatar, new List<Costume>(),
                    inventory.Equipments.Where(eq => eq.equipped).ToList(), new AllRuneState(0),
                    new RuneSlotState(BattleType.Arena));
                var secondArena = new ArenaPlayerDigest(secondAvatar, new List<Costume>(),
                    inventory.Equipments.Where(eq => eq.equipped).ToList(), new AllRuneState(0),
                    new RuneSlotState(BattleType.Arena));
                var sheets = TableSheetsHelper.MakeTableSheets();
                var log = simulator.Simulate(challArena, secondArena, sheets.GetArenaSimulatorSheets(),
                    new List<StatModifier>(), new List<StatModifier>(), sheets.BuffLimitSheet,
                    sheets.BuffLinkSheet);
                SimulationTest.TestArena.Instance.TableSheets = sheets;
                SimulationTest.TestArena.Instance.Enter(log, new List<ItemBase>(), challArena,secondArena,avatar.address, secondAvatar.address);
                // foreach (var eventBase in log)
                // {
                //
                //     NcDebug.Log($"{eventBase.Character.Id}, {eventBase.GetType()}, {eventBase.Character.usedSkill}, {eventBase.Character.Buffs}", "BattleSimulation");
                // }
            }
        }
    }
}
