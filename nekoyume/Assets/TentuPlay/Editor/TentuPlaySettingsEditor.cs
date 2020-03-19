using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace TentuPlay
{
    [CustomEditor(typeof(TentuPlaySettings))]
    public class TentuPlaySettingsEditor : UnityEditor.Editor
    {
        GUIContent apiKeyLabel = new GUIContent("Api Key [?]:", "For TentuPlay Api Key, contact us at contact@tentuplay.io");
        GUIContent secretLabel = new GUIContent("Secret [?]:", "For TentuPlay Secret, contact us at contact@tentuplay.io");
        GUIContent debugLabel = new GUIContent("TentuPlay Debug Mode [?]:", "Run TentuPlay in debug mode");
        GUIContent usePersistDBConnLabel = new GUIContent("Use Persistent DB Connection [?]:", "Use persistent client database connection. Default value is false.");
        GUIContent autoUploadLabel = new GUIContent("Auto Upload [?]:", "Check to automatically upload the data from the client to the server.");
        GUIContent deferredSendIntervalSecLabel = new GUIContent("Upload Interval (sec) [?]:", "Minimum server upload interval (1200 seconds recommended)");


        const string UnityAssetFolder = "Assets";

        public static TentuPlaySettings GetOrCreateSettingsAsset()
        {
            string fullPath = Path.Combine(Path.Combine(UnityAssetFolder, TentuPlaySettings.tpSettingsPath),
                               TentuPlaySettings.tpSettingsAssetName + TentuPlaySettings.tpSettingsAssetExtension
                               );
            TentuPlaySettings instance = AssetDatabase.LoadAssetAtPath(fullPath, typeof(TentuPlaySettings)) as TentuPlaySettings;

            if (instance == null)
            {
                // no asset found, we need to create it. 

                if (!Directory.Exists(Path.Combine(UnityAssetFolder, TentuPlaySettings.tpSettingsPath)))
                {
                    AssetDatabase.CreateFolder(Path.Combine(UnityAssetFolder, "TentuPlay"), "Resources");
                }

                instance = CreateInstance<TentuPlaySettings>();
                AssetDatabase.CreateAsset(instance, fullPath);
                AssetDatabase.SaveAssets();
            }
            return instance;
        }

        [MenuItem("TentuPlay/Edit Settings")]
        public static void Edit()
        {
            Selection.activeObject = GetOrCreateSettingsAsset();

            ShowInspector();
        }

        [MenuItem("TentuPlay/TentuPlay Website")]
        public static void OpenWeb()
        {
            Application.OpenURL("https://tentuplay.io/");
        }


        void OnDisable()
        {
            // make sure the runtime code will load the Asset from Resources when it next tries to access this. 
            TentuPlaySettings.SetInstance(null);
        }

        static TentuPlaySettingsEditor()
        {
        }

        private static void RecursiveDeleteFolders(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.GetDirectories("*", SearchOption.AllDirectories))
            {
                RecursiveDeleteFolders(dir);
            }

            try
            {
                if (baseDir.GetFiles().Length == 0)
                {
                    baseDir.Delete(true);

                    AssetDatabase.Refresh();
                }
            }
            catch
            {
            }
        }

        public override void OnInspectorGUI()
        {
            TentuPlaySettings settings = (TentuPlaySettings)target;
            TentuPlaySettings.SetInstance(settings);

            EditorGUILayout.BeginHorizontal();
            TentuPlaySettings.ApiKey = EditorGUILayout.TextField(apiKeyLabel, TentuPlaySettings.ApiKey).Trim();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            TentuPlaySettings.Secret = EditorGUILayout.TextField(secretLabel, TentuPlaySettings.Secret).Trim();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            TentuPlaySettings.DEBUG = EditorGUILayout.Toggle(debugLabel, TentuPlaySettings.DEBUG);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            TentuPlaySettings.UsePersistDBConn = EditorGUILayout.Toggle(usePersistDBConnLabel, TentuPlaySettings.UsePersistDBConn);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            TentuPlaySettings.AutoUpload = EditorGUILayout.Toggle(autoUploadLabel, TentuPlaySettings.AutoUpload);
            EditorGUILayout.EndHorizontal();

            if (TentuPlaySettings.AutoUpload)
            {
                EditorGUILayout.BeginHorizontal();
                TentuPlaySettings.DeferredSendIntervalSec = EditorGUILayout.IntField(deferredSendIntervalSecLabel, TentuPlaySettings.DeferredSendIntervalSec);
                //trim 
                EditorGUILayout.EndHorizontal();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        private static void ShowInspector()
        {
            try
            {
                var editorAsm = typeof(UnityEditor.Editor).Assembly;
                var type = editorAsm.GetType("UnityEditor.InspectorWindow");
                Object[] findObjectsOfTypeAll = Resources.FindObjectsOfTypeAll(type);

                if (findObjectsOfTypeAll.Length > 0)
                {
                    ((EditorWindow)findObjectsOfTypeAll[0]).Focus();
                }
                else
                {
                    EditorWindow.GetWindow(type);
                }
            }
            catch
            {
            }
        }
    }
}