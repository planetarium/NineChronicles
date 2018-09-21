using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Anima2D
{
	[CustomEditor(typeof(IkLimb2D))]
	public class IkLimb2DEditor : Ik2DEditor
	{
		override public void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();
			
			SerializedProperty flipProp = serializedObject.FindProperty("flip");

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(flipProp);

			if(EditorGUI.EndChangeCheck())
			{
				EditorUpdater.SetDirty("Flip");
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
