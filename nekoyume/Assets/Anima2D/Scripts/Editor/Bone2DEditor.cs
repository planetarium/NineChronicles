using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Bone2D))]
	public class Bone2DEditor : Editor
	{
		SerializedProperty m_ColorProperty;
		SerializedProperty m_AlphaProperty;
		SerializedProperty m_ChildProperty;
		SerializedProperty m_ChildTransformProperty;
		SerializedProperty m_LengthProperty;
		Bone2D m_Bone;
		
		void OnEnable()
		{
			Tools.hidden = Tools.current == Tool.Move;
			
			m_Bone = target as Bone2D;
			
			m_ColorProperty = serializedObject.FindProperty("m_Color");
			m_AlphaProperty = m_ColorProperty.FindPropertyRelative("a");
			
			//DEPRECATED
			m_ChildProperty = serializedObject.FindProperty("m_Child");
			
			m_ChildTransformProperty = serializedObject.FindProperty("m_ChildTransform");
			m_LengthProperty = serializedObject.FindProperty("m_Length");
			
			UpgradeToChildTransform();
		}
		
		void UpgradeToChildTransform()
		{
			if(Selection.transforms.Length == 1 && !m_ChildTransformProperty.objectReferenceValue && m_ChildProperty.objectReferenceValue)
			{
				serializedObject.Update();
				
				Bone2D l_bone = m_ChildProperty.objectReferenceValue as Bone2D;
				
				if(l_bone)
				{
					m_ChildTransformProperty.objectReferenceValue = l_bone.transform;
				}
				
				m_ChildProperty.objectReferenceValue = null;
				
				serializedObject.ApplyModifiedProperties();
			}
		}
		
		void OnDisable()
		{
			Tools.hidden = false;
		}
		
		override public void OnInspectorGUI()
		{
			bool childChanged = false;
			
			serializedObject.Update();
			
			EditorGUILayout.PropertyField(m_ColorProperty);
			EditorGUILayout.Slider(m_AlphaProperty,0f,1f,new GUIContent("Alpha"));
			
			Transform childTransform = null;
			
			if(m_Bone.child)
			{
				childTransform = m_Bone.child.transform;
			}
			
			EditorGUI.BeginDisabledGroup(targets.Length > 1);
			
			EditorGUI.showMixedValue = targets.Length > 1;
			
			EditorGUI.BeginChangeCheck();
			
			Transform newChildTransform = EditorGUILayout.ObjectField(new GUIContent("Child"),childTransform,typeof(Transform),true) as Transform;
			
			if(EditorGUI.EndChangeCheck())
			{
				if(newChildTransform && (newChildTransform.parent != m_Bone.transform || !newChildTransform.GetComponent<Bone2D>()))
				{
					newChildTransform = null;
				}
				
				m_ChildTransformProperty.objectReferenceValue = newChildTransform;
				
				childChanged = true;
			}
			
			EditorGUI.EndDisabledGroup();
			
			EditorGUILayout.PropertyField(m_LengthProperty);
			
			serializedObject.ApplyModifiedProperties();
			
			if(childChanged)
			{
				BoneUtils.OrientToChild(m_Bone,true,"set child",false);
			}
		}
		
		void OnSceneGUI()
		{
			if(Tools.current == Tool.Move)
			{
				Tools.hidden = true;
				
				float size = HandleUtility.GetHandleSize(m_Bone.transform.position) / 5f;
				
				Quaternion rotation = m_Bone.transform.rotation;
				
				EditorGUI.BeginChangeCheck();
				
				Quaternion cameraRotation = Camera.current.transform.rotation;
				
				if(Event.current.type == EventType.Repaint)
				{
					Camera.current.transform.rotation = m_Bone.transform.rotation;
				}
					
#if UNITY_5_6_OR_NEWER
				Vector3 newPosition = Handles.FreeMoveHandle(m_Bone.transform.position, rotation, size, Vector3.zero, Handles.RectangleHandleCap);
#else
				Vector3 newPosition = Handles.FreeMoveHandle(m_Bone.transform.position, rotation, size, Vector3.zero, Handles.RectangleCap);
#endif
				
				if(Event.current.type == EventType.Repaint)
				{
					Camera.current.transform.rotation = cameraRotation;
				}
				
				if(EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(m_Bone.transform,"Move");
					
					m_Bone.transform.position = newPosition;
					
					BoneUtils.OrientToChild(m_Bone.parentBone,Event.current.shift,Undo.GetCurrentGroupName(),true);
					
					EditorUtility.SetDirty(m_Bone.transform);
					
					EditorUpdater.SetDirty("Move");
				}
			}else{
				Tools.hidden = false;
			}
		}
	}
}
