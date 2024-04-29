using System.IO;
using System.Text;
using Editor;
using Lib9c.DevExtensions.Model;
using Nekoyume;
using Nekoyume.Game.ScriptableObject;
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
            const string path = "Assets/_Scripts/Lib9c/lib9c/Lib9c.DevExtensions/Data/";
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
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Testbed/TestbedTool.uxml");
        var labelFromUxml = visualTree.Instantiate();
        root.Add(labelFromUxml);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Testbed/TestbedTool.uss");
        root.styleSheets.Add(styleSheet);

        // for testbed sell
        var sellObjectField = EditorQueryHelper.FindByName<ObjectField>(root, "sell-field");
        sellObjectField.allowSceneObjects = true;
        sellObjectField.objectType = typeof(TestbedSellScriptableObject);
        var sell =
            AssetDatabase.LoadAssetAtPath<TestbedSellScriptableObject>(
                "Assets/Editor/Testbed/TestbedSell.asset");
        sellObjectField.value = sell;
        EditorQueryHelper.FindByName<Button>(root, "sell-export-button").clickable.clicked +=
            OnClickSellExport;
        EditorQueryHelper.FindByName<Button>(root, "sell-import-button").clickable.clicked +=
            OnClickSellImport;

        // for testbed create avatar
        var createAvatarObjectField =
            EditorQueryHelper.FindByName<ObjectField>(root, "create-avatar-field");
        createAvatarObjectField.allowSceneObjects = true;
        createAvatarObjectField.objectType = typeof(TestbedCreateAvatarScriptableObject);
        var createAvatar =
            AssetDatabase.LoadAssetAtPath<TestbedCreateAvatarScriptableObject>(
                "Assets/Editor/Testbed/TestbedCreateAvatar.asset");
        createAvatarObjectField.value = createAvatar;
        EditorQueryHelper.FindByName<Button>(root, "create-avatar-export-button").clickable
                .clicked +=
            OnClickCreateAvatarExport;
        EditorQueryHelper.FindByName<Button>(root, "create-avatar-import-button").clickable
                .clicked +=
            OnClickCreateAvatarImport;
    }

    private void OnClickSellExport()
    {
        NcDebug.Log("[OnClickSellExport]");
        Export<TestbedSellScriptableObject, TestbedSell>("TestbedSell", "sell-field");
    }

    private void OnClickCreateAvatarExport()
    {
        NcDebug.Log("[OnClickCreateAvatarExport]");
        Export<TestbedCreateAvatarScriptableObject, TestbedCreateAvatar>(
            "TestbedCreateAvatar", "create-avatar-field");
    }

    private void OnClickSellImport()
    {
        NcDebug.Log("[OnClickImport]");
        Import<TestbedSellScriptableObject, TestbedSell>("sell-field");
    }

    private void OnClickCreateAvatarImport()
    {
        NcDebug.Log("[OnClickImport]");
        Import<TestbedCreateAvatarScriptableObject, TestbedCreateAvatar>("create-avatar-field");
    }

    private void Export<T1, T2>(string fileName, string objectFieldName)
        where T1 : BaseTestbedScriptableObject<T2> where T2 : BaseTestbedModel
    {
        var path = Path.Combine(DataPath, $"{fileName}.json");
        AssetDatabase.Refresh();
        if (!File.Exists(path))
        {
            var file = File.CreateText(path);
            file.Flush();
            file.Close();
        }

        var objectField =
            EditorQueryHelper.FindByName<ObjectField>(rootVisualElement, $"{objectFieldName}");
        var scriptableObject = objectField.value as T1;
        var json = JsonUtility.ToJson(scriptableObject.Data, true);
        SaveJsonFile(path, json);
        ConfirmPopup("Alert", "Save success", $"PATH : {path}");
    }

    private void Import<T1, T2>(string objectFieldName)
        where T1 : BaseTestbedScriptableObject<T2> where T2 : BaseTestbedModel
    {
        var path = EditorUtility.OpenFilePanel("Load data", DataPath, "json");
        if (path.Length <= 0)
        {
            return;
        }

        var objectField =
            EditorQueryHelper.FindByName<ObjectField>(rootVisualElement, $"{objectFieldName}");
        var scriptableObject = objectField.value as T1;
        var data = LoadJsonFile<T2>(path);
        scriptableObject.Data = data;
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

    private T LoadJsonFile<T>(string path) where T : BaseTestbedModel
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

        NcDebug.Log("confirm");
        AssetDatabase.Refresh();
    }
}
