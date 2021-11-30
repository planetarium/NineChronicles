using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public static class EditorQueryHelper
    {
        public static T FindByName<T>(VisualElement root, string elementName)
            where T : VisualElement
        {
            var result = root.Q<T>(elementName);
            if (result is null)
            {
                Debug.LogError($"[ERROR] not found elementName : {elementName}");
                EditorUtility.DisplayDialog("[ERROR] Alert / EditorQueryHelper.FindByName ",
                    $"Not found = {elementName}", "ok");
            }

            return result;
        }

        public static bool IsExistName<T>(VisualElement root, string elementName, out T result)
            where T : VisualElement
        {
            result = root.Q<T>(elementName);
            return result != null;
        }

        public static T FindByClass<T>(VisualElement root, string className) where T : VisualElement
        {
            var result = root.Q<T>(className: className);
            if (result is null)
            {
                Debug.LogError($"[ERROR] not found className = {className}");
                EditorUtility.DisplayDialog("[ERROR] Alert / EditorQueryHelper.FindByClass ",
                    $"Not found : {className}", "ok");
            }

            return result;
        }

        public static void FindQuery(VisualElement root, string[] testName)
        {
            root.Query(classes: testName).ForEach((result) =>
            {
                Debug.Log($"FindQuery / name : {result.name}");
            });
        }

        public static bool IsChild(VisualElement parent, VisualElement child)
        {
            var result = parent.Q<VisualElement>(child.name);
            return result != null;
        }
    }
}
