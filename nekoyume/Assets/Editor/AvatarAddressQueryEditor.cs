using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class AvatarAddressQueryEditor : EditorWindow
    {
        private static string _avatarAddress;
        private static string _avatarQuery;
        private static string _inventoryQuery;
        private static string _allRuneQuery;
        private static string _runeSlotQuery;
        private static string _collectionQuery;

        [MenuItem("Tools/Show AvatarAddressQuery Editor")]
        private static void Init()
        {
            _avatarAddress = string.Empty;
            var window = GetWindow(typeof(AvatarAddressQueryEditor));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("아바타 주소로 시뮬레이션에 필요한 상태를 가져오는 쿼리를 생성해줍니다.");
            _avatarAddress = EditorGUILayout.TextField("avatar address: ", _avatarAddress);
            if (GUILayout.Button("try to generate gql"))
            {
                Debug.Log("Trying...");
                const string query =
                    "state(accountAddress:\"{0}\", address:\"{1}\")";
                _avatarQuery = $"query {{ {string.Format(format: query, Addresses.Avatar.ToString(), _avatarAddress)} }}";
                _inventoryQuery = $"query {{ {string.Format(format: query, Addresses.Inventory.ToString(), _avatarAddress)} }}";
                _allRuneQuery = $"query {{ {string.Format(format: query, Addresses.RuneState.ToString(), _avatarAddress)} }}";
                _runeSlotQuery = $"query {{ {string.Format(format: query, ReservedAddresses.LegacyAccount.ToString(), RuneSlotState.DeriveAddress(new Address(_avatarAddress), BattleType.Arena))} }}";
                _collectionQuery = $"query {{ {string.Format(format: query, Addresses.Collection.ToString(), _avatarAddress)} }}";
            }

            CopyableLabelField("avatar state: ", _avatarQuery);
            CopyableLabelField("inventory state: ", _inventoryQuery);
            CopyableLabelField("all rune state: ", _allRuneQuery);
            CopyableLabelField("rune slot state: ", _runeSlotQuery);
            CopyableLabelField("collection state: ", _collectionQuery);
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
    }
}
