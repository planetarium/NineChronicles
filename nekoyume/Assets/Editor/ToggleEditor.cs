﻿using Nekoyume.UI.Module;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(Toggle))]
    public class ToggleEditor : UnityEditor.UI.ToggleEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onClickToggle"));
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }

            base.OnInspectorGUI();
        }
    }
}
