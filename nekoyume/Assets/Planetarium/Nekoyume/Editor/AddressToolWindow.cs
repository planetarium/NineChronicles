using System.Globalization;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using UnityEditor;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Planetarium.Nekoyume.Editor
{
    public class AddressToolWindow : EditorWindow
    {
        //
        private string _originalAddrHex;
        private string _deriveKey;
        private string _derivedAddrHex;

        //
        private string _agentAddrHex;
        private string[] _avatarAddrHexes;
        private string[] _avatarInventoryAddrHexes;
        private string[] _avatarWorldInformationAddrHexes;
        private string[] _avatarQuestListAddrHexes;
        private string[] _avatarAddrIndexStrings;
        private int _avatarAddrIndex;

        [MenuItem("Tools/Address Tool")]
        private static void Init()
        {
            GetWindow<AddressToolWindow>("Address Tool", true)
                .Show();
        }

        private void OnGUI()
        {
            DrawArea1();
            DrawArea2();
        }

        private void DrawArea1()
        {
            GUILayout.Label("Original Address Hex", EditorStyles.boldLabel);
            _originalAddrHex = GUILayout.TextField(_originalAddrHex) ?? string.Empty;
            _originalAddrHex = ToHex(_originalAddrHex);

            GUILayout.Label("Derive Key", EditorStyles.boldLabel);
            _deriveKey = GUILayout.TextField(_deriveKey) ?? string.Empty;

            if (!string.IsNullOrEmpty(_originalAddrHex) &&
                !string.IsNullOrEmpty(_deriveKey) &&
                GUILayout.Button("Derive"))
            {
                Address originalAddr;
                try
                {
                    originalAddr = new Address(_originalAddrHex);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                var derivedAddr = originalAddr.Derive(_deriveKey);
                _derivedAddrHex = derivedAddr.ToHex();
            }

            GUILayout.Label("Derived Address Hex", EditorStyles.boldLabel);
            GUILayout.TextArea(_derivedAddrHex, EditorStyles.label);
        }

        private void DrawArea2()
        {
            GUILayout.Space(20);
            GUILayout.Label("Agent Address Hex", EditorStyles.boldLabel);
            _agentAddrHex = GUILayout.TextField(_agentAddrHex) ?? string.Empty;
            _agentAddrHex = ToHex(_agentAddrHex);

            if (!string.IsNullOrEmpty(_agentAddrHex) &&
                GUILayout.Button("Derive"))
            {
                Address agentAddr;
                try
                {
                    agentAddr = new Address(_agentAddrHex);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                var avatarAddrArray = Enumerable.Range(0, 3)
                    .Select(i => agentAddr.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CreateAvatar.DeriveFormat,
                            i
                        )))
                    .ToArray();
                _avatarAddrHexes = avatarAddrArray
                    .Select(addr => addr.ToHex())
                    .ToArray();
                _avatarInventoryAddrHexes = avatarAddrArray
                    .Select(addr => addr.Derive(LegacyInventoryKey).ToHex())
                    .ToArray();
                _avatarWorldInformationAddrHexes = avatarAddrArray
                    .Select(addr => addr.Derive(LegacyWorldInformationKey).ToHex())
                    .ToArray();
                _avatarQuestListAddrHexes = avatarAddrArray
                    .Select(addr => addr.Derive(LegacyQuestListKey).ToHex())
                    .ToArray();
                _avatarAddrIndexStrings = _avatarAddrHexes
                    .Select((_, index) => index.ToString())
                    .ToArray();
                _avatarAddrIndex = 0;
            }

            if (_avatarAddrIndexStrings == null ||
                _avatarAddrIndexStrings.Length == 0)
            {
                return;
            }

            _avatarAddrIndex = GUILayout.SelectionGrid(
                _avatarAddrIndex,
                _avatarAddrIndexStrings,
                _avatarAddrIndexStrings.Length);
            GUILayout.Label("Selected AvatarState Address Hex", EditorStyles.boldLabel);
            GUILayout.TextArea(
                _avatarAddrHexes[_avatarAddrIndex],
                EditorStyles.label);
            GUILayout.Label("Inventory Address Hex", EditorStyles.boldLabel);
            GUILayout.TextArea(
                _avatarInventoryAddrHexes[_avatarAddrIndex],
                EditorStyles.label);
            GUILayout.Label("WorldInformation Address Hex", EditorStyles.boldLabel);
            GUILayout.TextArea(
                _avatarWorldInformationAddrHexes[_avatarAddrIndex],
                EditorStyles.label);
            GUILayout.Label("QuestList Address Hex", EditorStyles.boldLabel);
            GUILayout.TextArea(
                _avatarQuestListAddrHexes[_avatarAddrIndex],
                EditorStyles.label);
        }

        private static string ToHex(string hexString) =>
            hexString.StartsWith("0x")
                ? hexString[2..]
                : hexString;
    }
}
