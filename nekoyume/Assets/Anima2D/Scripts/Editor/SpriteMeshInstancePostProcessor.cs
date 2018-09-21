using UnityEngine;
using UnityEditor;
using System.Collections;
using Anima2D;

public class SpriteMeshInstancePostProcessor : AssetPostprocessor
{
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		foreach(string importedAssetPath in importedAssets)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath(importedAssetPath,typeof(GameObject)) as GameObject;
			
			if(prefab)
			{
				bool needsReimport = false;
				
				SpriteMeshInstance[] spriteMeshInstances = prefab.GetComponentsInChildren<SpriteMeshInstance>(true);
				
				foreach(SpriteMeshInstance spriteMeshInstance in spriteMeshInstances)
				{
					if(spriteMeshInstance.spriteMesh)
					{
						if(spriteMeshInstance.cachedSkinnedRenderer)
						{
							needsReimport =  needsReimport || spriteMeshInstance.cachedSkinnedRenderer.sharedMesh != spriteMeshInstance.spriteMesh.sharedMesh;
							
							spriteMeshInstance.cachedSkinnedRenderer.sharedMesh = spriteMeshInstance.spriteMesh.sharedMesh;
							EditorUtility.SetDirty(spriteMeshInstance.cachedSkinnedRenderer);
						}
						
						if(spriteMeshInstance.cachedMeshFilter && spriteMeshInstance.cachedRenderer)
						{
							needsReimport = needsReimport || spriteMeshInstance.cachedMeshFilter.sharedMesh != spriteMeshInstance.spriteMesh.sharedMesh;
							
							spriteMeshInstance.cachedMeshFilter.sharedMesh = spriteMeshInstance.spriteMesh.sharedMesh;
							EditorUtility.SetDirty(spriteMeshInstance.cachedMeshFilter);
						}
					}
				}
				
				if(needsReimport)
				{
					EditorApplication.delayCall += () => { AssetDatabase.ImportAsset(importedAssetPath); };
				}
			}
		}
	}
	
}
