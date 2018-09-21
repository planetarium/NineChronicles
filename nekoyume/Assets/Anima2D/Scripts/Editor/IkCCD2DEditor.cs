using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D 
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(IkCCD2D))]
	public class IkCCD2DEditor : Ik2DEditor
	{
		override public void OnInspectorGUI()
		{
			IkCCD2D ikCCD2D = target as IkCCD2D;

			base.OnInspectorGUI();

			SerializedProperty numBonesProp = serializedObject.FindProperty("m_NumBones");
			SerializedProperty iterationsProp = serializedObject.FindProperty("iterations");
			SerializedProperty dampingProp = serializedObject.FindProperty("damping");

			Bone2D targetBone = ikCCD2D.target;

			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(!targetBone);

			EditorGUI.BeginChangeCheck();

			int chainLength = 0;

			if(targetBone)
			{
				chainLength = targetBone.chainLength;
			}

			EditorGUILayout.IntSlider(numBonesProp,0,chainLength);
			
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo(ikCCD2D,"Set num bones");

				IkUtils.InitializeIk2D(serializedObject);
				EditorUpdater.SetDirty("Set num bones");
			}

			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(iterationsProp);
			EditorGUILayout.PropertyField(dampingProp);

			if(EditorGUI.EndChangeCheck())
			{
				EditorUpdater.SetDirty(Undo.GetCurrentGroupName());
			}

			serializedObject.ApplyModifiedProperties();
		}
	}	
}
