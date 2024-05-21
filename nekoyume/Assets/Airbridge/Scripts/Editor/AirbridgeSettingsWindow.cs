#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class AirbridgeSettingsWindow : EditorWindow
{
    [MenuItem("AB180/Airbridge Settings")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AirbridgeSettingsWindow), true, "Airbridge Settings");
    }

    private void OnGUI()
    {
        EditorGUIUtility.labelWidth = 240;
        SerializedObject serializedAirbridgeData = new SerializedObject(AirbridgeData.GetInstance());

        SerializedProperty appNameProperty = serializedAirbridgeData.FindProperty("appName");
        EditorGUILayout.PropertyField(appNameProperty, new GUILayoutOption[] { });

        SerializedProperty appTokenProperty = serializedAirbridgeData.FindProperty("appToken");
        EditorGUILayout.PropertyField(appTokenProperty, new GUILayoutOption[] { });

        SerializedProperty logLevel = serializedAirbridgeData.FindProperty("logLevel");
        logLevel.intValue = EditorGUILayout.Popup("Log Level", logLevel.intValue, AirbridgeLogLevel.LogLevel);

        SerializedProperty iOSURISchemeProperty = serializedAirbridgeData.FindProperty("iOSURIScheme");
        EditorGUILayout.PropertyField(iOSURISchemeProperty, new GUIContent("iOS URI Scheme"), new GUILayoutOption[] { });

        SerializedProperty androidURISchemeProperty = serializedAirbridgeData.FindProperty("androidURIScheme");
        EditorGUILayout.PropertyField(androidURISchemeProperty, new GUILayoutOption[] { });

        SerializedProperty customDomainProperty = serializedAirbridgeData.FindProperty("customDomain");
        EditorGUILayout.PropertyField(customDomainProperty, new GUILayoutOption[] { });

        SerializedProperty sessionTimeoutSecondsProperty = serializedAirbridgeData.FindProperty("sessionTimeoutSeconds");
        EditorGUILayout.PropertyField(sessionTimeoutSecondsProperty, new GUILayoutOption[] { });

        SerializedProperty userInfoHashEnabledProperty = serializedAirbridgeData.FindProperty("userInfoHashEnabled");
        EditorGUILayout.PropertyField(userInfoHashEnabledProperty, new GUILayoutOption[] { });

        SerializedProperty locationCollectionEnabledProperty = serializedAirbridgeData.FindProperty("locationCollectionEnabled");
        EditorGUILayout.PropertyField(locationCollectionEnabledProperty, new GUILayoutOption[] { });

        SerializedProperty trackAirbridgeLinkOnlyProperty = serializedAirbridgeData.FindProperty("trackAirbridgeLinkOnly");
        EditorGUILayout.PropertyField(trackAirbridgeLinkOnlyProperty, new GUILayoutOption[] { });

        SerializedProperty autoStartTrackingEnabledProperty = serializedAirbridgeData.FindProperty("autoStartTrackingEnabled");
        EditorGUILayout.PropertyField(autoStartTrackingEnabledProperty, new GUILayoutOption[] { });

        SerializedProperty facebookDeferredAppLinkEnabledProperty = serializedAirbridgeData.FindProperty("facebookDeferredAppLinkEnabled");
        EditorGUILayout.PropertyField(facebookDeferredAppLinkEnabledProperty, new GUILayoutOption[] { });

        SerializedProperty iOSTrackingAuthorizeTimeoutSecondsProperty = serializedAirbridgeData.FindProperty("iOSTrackingAuthorizeTimeoutSeconds");
        EditorGUILayout.PropertyField(iOSTrackingAuthorizeTimeoutSecondsProperty, new GUILayoutOption[] { });

        SerializedProperty sdkSignatureSecretIDProperty = serializedAirbridgeData.FindProperty("sdkSignatureSecretID");
        EditorGUILayout.PropertyField(sdkSignatureSecretIDProperty, new GUILayoutOption[] { });

        SerializedProperty sdkSignatureSecretProperty = serializedAirbridgeData.FindProperty("sdkSignatureSecret");
        EditorGUILayout.PropertyField(sdkSignatureSecretProperty, new GUILayoutOption[] { });

        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Update iOS Setting", new GUILayoutOption[] { GUILayout.Height(30) }))
        {
            try
            {
                UpdateiOSAppSetting();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Updated iOS Settings");
            }
            catch (Exception)
            {
                Debug.LogError("Something broken while update ios app setting file.");
            }
        }
        if (GUILayout.Button("Update Android Manifest", new GUILayoutOption[] { GUILayout.Height(30) }))
        {
            try
            {
                UpdateAndroidManifest();
                AssetDatabase.Refresh();
                Debug.Log("Updated AndroidManifest.xml");
            }
            catch (Exception)
            {
                Debug.LogError("Something broken while createing android manifest file.");
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (GUI.changed)
        {
            serializedAirbridgeData.ApplyModifiedProperties();
            EditorUtility.SetDirty(AirbridgeData.GetInstance());
            AssetDatabase.SaveAssets();
        }
    }

    public static void UpdateAndroidManifest()
    {
        string metaDataPrefix = "airbridge==";

        string manifestDirPath = Path.Combine(Application.dataPath, "Plugins/Android");
        string defaultManifestPath = Path.Combine(Application.dataPath, "Plugins/Airbridge/Android/AndroidManifest.xml");
        string manifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            Debug.Log("Couldn't find any anroid manifest file");

            if (!Directory.Exists(manifestDirPath))
            {
                Directory.CreateDirectory(manifestDirPath);
                Debug.LogFormat("Create manifest directiory : {0}", manifestDirPath);
            }

            File.Copy(defaultManifestPath, manifestPath);
            Debug.LogFormat("Copied default android manifest file from \'{0}\'", defaultManifestPath);
        }

        AndroidManifest manifest = new AndroidManifest(manifestPath);
        manifest.SetPackageName(Application.identifier);
        manifest.SetPermission("android.permission.INTERNET");
        manifest.SetPermission("android.permission.ACCESS_NETWORK_STATE");
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.app_name", metaDataPrefix + AirbridgeData.GetInstance().appName);
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.app_token", metaDataPrefix + AirbridgeData.GetInstance().appToken);
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.sdk_signature_secret_id", metaDataPrefix + AirbridgeData.GetInstance().sdkSignatureSecretID);
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.sdk_signature_secret", metaDataPrefix + AirbridgeData.GetInstance().sdkSignatureSecret);
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.log_level", AirbridgeLogLevel.GetAndroidLogLevel(AirbridgeData.GetInstance().logLevel));
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.custom_domain", AirbridgeData.GetInstance().customDomain);
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.session_timeout_seconds", AirbridgeData.GetInstance().sessionTimeoutSeconds.ToString());
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.user_info_hash_enabled", AirbridgeData.GetInstance().userInfoHashEnabled.ToString().ToLower());
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.location_collection_enabled", AirbridgeData.GetInstance().locationCollectionEnabled.ToString().ToLower());
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.track_airbridge_link_only", AirbridgeData.GetInstance().trackAirbridgeLinkOnly.ToString().ToLower());
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.auto_start_tracking_enabled", AirbridgeData.GetInstance().autoStartTrackingEnabled.ToString().ToLower());
        manifest.SetAppMetadata("co.ab180.airbridge.sdk.facebook_deferred_app_link_enabled", AirbridgeData.GetInstance().facebookDeferredAppLinkEnabled.ToString().ToLower());
        manifest.SetContentProvider("co.ab180.airbridge.unity.AirbridgeContentProvider", "${applicationId}.co.ab180.airbridge.unity.AirbridgeContentProvider", "false");

        // {http, https}://{appname}.airbridge.io intent filter
        manifest.SetUnityActivityIntentFilter(
            true,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            "http",
            string.Format("{0}.airbridge.io", AirbridgeData.GetInstance().appName)
        );
        manifest.SetUnityActivityIntentFilter(
            true,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            "https",
            string.Format("{0}.airbridge.io", AirbridgeData.GetInstance().appName)
        );
        // {http, https}://{appname}.deeplink.page intent filter
        manifest.SetUnityActivityIntentFilter(
            true,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            "http",
            string.Format("{0}.deeplink.page", AirbridgeData.GetInstance().appName)
        );
        manifest.SetUnityActivityIntentFilter(
            true,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            "https",
            string.Format("{0}.deeplink.page", AirbridgeData.GetInstance().appName)
        );
        // {custom scheme}:// intent filter
        manifest.SetUnityActivityIntentFilter(
            false,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            AirbridgeData.GetInstance().androidURIScheme
        );
        // {http, https}://{custom domain} intent filter
        manifest.SetUnityActivityIntentFilter(
            true,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            "http",
            AirbridgeData.GetInstance().customDomain
        );
        manifest.SetUnityActivityIntentFilter(
            true,
            "android.intent.action.VIEW",
            new string[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
            "https",
            AirbridgeData.GetInstance().customDomain
        );

        manifest.Save(manifestPath);
    }

    public static void UpdateiOSAppSetting()
    {
        string path = Path.Combine(Application.dataPath, "Plugins/Airbridge/iOS/Delegate/AUAppSetting.h");
        if (!File.Exists(path))
        {
            File.Create(path);
        }

        string content =
        "#ifndef AUAppSetting_h\n"
        + "#define AUAppSetting_h\n"
        + "\n"
        + "static NSString* appName = @\"" + AirbridgeData.GetInstance().appName + "\";\n"
        + "static NSString* appToken = @\"" + AirbridgeData.GetInstance().appToken + "\";\n"
        + "static NSString* sdkSignatureSecretID = @\"" + AirbridgeData.GetInstance().sdkSignatureSecretID + "\";\n"
        + "static NSString* sdkSignatureSecret = @\"" + AirbridgeData.GetInstance().sdkSignatureSecret + "\";\n"
        + "static NSUInteger logLevel = " + AirbridgeLogLevel.GetIOSLogLevel(AirbridgeData.GetInstance().logLevel) + ";\n"
        + "static NSString* appScheme = @\"" + AirbridgeData.GetInstance().iOSURIScheme + "\";\n"
        + "static NSInteger sessionTimeoutSeconds = " + AirbridgeData.GetInstance().sessionTimeoutSeconds + ";\n"
        + "static BOOL autoStartTrackingEnabled = " + AirbridgeData.GetInstance().autoStartTrackingEnabled.ToString().ToLower() + ";\n"
        + "static BOOL userInfoHashEnabled = " + AirbridgeData.GetInstance().userInfoHashEnabled.ToString().ToLower() + ";\n"
        + "static BOOL trackAirbridgeLinkOnly = " + AirbridgeData.GetInstance().trackAirbridgeLinkOnly.ToString().ToLower() + ";\n"
        + "static BOOL facebookDeferredAppLinkEnabled = " + AirbridgeData.GetInstance().facebookDeferredAppLinkEnabled.ToString().ToLower() + ";\n"
        + "static NSInteger trackingAuthorizeTimeoutSeconds = " + AirbridgeData.GetInstance().iOSTrackingAuthorizeTimeoutSeconds + ";\n"
        + "\n"
        + "#endif\n";

        File.WriteAllText(path, content);
    }
}
#endif
