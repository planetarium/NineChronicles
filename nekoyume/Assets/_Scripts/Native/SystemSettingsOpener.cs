using System;
using UnityEngine;

namespace Nekoyume.Native
{
    public static class SystemSettingsOpener
    {
        public static void Open(string category)
        {
            NcDebug.Log($"[SystemSettingsOpener] Open System Settings: {category}");
            if (Application.platform != RuntimePlatform.Android)
            {
                NcDebug.LogWarning("SystemSettingOpener is only supported on Android.");
                return;
            }

            try
            {
                using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                var packageName = currentActivityObject.Call<string>("getPackageName");
                using var uriClass = new AndroidJavaClass("android.net.Uri");
                using var uriObject = uriClass.CallStatic<AndroidJavaObject>(
                    "fromParts",
                    "package",
                    packageName,
                    null);
                using var intentObject = new AndroidJavaObject(
                    "android.content.Intent",
                    $"android.settings.{category}",
                    uriObject);
                intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000);
                currentActivityObject.Call("startActivity", intentObject);
            }
            catch (Exception ex)
            {
                NcDebug.LogException(ex);
            }
        }

        public static void OpenApplicationDetailSettings()
        {
            Open("APPLICATION_DETAILS_SETTINGS");
        }
    }
}
