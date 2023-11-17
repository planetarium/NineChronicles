using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
public class AirbridgeData : ScriptableObject
{
    private const string assetPath = "Assets/Airbridge/Resources";
    private const string assetName = "AirbridgeData";
    private const string assetExtension = ".asset";

    public string appName;
    public string appToken;
    public int logLevel = AirbridgeLogLevel.Default;
    public string sdkSignatureSecretID;
    public string sdkSignatureSecret;
    public string iOSURIScheme;
    public string androidURIScheme;
    public string customDomain;
    public int sessionTimeoutSeconds = 300;
    public bool userInfoHashEnabled = true;
    public bool locationCollectionEnabled = false;
    public bool trackAirbridgeLinkOnly = false;
    public bool autoStartTrackingEnabled = true;
    public bool facebookDeferredAppLinkEnabled = false;
    public int iOSTrackingAuthorizeTimeoutSeconds = 30;

#if UNITY_EDITOR
    private static AirbridgeData instance;
    public static AirbridgeData GetInstance()
    {
        instance = Resources.Load(assetName) as AirbridgeData;

        if (instance == null)
        {
            instance = CreateInstance<AirbridgeData>();
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(assetPath, assetName + assetExtension);
            AssetDatabase.CreateAsset(instance, fullPath);
        }
        return instance;
    }
#endif
}
