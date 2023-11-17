using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
public class AppTrackingTransparencyData : ScriptableObject
{
    private const string assetPath = "Assets/AppTrackingTransparency/Resources";
    private const string assetName = "AppTrackingTransparencyData";
    private const string assetExtension = ".asset";

    public string userTrackingUsageDescription = "Your data will be used to deliver personalized ads to you";

#if UNITY_EDITOR
    private static AppTrackingTransparencyData instance;
    public static AppTrackingTransparencyData GetInstance()
    {
        instance = Resources.Load(assetName) as AppTrackingTransparencyData;
        if (instance == null)
        {
            instance = CreateInstance<AppTrackingTransparencyData>();
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
