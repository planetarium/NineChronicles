using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Globalization;
using System.Linq;
using Libplanet;
using Libplanet.KeyStore;
using Nekoyume;
using UnityEngine;
using UnityEditor;

namespace Editor
{
    public sealed class AppProtocolVersionSignerWindow : EditorWindow
    {
        private bool _showParameters = true;

        public string macOSBinaryUrl = string.Empty;

        public string windowsBinaryUrl = string.Empty;

        public int version;

        private string _versionString = "1";

        private DateTimeOffset _timestamp = DateTimeOffset.UtcNow;

        private AppProtocolVersion? _appProtocolVersion;

        private bool _showPrivateKey = true;

        private IKeyStore _keyStore;

        private Tuple<Guid, ProtectedPrivateKey>[] _privateKeys;

        private string[] _privateKeyOptions;

        private int _selectedPrivateKeyIndex;

        private bool _toggledOnTypePrivateKey;

        private string _privateKey;

        private string _privateKeyPassphrase;

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
            if (_keyStore is null)
            {
                FillAttributes();
            }

            RefreshPrivateKeys();
        }

        void OnGUI()
        {
            _showPrivateKey = EditorGUILayout.Foldout(_showPrivateKey, "Private Key");
            if (_showPrivateKey)
            {
                _selectedPrivateKeyIndex = EditorGUILayout.Popup(
                    "Private Key",
                    _selectedPrivateKeyIndex,
                    _privateKeyOptions);
                if (_selectedPrivateKeyIndex == _privateKeyOptions.Length - 1)
                {
                    _toggledOnTypePrivateKey = EditorGUILayout.Toggle(
                        "Type New Private Key",
                        _toggledOnTypePrivateKey);
                    if (_toggledOnTypePrivateKey)
                    {
                        _privateKey =
                            EditorGUILayout.PasswordField("New Private Key", _privateKey) ??
                            string.Empty;
                        ShowError(_privateKey.Any() ? null : "New private key is empty.");
                    }
                }

                _privateKeyPassphrase =
                    EditorGUILayout.PasswordField("Passphrase", _privateKeyPassphrase) ??
                    string.Empty;
                ShowError(_privateKeyPassphrase.Any() ? null : "Passphrase is empty.");

                if (_selectedPrivateKeyIndex == _privateKeyOptions.Length - 1)
                {
                    EditorGUI.BeginDisabledGroup(!_privateKeyPassphrase.Any());
                    if (GUILayout.Button("Create"))
                    {
                        var privateKey = _toggledOnTypePrivateKey
                            ? new PrivateKey(ByteUtil.ParseHex(_privateKey))
                            : new PrivateKey();
                        var ppk = ProtectedPrivateKey.Protect(privateKey, _privateKeyPassphrase);
                        _keyStore.Add(ppk);
                        RefreshPrivateKeys();
                        _selectedPrivateKeyIndex = Array.IndexOf(_privateKeys, privateKey);
                    }

                    EditorGUI.EndDisabledGroup();
                }
            }

            HorizontalLine();

            _showParameters = EditorGUILayout.Foldout(_showParameters, "Parameters");
            if (_showParameters)
            {
                _versionString = EditorGUILayout.TextField("Version", _versionString);
                try
                {
                    version = int.Parse(_versionString, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    ShowError(e.Message);
                }

                macOSBinaryUrl = EditorGUILayout.TextField("macOS Binary URL", macOSBinaryUrl);
                windowsBinaryUrl =
                    EditorGUILayout.TextField("Windows Binary URL", windowsBinaryUrl);
            }

            HorizontalLine();

            EditorGUI.BeginDisabledGroup(
                !(_privateKeyPassphrase.Any() &&
                  _selectedPrivateKeyIndex < _privateKeyOptions.Length - 1));
            if (GUILayout.Button("Sign"))
            {
                var appProtocolVersionExtra =
                    new AppProtocolVersionExtra(macOSBinaryUrl, windowsBinaryUrl, _timestamp);

                PrivateKey key;
                try
                {
                    key = _privateKeys[_selectedPrivateKeyIndex].Item2
                        .Unprotect(_privateKeyPassphrase);
                }
                catch (IncorrectPassphraseException)
                {
                    EditorUtility.DisplayDialog(
                        "Unmatched passphrase",
                        "Private key passphrase is incorrect.",
                        "Retype passphrase"
                    );
                    _privateKeyPassphrase = string.Empty;
                    return;
                }

                _appProtocolVersion = AppProtocolVersion.Sign(
                    key,
                    version,
                    appProtocolVersionExtra.Serialize());
            }

            EditorGUI.EndDisabledGroup();

            if (_appProtocolVersion is AppProtocolVersion v)
            {
                GUILayout.TextArea(v.Token);
            }
        }

        void FillAttributes()
        {
            _keyStore = Web3KeyStore.DefaultKeyStore;
            maxSize = new Vector2(600, 450);
            _timestamp = DateTimeOffset.UtcNow;
            titleContent = new GUIContent("Libplanet Version Signer");
        }

        void RefreshPrivateKeys()
        {
            _privateKeys = _keyStore
                .List()
                .OrderBy(pair => pair.Item1)
                .ToArray();
            if (_privateKeys.Any())
            {
                _privateKeyOptions = _keyStore
                    .List()
                    .Select(pair =>
                        $"{pair.Item2.Address} ({pair.Item1.ToString().ToLower()})"
                    )
                    .Append("Create a new private key:")
                    .ToArray();
            }
            else
            {
                _privateKeyOptions = new[] {"No private key; create one first:"};
            }
        }

        static void ShowError(string message)
        {
            if (message is null)
            {
                return;
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
