using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

namespace Anima2D
{
	[CustomEditor(typeof(SpriteMeshAnimation))]
	public class SpriteMeshAnimationEditor : Editor
	{
		ReorderableList m_List = null;

		SerializedProperty m_FrameListProperty;
		SerializedProperty m_FrameProperty;

		void OnEnable()
		{
			m_FrameListProperty = serializedObject.FindProperty("m_Frames");
			m_FrameProperty = serializedObject.FindProperty("m_Frame");

			SetupList();
		}
		
		void SetupList()
		{
			if(m_FrameListProperty != null)
			{
				m_List = new ReorderableList(serializedObject,m_FrameListProperty,true,true,true,true);
				
				m_List.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					
					SerializedProperty poseProperty = m_List.serializedProperty.GetArrayElementAtIndex(index);
					
					rect.y += 1.5f;
					
					EditorGUI.PropertyField( new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), poseProperty, GUIContent.none);
				};
				
				m_List.drawHeaderCallback = (Rect rect) => {  
					EditorGUI.LabelField(rect, "Frames");
				};
				
				m_List.onSelectCallback = (ReorderableList list) => {};
			}
		}

		override public void OnInspectorGUI()
		{
			serializedObject.Update();

			SpriteMeshAnimation spriteMeshAnimation = target as SpriteMeshAnimation;

			EditorGUI.BeginDisabledGroup(m_FrameListProperty.arraySize == 0);

			EditorGUI.BeginChangeCheck();

			int frame = EditorGUILayout.IntSlider("Frame",spriteMeshAnimation.frame,0,m_FrameListProperty.arraySize-1);

			if(EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spriteMeshAnimation,"Set frame");

				m_FrameProperty.floatValue = (float)frame;
				spriteMeshAnimation.frame = frame;
			}

			EditorGUI.EndDisabledGroup();

			m_List.DoLayoutList();

			serializedObject.ApplyModifiedProperties();

			EditorUtility.SetDirty(spriteMeshAnimation);
			EditorUtility.SetDirty(spriteMeshAnimation.cachedSpriteMeshInstance);
		}
	}
}
