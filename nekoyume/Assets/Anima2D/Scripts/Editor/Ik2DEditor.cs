using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D 
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Ik2D),true)]
	public class Ik2DEditor : Editor
	{
		SerializedProperty m_RecordProperty;
		SerializedProperty m_TargetTransformProperty;
		SerializedProperty m_WeightProperty;
		SerializedProperty m_RestorePoseProperty;
		SerializedProperty m_OrientChildProperty;

		Ik2D m_Ik2D;

		protected virtual void OnEnable()
		{
			m_Ik2D = target as Ik2D;

			m_RecordProperty = serializedObject.FindProperty("m_Record");
			m_TargetTransformProperty = serializedObject.FindProperty("m_TargetTransform");
			m_WeightProperty = serializedObject.FindProperty("m_Weight");
			m_RestorePoseProperty = serializedObject.FindProperty("m_RestoreDefaultPose");
			m_OrientChildProperty = serializedObject.FindProperty("m_OrientChild");
		}

		override public void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_RecordProperty);

			Transform targetTransform = null;
			
			if(m_Ik2D.target)
			{
				targetTransform = m_Ik2D.target.transform;
			}
			
			EditorGUI.BeginChangeCheck();
			
			Transform newTargetTransform = EditorGUILayout.ObjectField(new GUIContent("Target"),targetTransform,typeof(Transform),true) as Transform;
			
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo(m_Ik2D,"set target");

				if(newTargetTransform && !newTargetTransform.GetComponent<Bone2D>())
				{
					newTargetTransform = null;
				}

				if(newTargetTransform != targetTransform)
				{
					m_TargetTransformProperty.objectReferenceValue = newTargetTransform;
					IkUtils.InitializeIk2D(serializedObject);
					EditorUpdater.SetDirty("set target");
				}
			}

			/*
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_TargetTransformProperty);

			if(EditorGUI.EndChangeCheck())
			{
				IkUtils.InitializeIk2D(serializedObject);

				DoUpdateIK();
			}
			*/

			EditorGUI.BeginChangeCheck();
			
			EditorGUILayout.Slider(m_WeightProperty,0f,1f);
			EditorGUILayout.PropertyField(m_RestorePoseProperty);
			EditorGUILayout.PropertyField(m_OrientChildProperty);

			if(EditorGUI.EndChangeCheck())
			{
				EditorUpdater.SetDirty(Undo.GetCurrentGroupName());
			}

			serializedObject.ApplyModifiedProperties();
		}
	}	
}
