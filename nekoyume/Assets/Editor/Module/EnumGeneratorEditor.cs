using System;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    public class EnumGeneratorEditor<T> : UnityEditor.Editor where T : Enum
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = (ScriptableObjectIncludeEnum<T>) target;

            if (GUILayout.Button("Apply List to Enum", GUILayout.Height(40)))
            {
                EnumGenerator.Generate(script.type, script.Enums);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("success", "Apply Complete!", "ok");
            }

            if (GUILayout.Button("Get Enum to List", GUILayout.Height(40)))
            {
                var list = EnumGenerator.EnumToList(script.type);
                script.Enums = list;
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", "Import Complete!", "ok");
            }
        }
    }
}
