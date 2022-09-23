using Libplanet;
using Nekoyume.Action;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public class AddressToolWindow : EditorWindow
    {
        private string _originalAddressHex;
        private string _deriveKey;
        private string _derivedAddressHex;

        [MenuItem("Tools/Address Tool")]
        private static void Init()
        {
            GetWindow<AddressToolWindow>("Address Tool", true)
                .Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Original Address Hex", EditorStyles.boldLabel);
            _originalAddressHex = GUILayout.TextField(_originalAddressHex) ?? string.Empty;
            if (_originalAddressHex.StartsWith("0x"))
            {
                _originalAddressHex = _originalAddressHex[2..];
            }

            GUILayout.Label("Derive Key", EditorStyles.boldLabel);
            _deriveKey = GUILayout.TextField(_deriveKey) ?? string.Empty;

            if (!string.IsNullOrEmpty(_originalAddressHex) &&
                !string.IsNullOrEmpty(_deriveKey) &&
                GUILayout.Button("Derive"))
            {
                Address originalAddress;
                try
                {
                    originalAddress = new Address(_originalAddressHex);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                var derivedAddress = originalAddress.Derive(_deriveKey);
                _derivedAddressHex = derivedAddress.ToHex();
            }

            GUILayout.Label("Derived Address Hex", EditorStyles.boldLabel);
            GUILayout.TextArea(_derivedAddressHex, EditorStyles.label);
        }
    }
}
