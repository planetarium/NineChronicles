using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class Exporter
	{
		[MenuItem("Window/Anima2D/Export Prefab",true)]
		static bool ExportValidate()
		{
			SpriteMeshInstance[] spriteMeshInstances = null;

			if(Selection.activeGameObject)
			{
				spriteMeshInstances = Selection.activeGameObject.GetComponentsInChildren<SpriteMeshInstance>(true);
			}

			return !Application.isPlaying && spriteMeshInstances != null && spriteMeshInstances.Length > 0;
		}
		
		[MenuItem("Window/Anima2D/Export Prefab",false,40)]
		static void Export()
		{
			string path = EditorUtility.SaveFilePanelInProject("Export",Selection.activeGameObject.name + ".prefab","prefab","Export to prefab");

			if(path.Length <= 0)
			{
				return;
			}

			GameObject instance = GameObject.Instantiate(Selection.activeGameObject) as GameObject;

#if UNITY_2018_3_OR_NEWER
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
#else
			GameObject prefab = PrefabUtility.CreatePrefab(path,instance);
#endif

			GameObject.DestroyImmediate(instance);

			List<SpriteMeshInstance> spriteMeshInstances = new List<SpriteMeshInstance>();

			prefab.GetComponentsInChildren<SpriteMeshInstance>(true,spriteMeshInstances);

			foreach(SpriteMeshInstance spriteMeshInstance in spriteMeshInstances)
			{
				if(spriteMeshInstance.spriteMesh &&
				   spriteMeshInstance.spriteMesh.sprite)
				{
					if(spriteMeshInstance.spriteMesh.sharedMesh)
					{
						Mesh mesh = GameObject.Instantiate(spriteMeshInstance.spriteMesh.sharedMesh) as Mesh;

						mesh.name = spriteMeshInstance.spriteMesh.sharedMesh.name;

						AssetDatabase.AddObjectToAsset(mesh,prefab);

						if(spriteMeshInstance.cachedMeshFilter)
						{
							spriteMeshInstance.cachedMeshFilter.sharedMesh = mesh;
						}else if(spriteMeshInstance.cachedSkinnedRenderer)
						{
							spriteMeshInstance.cachedSkinnedRenderer.sharedMesh = mesh;
						}

					}

					if(spriteMeshInstance.sharedMaterial)
					{
						Material material = GameObject.Instantiate(spriteMeshInstance.sharedMaterial) as Material;

						material.name = spriteMeshInstance.name;
						material.mainTexture = spriteMeshInstance.spriteMesh.sprite.texture;
						material.color = spriteMeshInstance.color;

						AssetDatabase.AddObjectToAsset(material,prefab);

						if(spriteMeshInstance.cachedRenderer)
						{
							spriteMeshInstance.cachedRenderer.sharedMaterial = material;
						}
					}
				}
			}

			DestroyComponents<SpriteMeshInstance>(prefab);
			DestroyComponents<SpriteMeshAnimation>(prefab);
			DestroyComponents<Ik2D>(prefab);
			DestroyComponents<IkGroup>(prefab);
			DestroyComponents<Control>(prefab);
			DestroyComponents<Bone2D>(prefab);
			DestroyComponents<PoseManager>(prefab);

			EditorUtility.SetDirty(prefab);

			AssetDatabase.SaveAssets();
		}

		static void DestroyComponents<T>(GameObject gameObject) where T : MonoBehaviour
		{
			List<T> components = new List<T>();

			gameObject.GetComponentsInChildren<T>(true,components);

			foreach(T component in components)
			{
				GameObject.DestroyImmediate(component,true);
			}
		}
	}
}
