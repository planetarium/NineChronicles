using System;
using System.IO;
using System.Text;
using Editor;
using Lib9c.DevExtensions.Model;
using Nekoyume;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

public class TestbedTool : EditorWindow
{
    private static string DataPath
    {
        get
        {
            var path =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "planetarium", "testbed");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }

    [MenuItem("Tools/Testbed Tool")]
    public static void ShowExample()
    {
        var wnd = GetWindow<TestbedTool>();
        wnd.titleContent = new GUIContent("Testbed Tool");
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var visualTree =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TestbedTool.uxml");
        var labelFromUxml = visualTree.Instantiate();
        root.Add(labelFromUxml);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/TestbedTool.uss");
        root.styleSheets.Add(styleSheet);

        var objectField = EditorQueryHelper.FindByName<ObjectField>(root, "the-uxml-field");
        objectField.allowSceneObjects = true;
        objectField.objectType = typeof(TestbedToolScriptableObject);
        objectField.value =
            Resources.Load<TestbedToolScriptableObject>("ScriptableObject/TestbedSell");
        EditorQueryHelper.FindByName<Button>(root, "sell-export-button").clickable.clicked +=
            OnClickSellExport;
        EditorQueryHelper.FindByName<Button>(root, "sell-import-button").clickable.clicked +=
            OnClickSellImport;
    }

    private void OnClickSellExport()
    {
        Debug.Log("[OnClickExport]");
        var path = $"{DataPath}\\TestbedSell.json";
        AssetDatabase.Refresh();
        if (!File.Exists(path))
        {
            var file = File.CreateText(path);
            file.Flush();
            file.Close();
        }

        var objectField =
            EditorQueryHelper.FindByName<ObjectField>(rootVisualElement, "the-uxml-field");
        var scriptableObject = objectField.value as TestbedToolScriptableObject;
        var json = JsonUtility.ToJson(scriptableObject.data, true);
        SaveJsonFile(path, json);
        ConfirmPopup("Alert", "Save success", $"PATH : {path}");
    }

    private void OnClickSellImport()
    {
        Debug.Log("[OnClickImport]");
        var path = EditorUtility.OpenFilePanel("Load data", DataPath, "json");
        if (path.Length <= 0)
        {
            return;
        }

        var data = LoadJsonFile<TestbedSell>(path);
        var objectField =
            EditorQueryHelper.FindByName<ObjectField>(rootVisualElement, "the-uxml-field");
        var scriptableObject = objectField.value as TestbedToolScriptableObject;
        scriptableObject.data = data;
        AssetDatabase.Refresh();
        ConfirmPopup("Alert", "Load success");
    }

    private void SaveJsonFile(string path, string jsonData)
    {
        var fileStream = new FileStream(path, FileMode.Create);
        var data = Encoding.UTF8.GetBytes(jsonData);
        fileStream.Write(data, 0, data.Length);
        fileStream.Close();
    }

    private T LoadJsonFile<T>(string path)
    {
        var fileStream = new FileStream(path, FileMode.Open);
        var data = new byte[fileStream.Length];
        fileStream.Read(data, 0, data.Length);
        fileStream.Close();
        var jsonData = Encoding.UTF8.GetString(data);
        return JsonUtility.FromJson<T>(jsonData);
    }

    private void ConfirmPopup(string title, string text, string content = "")
    {
        var transforms = Selection.GetTransforms(SelectionMode.Deep |
                                                 SelectionMode.ExcludePrefab |
                                                 SelectionMode.Editable);
        if (EditorUtility.DisplayDialog(title, $"{text}\n{content}", "confirm"))
        {
            foreach (var transform in transforms)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, -Vector3.up, out hit))
                {
                }
            }
        }

        Debug.Log("confirm");
        AssetDatabase.Refresh();
    }
}
