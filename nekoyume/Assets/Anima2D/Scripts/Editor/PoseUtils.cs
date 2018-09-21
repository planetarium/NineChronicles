using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public static class PoseUtils
	{
		public static void SavePose(Pose pose, Transform root)
		{
			List<Bone2D> bones = new List<Bone2D>(50);

			root.GetComponentsInChildren<Bone2D>(true,bones);

			SerializedObject poseSO = new SerializedObject(pose);
			SerializedProperty entriesProp = poseSO.FindProperty("m_PoseEntries");

			poseSO.Update();
			entriesProp.arraySize = bones.Count;

			for (int i = 0; i < bones.Count; i++)
			{
				Bone2D bone = bones [i];

				if(bone)
				{
					SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);
					element.FindPropertyRelative("path").stringValue = BoneUtils.GetBonePath(root,bone);
					element.FindPropertyRelative("localPosition").vector3Value = bone.transform.localPosition;
					element.FindPropertyRelative("localRotation").quaternionValue = bone.transform.localRotation;
					element.FindPropertyRelative("localScale").vector3Value = bone.transform.localScale;
				}
			}

			poseSO.ApplyModifiedProperties();
		}

		public static void LoadPose(Pose pose, Transform root)
		{
			SerializedObject poseSO = new SerializedObject(pose);
			SerializedProperty entriesProp = poseSO.FindProperty("m_PoseEntries");

			List<Ik2D> iks = new List<Ik2D>();

			for (int i = 0; i < entriesProp.arraySize; i++)
			{
				SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);

				Transform boneTransform = root.Find(element.FindPropertyRelative("path").stringValue);

				if(boneTransform)
				{
					Bone2D boneComponent = boneTransform.GetComponent<Bone2D>();

					if(boneComponent && boneComponent.attachedIK && !iks.Contains(boneComponent.attachedIK))
					{
						iks.Add(boneComponent.attachedIK);
					}

					Undo.RecordObject(boneTransform,"Load Pose");

					boneTransform.localPosition = element.FindPropertyRelative("localPosition").vector3Value;
					boneTransform.localRotation = element.FindPropertyRelative("localRotation").quaternionValue;
					boneTransform.localScale = element.FindPropertyRelative("localScale").vector3Value;
					BoneUtils.FixLocalEulerHint(boneTransform);
				}
			}

			for (int i = 0; i < iks.Count; i++)
			{
				Ik2D ik = iks[i];

				if(ik && ik.target)
				{
					Undo.RecordObject(ik.transform,"Load Pose");

					ik.transform.position = ik.target.endPosition;

					if(ik.orientChild && ik.target.child)
					{
						ik.transform.rotation = ik.target.child.transform.rotation;
						BoneUtils.FixLocalEulerHint(ik.transform);
					}
				}
			}

			EditorUpdater.SetDirty("Load Pose");
		}
	}
}
