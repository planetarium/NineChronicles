using Libplanet.Crypto;
using Nekoyume.Helper;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class KeyStoreEditor : EditorWindow
    {
        private static string _addr;
        private static string _originPw;
        private static string _newPw;

        [MenuItem("Tools/Show Keystore Editor")]
        private static void Init()
        {
            _addr = "0xABCD";
            _originPw = "origin password";
            _newPw = "new password";
            var window = GetWindow(typeof(KeyStoreEditor));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("reset protected private key");
            _addr = EditorGUILayout.TextField("Address to reset: ", _addr);
            _originPw = EditorGUILayout.TextField("Origin password: ", _originPw);
            _newPw = EditorGUILayout.TextField("Password for new setting: ", _newPw);
            if (GUILayout.Button("Reset password"))
            {
                Debug.Log("Trying to reset password...");
                KeyStoreHelper.ResetPassword(new Address(_addr), _originPw, _newPw);
            }
        }
    }
}
