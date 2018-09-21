using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[CustomEditor(typeof(IkGroup))]
	public class IkGroupEditor : Editor
	{
		ReorderableList mList = null;

		void OnEnable()
		{
			SetupList();
		}
		
		void SetupList()
		{
			SerializedProperty ikListProperty = serializedObject.FindProperty("m_IkComponents");
			
			if(ikListProperty != null)
			{
				mList = new ReorderableList(serializedObject,ikListProperty,true,true,true,true);
				
				mList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					
					SerializedProperty boneProperty = mList.serializedProperty.GetArrayElementAtIndex(index);
					
					rect.y += 1.5f;
					
					EditorGUI.PropertyField( new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), boneProperty, GUIContent.none);
				};
				
				mList.drawHeaderCallback = (Rect rect) => {  
					EditorGUI.LabelField(rect, "IK Components");
				};
				
				mList.onSelectCallback = (ReorderableList list) => {};
			}
		}
		
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			
			serializedObject.Update();

			if(mList != null)
			{
				mList.DoLayoutList();
			}

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
