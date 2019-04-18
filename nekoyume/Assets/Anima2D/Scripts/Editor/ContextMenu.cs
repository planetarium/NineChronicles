using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Anima2D
{
	public class ContextMenu
	{
		[MenuItem("Assets/Create/Anima2D/SpriteMesh", true)]
		static bool ValidateCreateSpriteMesh(MenuCommand menuCommand)
		{
			bool valid = false;

			Sprite sprite = Selection.activeObject as Sprite;

			if(sprite && !SpriteMeshPostprocessor.GetSpriteMeshFromSprite(sprite))
			{
				valid = true;
			}

			List<Texture2D> selectedTextures = Selection.objects.ToList().Where( o => o is Texture2D).ToList().ConvertAll( o => o as Texture2D);

			valid = valid || selectedTextures.Count > 0;

			return valid;
		}

		[MenuItem("Assets/Create/Anima2D/SpriteMesh", false)]
		static void CreateSpriteMesh(MenuCommand menuCommand)
		{
			List<Texture2D> selectedTextures = Selection.objects.ToList().Where( o => o is Texture2D).ToList().ConvertAll( o => o as Texture2D);

			foreach(Texture2D texture in selectedTextures)
			{
				SpriteMeshUtils.CreateSpriteMesh(texture);
			}

			if(selectedTextures.Count == 0)
			{
				SpriteMeshUtils.CreateSpriteMesh(Selection.activeObject as Sprite);
			}
		}
		
		[MenuItem("GameObject/2D Object/SpriteMesh", false, 10)]
		static void ContextCreateSpriteMesh(MenuCommand menuCommand)
		{
			GameObject spriteRendererGO = Selection.activeGameObject;
			SpriteRenderer spriteRenderer = null;
			SpriteMesh spriteMesh = null;

			int sortingLayerID = 0;
			int sortingOrder = 0;

			if(spriteRendererGO)
			{
				spriteRenderer = spriteRendererGO.GetComponent<SpriteRenderer>();
			}
			
			if(spriteRenderer &&
			   spriteRenderer.sprite)
			{
				sortingLayerID = spriteRenderer.sortingLayerID;
				sortingOrder = spriteRenderer.sortingOrder;

				SpriteMesh overrideSpriteMesh =  SpriteMeshPostprocessor.GetSpriteMeshFromSprite(spriteRenderer.sprite);

				if(overrideSpriteMesh)
				{
					spriteMesh = overrideSpriteMesh;
				}else{
					spriteMesh = SpriteMeshUtils.CreateSpriteMesh(spriteRenderer.sprite);
				}
			}
			
			if(spriteMesh)
			{
				Undo.SetCurrentGroupName("create SpriteMeshInstance"); 
				Undo.DestroyObjectImmediate(spriteRenderer);
				SpriteMeshInstance spriteMeshInstance = SpriteMeshUtils.CreateSpriteMeshInstance(spriteMesh,spriteRendererGO,true);

				spriteMeshInstance.sortingLayerID = sortingLayerID;
				spriteMeshInstance.sortingOrder = sortingOrder;
				
				Selection.activeGameObject = spriteRendererGO;
			}else{
				Debug.Log("Select a SpriteRenderer with a Sprite to convert to SpriteMesh");
			}
		}

		[MenuItem("GameObject/2D Object/Bone &#b", false, 10)]
		public static void CreateBone(MenuCommand menuCommand)
		{
			GameObject bone = new GameObject("New bone");
			Bone2D boneComponent = bone.AddComponent<Bone2D>();

			Undo.RegisterCreatedObjectUndo(bone, "Create bone");

			bone.transform.position = GetDefaultInstantiatePosition();

			GameObject selectedGO = Selection.activeGameObject;
			if(selectedGO)
			{
				bone.transform.parent = selectedGO.transform;

				Vector3 localPosition = bone.transform.localPosition;
				localPosition.z = 0f;

				bone.transform.localPosition = localPosition;
				bone.transform.localRotation = Quaternion.identity;
				bone.transform.localScale = Vector3.one;
				
				Bone2D selectedBone = selectedGO.GetComponent<Bone2D>();

				if(selectedBone)
				{
					if(!selectedBone.child)
					{
						bone.transform.position = selectedBone.endPosition;
						selectedBone.child = boneComponent;
					}
				}
			}

			Selection.activeGameObject = bone;
		}

		[MenuItem("GameObject/2D Object/IK CCD &#k", false, 10)]
		static void CreateIkCCD(MenuCommand menuCommand)
		{
			GameObject ikCCD = new GameObject("New Ik CCD");
			Undo.RegisterCreatedObjectUndo(ikCCD,"Crate Ik CCD");

			IkCCD2D ikCCDComponent = ikCCD.AddComponent<IkCCD2D>();
			ikCCD.transform.position = GetDefaultInstantiatePosition();
			
			GameObject selectedGO = Selection.activeGameObject;
			if(selectedGO)
			{
				ikCCD.transform.parent = selectedGO.transform;
				ikCCD.transform.localPosition = Vector3.zero;

				Bone2D selectedBone = selectedGO.GetComponent<Bone2D>();
				
				if(selectedBone)
				{
					ikCCD.transform.parent = selectedBone.root.transform.parent;
					ikCCD.transform.position = selectedBone.endPosition;

					if(selectedBone.child)
					{
						ikCCD.transform.rotation = selectedBone.child.transform.rotation;
					}

					ikCCDComponent.numBones = selectedBone.chainLength;
					ikCCDComponent.target = selectedBone;
				}
			}

			ikCCD.transform.localScale = Vector3.one;

			EditorUtility.SetDirty(ikCCDComponent);

			Selection.activeGameObject = ikCCD;
		}

		[MenuItem("GameObject/2D Object/IK Limb &#l", false, 10)]
		static void CreateIkLimb(MenuCommand menuCommand)
		{
			GameObject ikLimb = new GameObject("New Ik Limb");
			Undo.RegisterCreatedObjectUndo(ikLimb,"Crate Ik Limb");
			
			IkLimb2D ikLimbComponent = ikLimb.AddComponent<IkLimb2D>();
			ikLimb.transform.position = GetDefaultInstantiatePosition();
			
			GameObject selectedGO = Selection.activeGameObject;
			if(selectedGO)
			{
				ikLimb.transform.parent = selectedGO.transform;
				ikLimb.transform.localPosition = Vector3.zero;
				
				Bone2D selectedBone = selectedGO.GetComponent<Bone2D>();
				
				if(selectedBone)
				{
					ikLimb.transform.parent = selectedBone.root.transform.parent;
					ikLimb.transform.position = selectedBone.endPosition;

					if(selectedBone.child)
					{
						ikLimb.transform.rotation = selectedBone.child.transform.rotation;
					}

					ikLimbComponent.numBones = selectedBone.chainLength;
					ikLimbComponent.target = selectedBone;
				}
			}

			ikLimb.transform.localScale = Vector3.one;

			EditorUtility.SetDirty(ikLimbComponent);

			Selection.activeGameObject = ikLimb;
		}

		[MenuItem("GameObject/2D Object/Control &#c", false, 10)]
		static void CreateControl(MenuCommand menuCommand)
		{
			GameObject control = new GameObject("New control");
			Undo.RegisterCreatedObjectUndo(control,"Crate Control");
			
			Control controlComponent = control.AddComponent<Control>();
			control.transform.position = GetDefaultInstantiatePosition();
			
			GameObject selectedGO = Selection.activeGameObject;
			if(selectedGO)
			{
				control.transform.parent = selectedGO.transform;
				control.transform.localPosition = Vector3.zero;
				control.transform.localRotation = Quaternion.identity;
				
				Bone2D selectedBone = selectedGO.GetComponent<Bone2D>();
				
				if(selectedBone)
				{
					control.name = "Control " + selectedBone.name;
					controlComponent.bone = selectedBone;
					control.transform.parent = selectedBone.root.transform.parent;
				}
			}

			EditorUtility.SetDirty(controlComponent);
			
			Selection.activeGameObject = control;
		}

		[MenuItem("Assets/Create/Anima2D/Pose")]
		public static void CreatePose(MenuCommand menuCommand)
		{
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);

			if(System.IO.File.Exists(path))
			{
				path = System.IO.Path.GetDirectoryName(path);
			}
			
			path += "/";
			
			if(System.IO.Directory.Exists(path))
			{
				path += "New pose.asset";

				ScriptableObjectUtility.CreateAsset<Pose>(path);
			}
		}

		static Vector3 GetDefaultInstantiatePosition()
		{
			Vector3 result = Vector3.zero;
			if (SceneView.lastActiveSceneView)
			{
				if (SceneView.lastActiveSceneView.in2DMode)
				{
					result = SceneView.lastActiveSceneView.camera.transform.position;
					result.z = 0f;
				}
				else
				{
					PropertyInfo prop = typeof(SceneView).GetProperty("cameraTargetPosition",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

					result = (Vector3) prop.GetValue(SceneView.lastActiveSceneView,null);
				}
			}
			return result;
		}
	}
}
