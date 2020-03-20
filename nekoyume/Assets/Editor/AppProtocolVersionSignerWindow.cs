using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;

namespace Editor
{
    public sealed class AppProtocolVersionSignerWindow : EditorWindow
    {
        public string MacOSBinaryUrl = string.Empty;

        public string WindowsBinaryUrl = string.Empty;

        public PrivateKey PrivateKey;

        private string privateKeyHex = string.Empty;

        public int Version;

        private string versionString = "1";

        private DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        public AppProtocolVersion AppProtocolVersion;

        [MenuItem("Tools/Libplanet/Sign A New Version")]
        public static void Init()
        {
            var window = EditorWindow.GetWindow<AppProtocolVersionSignerWindow>();
            window.maxSize = new Vector2(600, 450);
            window.timestamp = DateTimeOffset.UtcNow;
            window.Show();
        }
 
        void OnGUI()
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

            privateKeyHex = EditorGUILayout.TextField("Private Key", privateKeyHex);
            if (privateKeyHex.Equals(string.Empty))
            {
                PrivateKey = null;
            }
            else
            {
                try
                {
                    string hex = privateKeyHex.Trim();

                    if (hex.Equals(string.Empty))
                    {
                        PrivateKey = null;
                        return;
                    }

                    PrivateKey = new PrivateKey(ByteUtil.ParseHex(hex));
                }
                catch (Exception e)
                {
                    ShowError(e.Message);
                }
            }

            MacOSBinaryUrl = EditorGUILayout.TextField("macOS Binary URL", MacOSBinaryUrl);
            WindowsBinaryUrl = EditorGUILayout.TextField("Windows Binary URL", WindowsBinaryUrl);

            if (PrivateKey is PrivateKey key)
            {
                Bencodex.Types.Dictionary downloadUrls = Bencodex.Types.Dictionary.Empty
                    .Add("macOS", MacOSBinaryUrl)
                    .Add("Windows", WindowsBinaryUrl);
                Bencodex.Types.Dictionary extra = Bencodex.Types.Dictionary.Empty
                    .Add("downloadUrls", downloadUrls)
                    .Add("timestamp", $"{timestamp:O}");

                AppProtocolVersion = AppProtocolVersion.Sign(key, Version, extra);
                GUILayout.TextArea(AppProtocolVersion.Token);
            }
        }
        
        void ShowError(string message)
        {
            if (message is null)
            {
                return ;
            }

            var style = new GUIStyle();
            style.normal.textColor = Color.red;
            EditorGUILayout.LabelField(message, style);
        }
    }
}
