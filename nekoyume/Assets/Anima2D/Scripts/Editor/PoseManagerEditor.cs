using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[CustomEditor(typeof(PoseManager))]
	public class PoseManagerEditor : Editor
	{
		ReorderableList mList = null;

		List<string> m_DuplicatedPaths;

		void OnEnable()
		{
			m_DuplicatedPaths = GetDuplicatedPaths((target as PoseManager).transform);

			SetupList();
		}
		
		void SetupList()
		{
			SerializedProperty poseListProperty = serializedObject.FindProperty("m_Poses");
			
			if(poseListProperty != null)
			{
				mList = new ReorderableList(serializedObject,poseListProperty,true,true,true,true);
				
				mList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					
					SerializedProperty poseProperty = mList.serializedProperty.GetArrayElementAtIndex(index);
					
					rect.y += 1.5f;

					EditorGUI.PropertyField( new Rect(rect.x, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight), poseProperty, GUIContent.none);

					EditorGUI.BeginDisabledGroup(!poseProperty.objectReferenceValue);

					if(GUI.Button(new Rect(rect.x + rect.width - 115, rect.y, 55, EditorGUIUtility.singleLineHeight),"Save"))
					{
						if (EditorUtility.DisplayDialog("Overwrite Pose", "Overwrite '" + poseProperty.objectReferenceValue.name + "'?", "Apply", "Cancel"))
						{
							PoseUtils.SavePose(poseProperty.objectReferenceValue as Pose,(target as PoseManager).transform);
							mList.index = index;
						}
					}

					if(GUI.Button(new Rect(rect.x + rect.width - 55, rect.y, 55, EditorGUIUtility.singleLineHeight),"Load"))
					{
						PoseUtils.LoadPose(poseProperty.objectReferenceValue as Pose,(target as PoseManager).transform);
						mList.index = index;
					}

					EditorGUI.EndDisabledGroup();
				};
				
				mList.drawHeaderCallback = (Rect rect) => {  
					EditorGUI.LabelField(rect, "Poses");
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

			EditorGUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Create new pose",GUILayout.Width(150)))
			{
				EditorApplication.delayCall += CreateNewPose;
			}

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			if(m_DuplicatedPaths.Count > 0)
			{
				string helpString = "Warning: duplicated bone paths found.\nPlease use unique bone paths:\n\n";

				foreach(string path in m_DuplicatedPaths)
				{
					helpString += path + "\n";
				}

				EditorGUILayout.HelpBox(helpString, MessageType.Warning, true);
			}

			serializedObject.ApplyModifiedProperties();
		}

		void CreateNewPose()
		{
			serializedObject.Update();

			Pose newPose = ScriptableObjectUtility.CreateAssetWithSavePanel<Pose>("Create a pose asset","pose.asset","asset","Create a new pose");
			
			mList.serializedProperty.arraySize += 1;
			
			SerializedProperty newElement = mList.serializedProperty.GetArrayElementAtIndex(mList.serializedProperty.arraySize-1);
			
			newElement.objectReferenceValue = newPose;

			serializedObject.ApplyModifiedProperties();

			PoseUtils.SavePose(newPose,(target as PoseManager).transform);
		}

		List<string> GetDuplicatedPaths(Transform root)
		{
			List<string> paths = new List<string>(50);
			List<string> duplicates = new List<string>(50);
			List<Bone2D> bones = new List<Bone2D>(50);

			root.GetComponentsInChildren<Bone2D>(true,bones);

			for (int i = 0; i < bones.Count; i++)
			{
				Bone2D bone = bones [i];
				
				if(bone)
				{
					string bonePath = BoneUtils.GetBonePath(root,bone);

					if(paths.Contains(bonePath))
					{
						duplicates.Add(bonePath);
					}else{
						paths.Add(bonePath);
					}	
				}
			}

			return duplicates;
		}
	}
}
