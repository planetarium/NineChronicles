using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Globalization;
using System.Linq;
using Libplanet.KeyStore;
using Nekoyume;
using UnityEngine;
using UnityEditor;

namespace Editor
{
    public sealed class AppProtocolVersionSignerWindow : EditorWindow
    {
        private bool showParameters = true;

        public string MacOSBinaryUrl = string.Empty;

        public string WindowsBinaryUrl = string.Empty;


        public int Version;

        private string versionString = "1";

        private DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        public AppProtocolVersion? AppProtocolVersion;

        private bool showPrivateKey = true;

        public IKeyStore KeyStore;

        private Tuple<Guid, ProtectedPrivateKey>[] privateKeys;

        private string[] privateKeyOptions;

        private int selectedPrivateKeyIndex = 0;

        private string privateKeyPassphrase;

        [MenuItem("Tools/Libplanet/Sign A New Version")]
        public static void Init()
        {
            var window = EditorWindow.GetWindow<AppProtocolVersionSignerWindow>();
            window.Show();
        }

        public void Awake()
        {
            FillAttributes();
        }

        public void OnFocus()
        {
            if (KeyStore is null)
            {
                FillAttributes();
            }

            RefreshPrivateKeys();
        }

        void OnGUI()
        {
            showPrivateKey = EditorGUILayout.Foldout(showPrivateKey, "Private Key");
            if (showPrivateKey)
            {
                selectedPrivateKeyIndex = EditorGUILayout.Popup("Private Key", selectedPrivateKeyIndex, privateKeyOptions);
                privateKeyPassphrase = EditorGUILayout.PasswordField("Passphrase", privateKeyPassphrase) ?? string.Empty;
                ShowError(privateKeyPassphrase.Any() ? null : "Passphrase is empty.");

                if (selectedPrivateKeyIndex == privateKeyOptions.Length - 1)
                {
                    EditorGUI.BeginDisabledGroup(!privateKeyPassphrase.Any());
                    if (GUILayout.Button("Create"))
                    {
                        var privateKey = new PrivateKey();
                        ProtectedPrivateKey ppk =
                            ProtectedPrivateKey.Protect(privateKey, privateKeyPassphrase);
                        KeyStore.Add(ppk);
                        RefreshPrivateKeys();
                        selectedPrivateKeyIndex = Array.IndexOf(privateKeys, privateKey);
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }

            HorizontalLine();

            showParameters = EditorGUILayout.Foldout(showParameters, "Parameters");
            if (showParameters)
            {
                versionString = EditorGUILayout.TextField("Version", versionString);
                try
                {
                    Version = int.Parse(versionString, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    ShowError(e.Message);
                }

                MacOSBinaryUrl = EditorGUILayout.TextField("macOS Binary URL", MacOSBinaryUrl);
                WindowsBinaryUrl =
                    EditorGUILayout.TextField("Windows Binary URL", WindowsBinaryUrl);
            }

            HorizontalLine();

            EditorGUI.BeginDisabledGroup(
                !(privateKeyPassphrase.Any() && selectedPrivateKeyIndex < privateKeyOptions.Length - 1)
            );
            if (GUILayout.Button("Sign"))
            {
                var appProtocolVersionExtra =
                    new AppProtocolVersionExtra(MacOSBinaryUrl, WindowsBinaryUrl, timestamp);

                PrivateKey key;
                try
                {
                    key = privateKeys[selectedPrivateKeyIndex].Item2.Unprotect(privateKeyPassphrase);
                }
                catch (IncorrectPassphraseException)
                {
                    EditorUtility.DisplayDialog(
                        "Unmatched passphrase",
                        "Private key passphrase is incorrect.",
                        "Retype passphrase"
                    );
                    privateKeyPassphrase = string.Empty;
                    return;
                }

                AppProtocolVersion = Libplanet.Net.AppProtocolVersion.Sign(
                    key,
                    Version,
                    appProtocolVersionExtra.Serialize());
            }
            EditorGUI.EndDisabledGroup();

            if (AppProtocolVersion is Libplanet.Net.AppProtocolVersion v)
            {
                GUILayout.TextArea(v.Token);
            }
        }

        void FillAttributes()
        {
            KeyStore = Web3KeyStore.DefaultKeyStore;
            maxSize = new Vector2(600, 450);
            timestamp = DateTimeOffset.UtcNow;
            titleContent = new GUIContent("Libplanet Version Signer");
        }

        void RefreshPrivateKeys()
        {
            privateKeys = KeyStore.List().OrderBy(pair => pair.Item1).ToArray();
            if (privateKeys.Any())
            {
                privateKeyOptions = KeyStore.List().Select(pair =>
                    $"{pair.Item2.Address} ({pair.Item1.ToString().ToLower()})"
                ).Append("Create a new private key:").ToArray();
            }
            else
            {
                privateKeyOptions = new[] { "No private key; create one first:" };
            }
        }

        static void ShowError(string message)
        {
            if (message is null)
            {
                return ;
            }

            var style = new GUIStyle();
            style.normal.textColor = Color.red;
            EditorGUILayout.LabelField(message, style);
        }

        static void HorizontalLine()
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(6));
            r.height = 1;
            r.y += 5;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, Color.gray);
        }
    }
}
