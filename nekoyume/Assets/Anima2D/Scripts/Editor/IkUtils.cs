using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anima2D 
{
	public static class IkUtils
	{
		public static void InitializeIk2D(SerializedObject ikSO)
		{
			SerializedProperty targetTransformProp = ikSO.FindProperty("m_TargetTransform");
			SerializedProperty numBonesProp = ikSO.FindProperty("m_NumBones");
			SerializedProperty solverProp = ikSO.FindProperty("m_Solver");
			SerializedProperty solverPosesProp = solverProp.FindPropertyRelative("m_SolverPoses");
			SerializedProperty rootBoneTransformProp = solverProp.FindPropertyRelative("m_RootBoneTransform");

			Transform targetTransform = targetTransformProp.objectReferenceValue as Transform;
			Bone2D targetBone = null;

			if(targetTransform)
			{
				targetBone = targetTransform.GetComponent<Bone2D>();
			}

			Bone2D rootBone = null;
			Transform rootBoneTransform = null;

			if(targetBone)
			{
				rootBone = Bone2D.GetChainBoneByIndex(targetBone, numBonesProp.intValue-1);
			}

			if(rootBone)
			{
				rootBoneTransform = rootBone.transform;
			}

			for(int i = 0; i < solverPosesProp.arraySize; ++i)
			{
				SerializedProperty poseProp = solverPosesProp.GetArrayElementAtIndex(i);
				SerializedProperty poseBoneProp = poseProp.FindPropertyRelative("m_BoneTransform");

				Transform boneTransform = poseBoneProp.objectReferenceValue as Transform;
				Bone2D bone = boneTransform.GetComponent<Bone2D>();

				if(bone)
				{
					bone.attachedIK = null;
				}
			}

			rootBoneTransformProp.objectReferenceValue = rootBoneTransform;
			solverPosesProp.arraySize = 0;

			if(rootBone)
			{
				solverPosesProp.arraySize = numBonesProp.intValue;

				Bone2D bone = rootBone;
				
				for(int i = 0; i < numBonesProp.intValue; ++i)
				{
					SerializedProperty poseProp = solverPosesProp.GetArrayElementAtIndex(i);
					SerializedProperty poseBoneTransformProp = poseProp.FindPropertyRelative("m_BoneTransform");
					SerializedProperty localRotationProp = poseProp.FindPropertyRelative("defaultLocalRotation");
					SerializedProperty solverPositionProp = poseProp.FindPropertyRelative("solverPosition");
					SerializedProperty solverRotationProp = poseProp.FindPropertyRelative("solverRotation");

					if(bone)
					{
						poseBoneTransformProp.objectReferenceValue = bone.transform;
						localRotationProp.quaternionValue = bone.transform.localRotation;
						solverPositionProp.vector3Value = Vector3.zero;
						solverRotationProp.quaternionValue = Quaternion.identity;

						bone = bone.child;
					}
				}
			}
		}

		public static List<Ik2D> BuildIkList(Ik2D ik2D)
		{
			if(ik2D.target)
			{
				return BuildIkList(ik2D.target);
			}

			return new List<Ik2D>();
		}

		static List<Ik2D> BuildIkList(Bone2D bone)
		{
			return BuildIkList(bone.chainRoot.gameObject);
		}

		static List<Ik2D> BuildIkList(GameObject gameObject)
		{
			return gameObject.GetComponentsInChildren<Bone2D>().Select( b => b.attachedIK ).Distinct().ToList();
		}

		public static void UpdateAttachedIKs(List<Ik2D> Ik2Ds)
		{
			for (int i = 0; i < Ik2Ds.Count; i++)
			{
				Ik2D ik2D = Ik2Ds[i];
				
				if(ik2D)
				{
					for (int j = 0; j < ik2D.solver.solverPoses.Count; j++)
					{
						IkSolver2D.SolverPose pose = ik2D.solver.solverPoses[j];
						
						if(pose.bone)
						{
							pose.bone.attachedIK = ik2D;
						}
					}
				}
			}
		}

		public static List<Ik2D> UpdateIK(GameObject gameObject, string undoName, bool recordObject)
		{
			return UpdateIK(gameObject,undoName,recordObject,false);
		}

		public static List<Ik2D> UpdateIK(GameObject gameObject, string undoName, bool recordObject, bool updateAttachedIK)
		{
			if(updateAttachedIK)
			{
				List<Ik2D> ik2Ds = new List<Ik2D>();
				gameObject.GetComponentsInChildren<Ik2D>(ik2Ds);
				
				UpdateAttachedIKs(ik2Ds);
			}

			List<Ik2D> list = BuildIkList(gameObject);

			UpdateIkList(list,undoName,recordObject);

			return list;
		}

		public static List<Ik2D> UpdateIK(Ik2D ik2D, string undoName, bool recordObject)
		{
			if(ik2D && ik2D.target)
			{
				return UpdateIK(ik2D.target.chainRoot,undoName,recordObject);
			}
			return null;
		}

		public static List<Ik2D> UpdateIK(Bone2D bone, string undoName, bool recordObject)
		{
			List<Ik2D> list = BuildIkList(bone.chainRoot.gameObject);

			UpdateIkList(list,undoName,recordObject);

			return list;
		}

		static void UpdateIkList(List<Ik2D> ikList, string undoName, bool recordObject)
		{
			for (int i = 0; i < ikList.Count; i++)
			{
				Ik2D l_ik2D = ikList[i];
				
				if(l_ik2D && l_ik2D.isActiveAndEnabled)
				{
					if(!string.IsNullOrEmpty(undoName))
					{
						for (int j = 0; j < l_ik2D.solver.solverPoses.Count; j++)
						{
							IkSolver2D.SolverPose pose = l_ik2D.solver.solverPoses [j];
							if(pose.bone)
							{
								if(recordObject)
								{
									Undo.RecordObject(pose.bone.transform, undoName);
								}else{
									Undo.RegisterCompleteObjectUndo(pose.bone.transform, undoName);
								}
							}
						}
					}

					if(!string.IsNullOrEmpty(undoName) && 
					   l_ik2D.orientChild &&
					   l_ik2D.target &&
					   l_ik2D.target.child)
					{
						if(recordObject)
						{
							Undo.RecordObject(l_ik2D.target.child.transform, undoName);
						}else{
							Undo.RegisterCompleteObjectUndo(l_ik2D.target.child.transform, undoName);
						}
					}

					l_ik2D.UpdateIK();

					for (int j = 0; j < l_ik2D.solver.solverPoses.Count; j++)
					{
						IkSolver2D.SolverPose pose = l_ik2D.solver.solverPoses [j];
						if(pose.bone)
						{
							BoneUtils.FixLocalEulerHint(pose.bone.transform);
						}

						if(l_ik2D.orientChild && l_ik2D.target.child)
						{
							BoneUtils.FixLocalEulerHint(l_ik2D.target.child.transform);
						}
					}
				}
			}
		}
	}
}
