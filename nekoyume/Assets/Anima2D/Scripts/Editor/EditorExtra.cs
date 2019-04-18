using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_5_6_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace Anima2D
{
	public class EditorExtra
	{
		public static GameObject InstantiateForAnimatorPreview(UnityEngine.Object original)
		{
			GameObject result = GameObject.Instantiate(original) as GameObject;

			List<Behaviour> behaviours = new List<Behaviour>();
			result.GetComponentsInChildren<Behaviour>(false,behaviours);

			foreach(Behaviour behaviour in behaviours)
			{
				SpriteMeshInstance spriteMeshInstance = behaviour as SpriteMeshInstance;

				if(spriteMeshInstance && spriteMeshInstance.spriteMesh && spriteMeshInstance.spriteMesh.sprite)
				{
					Material material = spriteMeshInstance.sharedMaterial;

					if(material)
					{
						Material materialClone = GameObject.Instantiate(material);
						materialClone.hideFlags = HideFlags.HideAndDontSave;
						materialClone.mainTexture = spriteMeshInstance.spriteMesh.sprite.texture;

						spriteMeshInstance.sharedMaterial = materialClone;
						spriteMeshInstance.cachedRenderer.sharedMaterial = materialClone;
					}
				}

				if(behaviour == null ||
					behaviour is Ik2D ||
					behaviour is SpriteMeshAnimation
#if UNITY_5_6_OR_NEWER
					|| behaviour is SortingGroup
#endif
				)
					continue;
				else
					behaviour.enabled = false;
			}

			return result;
		}

		public static void DestroyAnimatorPreviewInstance(GameObject instance)
		{
			var spriteMeshInstances = new List<SpriteMeshInstance>();
			instance.GetComponentsInChildren<SpriteMeshInstance>(false, spriteMeshInstances);

			foreach(SpriteMeshInstance spriteMeshInstance in spriteMeshInstances)
			{
				if(spriteMeshInstance && spriteMeshInstance.spriteMesh && spriteMeshInstance.spriteMesh.sprite)
				{
					var materialClone = spriteMeshInstance.sharedMaterial;

					if(materialClone != null)
						UnityEngine.Object.DestroyImmediate(materialClone);
				}
			}

			GameObject.DestroyImmediate(instance);
		}
		
		public static void InitInstantiatedPreviewRecursive(GameObject go)
		{
			go.hideFlags = HideFlags.HideAndDontSave;

			foreach (Transform transform in go.transform)
			{
				InitInstantiatedPreviewRecursive(transform.gameObject);
			}
		}

		
		public static List<string> GetSortingLayerNames()
		{
			List<string> names = new List<string>();
			
			PropertyInfo sortingLayersProperty = typeof(InternalEditorUtility).GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
			if(sortingLayersProperty != null)
			{
				string[] sortingLayers = (string[])sortingLayersProperty.GetValue(null, new object[0]);
				names.AddRange(sortingLayers);
			}
			
			return names;
		}

		public static bool IsProSkin()
		{
			bool isProSkin = false;

			PropertyInfo prop = typeof(EditorGUIUtility).GetProperty("isProSkin", BindingFlags.Static | BindingFlags.NonPublic);

			if(prop != null)
			{
				isProSkin = (bool)prop.GetValue(null, new object[0]);
			}

			return isProSkin;
		}

		public static GameObject PickGameObject(Vector2 mousePosition)
		{
			MethodInfo methodInfo = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneViewPicking").GetMethod("PickGameObject", BindingFlags.Static | BindingFlags.Public);

			if(methodInfo != null)
			{
				return (GameObject)methodInfo.Invoke(null, new object[] { mousePosition });
			}

			return null;
		}

		public static T[] FindComponentsOfType<T>() where T : Component
		{
#if UNITY_2018_3_OR_NEWER
			var currentStage = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle();
            return  currentStage.FindComponentsOfType<T>().Where(x => x.gameObject.scene.isLoaded && x.gameObject.activeInHierarchy).ToArray();
#else
			return GameObject.FindObjectsOfType<T>();
#endif
		}
	}
}
