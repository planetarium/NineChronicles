using Nekoyume.UI.Module;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(NCToggleDropdown))]
    public class NCToggleDropdownEditor : ToggleEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offObject"));
            DrawList(serializedObject.FindProperty("items"), "item");

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }

            base.OnInspectorGUI();
        }

        private void DrawList(SerializedProperty listProperty, string labalName)
        {
            listProperty.isExpanded =
                EditorGUILayout.Foldout(listProperty.isExpanded, listProperty.name);
            if(listProperty.isExpanded)
            {
                EditorGUILayout.PropertyField(listProperty.FindPropertyRelative("Array.size"));
                int Count = listProperty.arraySize;
                for (int i = 0; i < Count; ++i)
                {
                    EditorGUILayout.PropertyField(listProperty.GetArrayElementAtIndex(i), new GUIContent(labalName + i));
                }
            }
        }
    }
}
