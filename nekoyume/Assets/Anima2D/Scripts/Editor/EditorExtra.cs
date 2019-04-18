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
			/*
			GameObject result = null;
			MethodInfo methodInfo = typeof(EditorUtility).GetMethod("InstantiateForAnimatorPreview", BindingFlags.Static | BindingFlags.NonPublic);
			if(methodInfo != null)
			{
				object[] parameters = new object[] { original };
				result = (GameObject) methodInfo.Invoke(null,parameters);
			}
			*/

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

						spriteMeshInstance.cachedRenderer.sharedMaterial = materialClone;
					}
				}

				if(!(behaviour is Ik2D)
#if UNITY_5_6_OR_NEWER
					&& !(behaviour is SortingGroup)
#endif
				)
				{
					behaviour.enabled = false;
				}
			}

			return result;
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
	}
}
