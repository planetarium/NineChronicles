using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[InitializeOnLoad]
	public class EditorEventHandler
	{
		static List<SpriteMeshInstance> s_SpriteMeshInstances = new List<SpriteMeshInstance>();

		static EditorEventHandler()
		{
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemCallback;
			EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
		}

		static SpriteMesh spriteMesh = null;
		static SpriteMeshInstance instance = null;
		static SpriteMeshInstance currentDestination = null;
		static List<Bone2D> s_InstanceBones = new List<Bone2D>();
		static bool init = false;
		static Vector3 instancePosition = Vector3.zero;
		static Transform parentTransform = null;

		static SpriteMesh GetSpriteMesh()
		{
			SpriteMesh l_spriteMesh = null;

			if(DragAndDrop.objectReferences.Length > 0)
			{
				Object obj = DragAndDrop.objectReferences[0];

				l_spriteMesh = obj as SpriteMesh;
			}

			return l_spriteMesh;
		}

		static void Cleanup()
		{
			init = false;
			spriteMesh = null;
			instance = null;
			currentDestination = null;
			parentTransform = null;
			s_InstanceBones.Clear();
		}

		static Vector3 GetMouseWorldPosition()
		{
			Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			Plane rootPlane = new Plane(Vector3.forward,Vector3.zero);
			
			float distance = 0f;
			Vector3 mouseWorldPos = Vector3.zero;
			
			if(rootPlane.Raycast(mouseRay, out distance))
			{
				mouseWorldPos = mouseRay.GetPoint(distance);
			}

			return mouseWorldPos;
		}

		static void CreateInstance()
		{
			instance = SpriteMeshUtils.CreateSpriteMeshInstance(spriteMesh,false);

			if(instance)
			{
				s_InstanceBones = instance.bones;

				instance.transform.parent = parentTransform;
				
				if(parentTransform)
				{
					instance.transform.localPosition = Vector3.zero;
				}
			}
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		static void HierarchyWindowChanged()
		{
			s_SpriteMeshInstances = GameObject.FindObjectsOfType<SpriteMeshInstance>().ToList();
		}

		private static void HierarchyWindowItemCallback(int pID, Rect pRect)
		{
			instancePosition = Vector3.zero;
			GameObject parent = null;

			if(pRect.Contains(Event.current.mousePosition))
			{
				parent = EditorUtility.InstanceIDToObject(pID) as GameObject;

				if(parent)
				{
					parentTransform = parent.transform;
				}
			}

			HandleDragAndDrop(false,parentTransform);
		}

		static void OnSceneGUI(SceneView sceneview)
		{
			instancePosition = GetMouseWorldPosition();
			HandleDragAndDrop(true,null);
		}

		static SpriteMeshInstance GetClosestBindeableIntersectingSpriteMeshInstance()
		{
			float minDistance = float.MaxValue;
			SpriteMeshInstance closestSpriteMeshInstance = null;

			foreach(SpriteMeshInstance spriteMeshInstance in s_SpriteMeshInstances)
			{
				if(spriteMeshInstance && spriteMeshInstance != instance && spriteMeshInstance.spriteMesh && spriteMeshInstance.cachedRenderer)
				{
					Ray guiRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

					if(spriteMeshInstance.cachedRenderer.bounds.IntersectRay(guiRay))
					{
						if(Bindeable(instance,spriteMeshInstance))
						{
							Vector2 guiCenter = HandleUtility.WorldToGUIPoint(spriteMeshInstance.cachedRenderer.bounds.center);
							float distance = (Event.current.mousePosition - guiCenter).sqrMagnitude;
							if(distance < minDistance)
							{
								closestSpriteMeshInstance = spriteMeshInstance;
							}
						}
					}
				}
			}

			return closestSpriteMeshInstance;
		}

		static int FindBindInfo(BindInfo bindInfo, SpriteMeshInstance spriteMeshInstance)
		{
			if(spriteMeshInstance)
			{
				return FindBindInfo(bindInfo, SpriteMeshUtils.LoadSpriteMeshData(spriteMeshInstance.spriteMesh));

			}

			return -1;
		}

		static int FindBindInfo(BindInfo bindInfo, SpriteMeshData spriteMeshData)
		{
			if(bindInfo && spriteMeshData)
			{
				for(int i = 0; i < spriteMeshData.bindPoses.Length; ++i)
				{
					BindInfo l_bindInfo = spriteMeshData.bindPoses[i];
					
					if(bindInfo.name == l_bindInfo.name /*&& Mathf.Approximately(bindInfo.boneLength,l_bindInfo.boneLength)*/)
					{
						return i;
					}
				}
			}

			return -1;
		}

		static bool Bindeable(SpriteMeshInstance targetSpriteMeshInstance, SpriteMeshInstance destinationSpriteMeshInstance)
		{
			bool bindeable = false;

			if(targetSpriteMeshInstance &&
			   destinationSpriteMeshInstance &&
			   targetSpriteMeshInstance.spriteMesh &&
			   destinationSpriteMeshInstance.spriteMesh &&
			   targetSpriteMeshInstance.spriteMesh != destinationSpriteMeshInstance.spriteMesh &&
			   destinationSpriteMeshInstance.cachedSkinnedRenderer)
			{
				SpriteMeshData targetData = SpriteMeshUtils.LoadSpriteMeshData(targetSpriteMeshInstance.spriteMesh);
				SpriteMeshData destinationData = SpriteMeshUtils.LoadSpriteMeshData(destinationSpriteMeshInstance.spriteMesh);

				bindeable = true;

				if(destinationData.bindPoses.Length >= targetData.bindPoses.Length)
				{
					for(int i = 0; i < targetData.bindPoses.Length; ++i)
					{
						if(bindeable)
						{
							BindInfo bindInfo = targetData.bindPoses[i];

							if(FindBindInfo(bindInfo,destinationData) < 0)
							{
								bindeable = false;
							}
						}
					}
				}else{
					bindeable = false;
				}	
			}
			return bindeable;
		}

		static void HandleDragAndDrop(bool createOnEnter, Transform parent)
		{
			switch(Event.current.type)
			{
			case EventType.DragUpdated:

				if(!init)
				{
					spriteMesh = GetSpriteMesh();

					if(createOnEnter)
					{
						parentTransform = null;
						CreateInstance();
					}

					Event.current.Use();

					init = true;
				}

				if(instance)
				{
					instance.transform.position = instancePosition;

					SpriteMeshInstance l_currentDestination = GetClosestBindeableIntersectingSpriteMeshInstance();

					if(currentDestination != l_currentDestination)
					{
						currentDestination = l_currentDestination;

						if(currentDestination)
						{
							List<Bone2D> destinationBones = currentDestination.bones;
							List<Bone2D> newBones = new List<Bone2D>();

							SpriteMeshData data = SpriteMeshUtils.LoadSpriteMeshData(instance.spriteMesh);

							for(int i = 0; i < data.bindPoses.Length; ++i)
							{
								BindInfo bindInfo = data.bindPoses[i];
								int index = FindBindInfo(bindInfo,currentDestination);
								if(index >= 0 && index < destinationBones.Count)
								{
									newBones.Add(destinationBones[index]);
								}
							}

							instance.transform.parent = currentDestination.transform.parent;
							instance.bones = newBones;
							SpriteMeshUtils.UpdateRenderer(instance,false);

							foreach(Bone2D bone in s_InstanceBones)
							{
								bone.hideFlags = HideFlags.HideAndDontSave;
								bone.gameObject.SetActive(false);
							}

						}else{
							foreach(Bone2D bone in s_InstanceBones)
							{
								bone.hideFlags = HideFlags.None;
								bone.gameObject.SetActive(true);
							}

							instance.transform.parent = null;
							instance.bones = s_InstanceBones;
							SpriteMeshUtils.UpdateRenderer(instance,false);
						}

						SceneView.RepaintAll();
					}
				}

				break;
			
			case EventType.DragExited:

				if(instance)
				{
					GameObject.DestroyImmediate(instance.gameObject);
					Event.current.Use();
				}
				Cleanup();
				break;

			case EventType.DragPerform:

				if(!createOnEnter)
				{
					CreateInstance();
				}

				if(instance)
				{
					if(currentDestination)
					{
						foreach(Bone2D bone in s_InstanceBones)
						{
							if(bone)
							{
								GameObject.DestroyImmediate(bone.gameObject);
							}
						}
					}

					Undo.RegisterCreatedObjectUndo(instance.gameObject,"create SpriteMeshInstance");
				}

				Cleanup();
				break;
			}

			if(instance)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			}
		}
	}
}
