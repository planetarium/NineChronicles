using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Anima2D 
{
	public static class BoneUtils
	{	
		public static string GetUniqueBoneName(Bone2D root)
		{
			string boneName = "bone";
			
			Bone2D[] bones = null;
			
			if(root)
			{
				bones = root.GetComponentsInChildren<Bone2D>(true);
				boneName = boneName + " " + (bones.Length + 1).ToString();
			}
			
			return boneName;
		}

		public static void DrawBoneCap(Bone2D bone)
		{
			Color color = bone.color * 0.25f;
			color.a = 1f;
			DrawBoneCap(bone, color);
		}

		public static void DrawBoneCap(Bone2D bone, Color color)
		{
			Handles.matrix = bone.transform.localToWorldMatrix;
			DrawBoneCap(Vector3.zero,GetBoneRadius(bone),color);
		}
		
		public static void DrawBoneCap(Vector3 position, float radius, Color color)
		{
			Handles.color = color;
			HandlesExtra.DrawCircle(position, radius*0.65f);
		}

		public static void DrawBoneBody(Bone2D bone)
		{
			DrawBoneBody(bone,bone.color);
		}

		public static void DrawBoneBody(Bone2D bone, Color color)
		{
			Handles.matrix = bone.transform.localToWorldMatrix;
			DrawBoneBody(Vector3.zero, bone.localEndPosition, GetBoneRadius(bone),color);
		}
		
		public static void DrawBoneBody(Vector3 position, Vector3 endPosition, float radius, Color color)
		{
			Vector3 distance = position - endPosition;
			
			if(distance.magnitude > radius && color.a > 0f)
			{
				HandlesExtra.DrawLine(position,endPosition,Vector3.back,2f*radius,0f,color);
				HandlesExtra.DrawSolidArc(position,Vector3.back,Vector3.Cross(endPosition-position,Vector3.forward),180f,radius,color);
			}
		}

		public static void DrawBoneOutline(Bone2D bone, float outlineSize, Color color)
		{
			Handles.matrix = bone.transform.localToWorldMatrix;
			DrawBoneOutline(Vector3.zero,
			                bone.localEndPosition,
			                GetBoneRadius(bone),
			                outlineSize / Handles.matrix.GetScale().x,
			                color);
		}

		public static void DrawBoneOutline(Vector3 position, Vector3 endPoint, float radius, float outlineSize, Color color)
		{
			Handles.color = color;
			HandlesExtra.DrawLine(position,endPoint,Vector3.back, 2f * (radius + outlineSize), 2f * outlineSize);
			HandlesExtra.DrawSolidArc(position,Vector3.forward,Vector3.Cross(endPoint-position,Vector3.back),180f,radius + outlineSize,color);

			if(outlineSize > 0f)
			{
				HandlesExtra.DrawSolidArc(endPoint,Vector3.back,Vector3.Cross(endPoint-position,Vector3.back),180f,outlineSize,color);
			}
		}

		public static float GetBoneRadius(Bone2D bone)
		{
			return Mathf.Min(bone.localLength / 20f, 0.125f * HandleUtility.GetHandleSize(bone.transform.position));
		}

		public static string GetBonePath(Bone2D bone)
		{
			return GetBonePath(bone.root.transform,bone);
		}

		public static string GetBonePath(Transform root, Bone2D bone)
		{
			return GetPath(root, bone.transform);
		}

		public static string GetPath(Transform root, Transform transform)
		{
			string path = "";

			Transform current = transform;
			
			if(root)
			{
				while(current && current != root)
				{
					path = current.name + path;

					current = current.parent;

					if(current != root)
					{
						path = "/" + path;
					}
				}

				if(!current)
				{
					path = "";
				}
			}

			return path;
		}

		public static Bone2D ReconstructHierarchy(List<Bone2D> bones, List<string> paths)
		{
			Bone2D rootBone = null;

			for (int i = 0; i < bones.Count; i++)
			{
				Bone2D bone = bones[i];
				string path = paths[i];

				for (int j = 0; j < bones.Count; j++)
				{
					Bone2D other = bones [j];
					string otherPath = paths[j];

					if(bone != other && !path.Equals(otherPath) && otherPath.Contains(path))
					{
						other.transform.parent = bone.transform;
						other.transform.localScale = Vector3.one;
					}
				}
			}

			for (int i = 0; i < bones.Count; i++)
			{
				Bone2D bone = bones[i];

				if(bone.parentBone)
				{
					if((bone.transform.position - bone.parentBone.endPosition).sqrMagnitude < 0.00001f)
					{
						bone.parentBone.child = bone;
					}
				}else{
					rootBone = bone;
				}
			}

			return rootBone;
		}

		public static void OrientToChild(Bone2D bone, bool freezeChildren, string undoName, bool recordObject)
		{
			if(!bone || !bone.child) return;

			Vector3 l_childPosition = Vector3.zero;

			/*
			if(recordObject)
			{
				Undo.RecordObject(bone.child.transform,undoName);
			}else{
				Undo.RegisterCompleteObjectUndo(bone.child.transform,undoName);
			}
			*/

			l_childPosition = bone.child.transform.position;

			Quaternion l_deltaRotation = OrientToLocalPosition(bone,bone.child.transform.localPosition,freezeChildren,undoName,recordObject);

			bone.child.transform.position = l_childPosition;
			bone.child.transform.localRotation *= Quaternion.Inverse(l_deltaRotation);
			
			EditorUtility.SetDirty(bone.child.transform);
		}

		public static Quaternion OrientToLocalPosition(Bone2D bone, Vector3 localPosition, bool freezeChildren, string undoName, bool recordObject)
		{
			Quaternion l_deltaRotation = Quaternion.identity;

			if(bone && localPosition.sqrMagnitude > 0f)
			{
				List<Vector3> l_childPositions = new List<Vector3>(bone.transform.childCount);
				List<Transform> l_children = new List<Transform>(bone.transform.childCount);

				if(freezeChildren)
				{
					Transform l_mainChild = null;

					if(bone.child)
					{
						l_mainChild = bone.child.transform;
					}

					foreach(Transform child in bone.transform)
					{
						if(child != l_mainChild)
						{
							if(recordObject)
							{
								Undo.RecordObject(child,undoName);
							}else{
								Undo.RegisterCompleteObjectUndo(child,undoName);
							}
							
							l_children.Add(child);
							l_childPositions.Add(child.position);
						}
					}	
				}

				Quaternion l_deltaInverseRotation = Quaternion.identity;
				
				float angle = Mathf.Atan2(localPosition.y,localPosition.x) * Mathf.Rad2Deg;
				
				if(recordObject)
				{
					Undo.RecordObject(bone.transform,undoName);
				}else{
					Undo.RegisterCompleteObjectUndo(bone.transform,undoName);
				}
				
				l_deltaRotation = Quaternion.AngleAxis(angle, Vector3.forward);
				l_deltaInverseRotation = Quaternion.Inverse(l_deltaRotation);
				
				bone.transform.localRotation *= l_deltaRotation;
				FixLocalEulerHint(bone.transform);

				for (int i = 0; i < l_children.Count; i++)
				{
					Transform l_child = l_children[i];
					
					l_child.position = l_childPositions[i];
					l_child.localRotation *= l_deltaInverseRotation;
					FixLocalEulerHint(l_child);

					EditorUtility.SetDirty (l_child);
				}
				
				EditorUtility.SetDirty(bone.transform);
			}

			return l_deltaRotation;
		}

		static Type s_TransformType = typeof(Transform);
		static bool s_Initialized = false;
		static MethodInfo s_SetLocalEulerHintMethod = null;
		static MethodInfo s_GetLocalEulerAnglesMethod = null;
		static PropertyInfo s_GetRotationOrderProperty = null;
		
		static void InitializeReflection()
		{
			if(!s_Initialized)
			{
				if(s_SetLocalEulerHintMethod == null)
				{
					s_SetLocalEulerHintMethod = s_TransformType.GetMethod("SetLocalEulerHint",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
				
				if(s_GetLocalEulerAnglesMethod == null)
				{
					s_GetLocalEulerAnglesMethod = s_TransformType.GetMethod("GetLocalEulerAngles",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
				
				if(s_GetRotationOrderProperty == null)
				{
					s_GetRotationOrderProperty = s_TransformType.GetProperty("rotationOrder",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
				s_Initialized = true;
			}
		}
		
		public static void FixLocalEulerHint(Transform transform)
		{
			InitializeReflection();
			
			if(transform && s_SetLocalEulerHintMethod != null)
			{
				object[] parameters = { GetLocalEulerAngles(transform) };
				s_SetLocalEulerHintMethod.Invoke(transform,parameters);
			}
		}
		
		public static Vector3 GetLocalEulerAngles(Transform transform)
		{
			InitializeReflection();
			
			object rotationOrder = GetRotationOrder(transform);
			
			if(transform && s_GetLocalEulerAnglesMethod != null && rotationOrder != null)
			{
				object[] parameters = { rotationOrder };
				return (Vector3) s_GetLocalEulerAnglesMethod.Invoke(transform,parameters);
			}
			
			return Vector3.zero;
		}
		
		static object GetRotationOrder(Transform transform)
		{
			InitializeReflection();
			
			if(transform && s_GetRotationOrderProperty != null)
			{
				return s_GetRotationOrderProperty.GetValue(transform, null);
			}
			
			return null;
		}
	}
}
