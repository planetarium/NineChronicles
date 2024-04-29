using System.Globalization;
using System.Linq;
using System.Reflection;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Planetarium.Nekoyume.Editor
{
    public class AddressToolWindow : EditorWindow
    {
        private static (string name, Address addr, string addrStr)[] _addresses;
        private static GUIContent[] _addressesContents;

        //
        private bool _useAddressesAsOriginal;
        private int _selectedIndexOfAddresses;
        private string _originalAddr;
        private string _deriveKey;
        private string _derivedAddr;

        //
        private Address _agentAddress;
        private string _agentAddr;

        private Address[] _avatarAddresses;
        private string[] _avatarAddrArr;
        private string[] _avatarAddrIndexes;
        private int _selectedAvatarIndex;
        private string[] _inventoryAddrArr;
        private string[] _worldInformationAddrArr;
        private string[] _questListAddrArr;
        private string[][] _workshopSlotAddrArr;

        private int _championshipId;
        private int _round;
        private string[] _arenaInformationAddrArr;
        private string[] _arenaScoreAddrArr;

        [MenuItem("Tools/Lib9c/Address Tool")]
        private static void Init()
        {
            GetWindow<AddressToolWindow>("Address Tool", true)
                .Show();
        }

        private void OnEnable()
        {
            _addresses = typeof(Addresses).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fi => fi.FieldType == typeof(Address))
                .Select(fi =>
                {
                    var addr = (Address)fi.GetValue(null);
                    return (fi.Name, addr, addr.ToString());
                })
                .ToArray();
            _addressesContents = _addresses.Select(tuple =>
                new GUIContent($"{tuple.name}:  {tuple.addrStr}")).ToArray();
        }

        private void OnGUI()
        {
            DrawArea1();
            DrawArea2();
        }

        private void DrawArea1()
        {
            GUILayout.Label("Derive Address", EditorStyles.boldLabel);
            _useAddressesAsOriginal = EditorGUILayout.Toggle(
                "Use Addresses as Original",
                _useAddressesAsOriginal);
            if (_useAddressesAsOriginal)
            {
                _selectedIndexOfAddresses = EditorGUILayout.Popup(
                    new GUIContent("Original Address"),
                    _selectedIndexOfAddresses,
                    _addressesContents);
                _originalAddr = _addresses[_selectedIndexOfAddresses].addrStr;
            }
            else
            {
                _originalAddr = EditorGUILayout.TextField(
                    "Original Address",
                    _originalAddr);
            }

            _deriveKey = EditorGUILayout.TextField("Derive Key", _deriveKey);

            if (!string.IsNullOrEmpty(_originalAddr) &&
                !string.IsNullOrEmpty(_deriveKey) &&
                GUILayout.Button("Derive"))
            {
                Address originalAddr;
                try
                {
                    originalAddr = new Address(_originalAddr);
                }
                catch (System.Exception e)
                {
                    NcDebug.LogError(e);
                    return;
                }

                var derivedAddr = originalAddr.Derive(_deriveKey);
                _derivedAddr = derivedAddr.ToString();
            }

            CopyableLabelField("Derived Address", _derivedAddr);
        }

        private void DrawArea2()
        {
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Label("Agent Addresses", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying ||
                                         !Game.instance.IsInitialized);
            if (GUILayout.Button("Setup Current Agent") &&
                Application.isPlaying &&
                Game.instance.IsInitialized &&
                Game.instance.Agent is { })
            {
                _agentAddr = Game.instance.Agent.Address.ToString();
                DeriveAvatarAddresses();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.BeginHorizontal();
            _agentAddr = EditorGUILayout.TextField(
                "Agent Address",
                _agentAddr);

            if (GUILayout.Button("Derive", GUILayout.Width(50)) &&
                !string.IsNullOrEmpty(_agentAddr))
            {
                DeriveAvatarAddresses();
            }

            EditorGUILayout.EndHorizontal();

            if (_avatarAddrIndexes == null ||
                _avatarAddrIndexes.Length == 0)
            {
                return;
            }

            _selectedAvatarIndex = GUILayout.SelectionGrid(
                _selectedAvatarIndex,
                _avatarAddrIndexes,
                _avatarAddrIndexes.Length);
            CopyableLabelField("- AvatarState", _avatarAddrArr[_selectedAvatarIndex]);
            CopyableLabelField("- Inventory", _inventoryAddrArr[_selectedAvatarIndex]);
            CopyableLabelField(
                "- WorldInformation",
                _worldInformationAddrArr[_selectedAvatarIndex]);
            CopyableLabelField("- QuestList", _questListAddrArr[_selectedAvatarIndex]);
            for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
            {
                CopyableLabelField(
                    $"- WorkshopSlot[{i}]",
                    _workshopSlotAddrArr?[_selectedAvatarIndex]?[i] ?? string.Empty);
            }

            EditorGUILayout.BeginHorizontal();
            _championshipId = EditorGUILayout.IntField("Championship Id", _championshipId);
            _round = EditorGUILayout.IntField("Round", _round);
            if (GUILayout.Button("Derive", GUILayout.Width(50)) &&
                _championshipId > 0 &&
                _round > 0)
            {
                DeriveArenaAddresses();
            }

            EditorGUILayout.EndHorizontal();
            CopyableLabelField("- Arena Info", _arenaInformationAddrArr[_selectedAvatarIndex]);
            CopyableLabelField("- Arena Score", _arenaScoreAddrArr[_selectedAvatarIndex]);
        }

        private static void CopyableLabelField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, value);
            var icon = EditorGUIUtility.IconContent("clipboard", "Copy to clipboard");
            if (GUILayout.Button(icon, GUILayout.ExpandWidth(false)))
            {
                EditorGUIUtility.systemCopyBuffer = value;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DeriveAvatarAddresses()
        {
            try
            {
                _agentAddress = new Address(_agentAddr);
            }
            catch (System.Exception e)
            {
                NcDebug.LogError(e);
                return;
            }

            _avatarAddresses = Enumerable.Range(0, 3)
                .Select(i => _agentAddress.Derive(string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    i)))
                .ToArray();
            _avatarAddrArr = _avatarAddresses
                .Select(addr => addr.ToString())
                .ToArray();
            _avatarAddrIndexes = _avatarAddrArr
                .Select((_, index) => index.ToString())
                .ToArray();
            _selectedAvatarIndex = 0;
            _inventoryAddrArr = _avatarAddresses
                .Select(addr => addr.Derive(LegacyInventoryKey).ToString())
                .ToArray();
            _worldInformationAddrArr = _avatarAddresses
                .Select(addr => addr.Derive(LegacyWorldInformationKey).ToString())
                .ToArray();
            _questListAddrArr = _avatarAddresses
                .Select(addr => addr.Derive(LegacyQuestListKey).ToString())
                .ToArray();
            _workshopSlotAddrArr = _avatarAddresses
                .Select(addr => Enumerable.Range(0, AvatarState.CombinationSlotCapacity)
                    .Select(i => addr.Derive(string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i)).ToString())
                    .ToArray())
                .ToArray();

            _championshipId = 0;
            _round = 0;
            DeriveArenaAddresses();
        }

        private void DeriveArenaAddresses()
        {
            _arenaInformationAddrArr = _avatarAddresses
                .Select(addr => ArenaInformation.DeriveAddress(
                    addr,
                    _championshipId,
                    _round).ToString())
                .ToArray();
            _arenaScoreAddrArr = _avatarAddresses
                .Select(addr => ArenaScore.DeriveAddress(
                    addr,
                    _championshipId,
                    _round).ToString())
                .ToArray();
        }
    }
}
