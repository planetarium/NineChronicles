using UnityEngine;
using System.Collections.Generic;
using Anima2D;

public class SkinnedMeshCombiner : MonoBehaviour
{
	[SerializeField]
	private SpriteMeshInstance[] m_SpriteMeshInstances;
	private MaterialPropertyBlock m_MaterialPropertyBlock;
	private SkinnedMeshRenderer m_CachedSkinnedRenderer;

	public SpriteMeshInstance[] spriteMeshInstances
	{
		get { return m_SpriteMeshInstances; }
		set { m_SpriteMeshInstances = value; }
	}

	private MaterialPropertyBlock materialPropertyBlock
	{
		get {
			if(m_MaterialPropertyBlock == null)
			{
				m_MaterialPropertyBlock = new MaterialPropertyBlock();
			}
			
			return m_MaterialPropertyBlock;
		}
	}

	public SkinnedMeshRenderer cachedSkinnedRenderer
	{
		get
		{
			if(!m_CachedSkinnedRenderer)
			{
				m_CachedSkinnedRenderer = GetComponent<SkinnedMeshRenderer>();
			}
			
			return m_CachedSkinnedRenderer;
		}
	}

	void Start()
	{        
		Vector3 l_position = transform.position;
		Quaternion l_rotation = transform.rotation;
		Vector3 l_scale = transform.localScale;

		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		transform.localScale = Vector3.one;

		List<Transform> bones = new List<Transform>();        
		List<BoneWeight> boneWeights = new List<BoneWeight>();        
		List<CombineInstance> combineInstances = new List<CombineInstance>();

		int numSubmeshes = 0;
		
		for (int i = 0; i < spriteMeshInstances.Length; i++)
		{
			SpriteMeshInstance spriteMeshInstance = spriteMeshInstances[i];

			if(spriteMeshInstance.cachedSkinnedRenderer)
			{
				numSubmeshes += spriteMeshInstance.mesh.subMeshCount;
			}
		}
		
		int[] meshIndex = new int[numSubmeshes];
		int boneOffset = 0;
		for( int i = 0; i < m_SpriteMeshInstances.Length; ++i)
		{
			SpriteMeshInstance spriteMeshInstance = spriteMeshInstances[i];

			if(spriteMeshInstance.cachedSkinnedRenderer)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = spriteMeshInstance.cachedSkinnedRenderer;          

				BoneWeight[] meshBoneweight = spriteMeshInstance.sharedMesh.boneWeights;
				
				// May want to modify this if the renderer shares bones as unnecessary bones will get added.
				for (int j = 0; j < meshBoneweight.Length; ++j)
				{
					BoneWeight bw = meshBoneweight[j];
					BoneWeight bWeight = bw;
					bWeight.boneIndex0 += boneOffset;
					bWeight.boneIndex1 += boneOffset;
					bWeight.boneIndex2 += boneOffset;
					bWeight.boneIndex3 += boneOffset;
					boneWeights.Add (bWeight);
				}

				boneOffset += spriteMeshInstance.bones.Count;
				
				Transform[] meshBones = skinnedMeshRenderer.bones;
				for (int j = 0; j < meshBones.Length; j++)
				{
					Transform bone = meshBones[j];
					bones.Add (bone);
				}

				CombineInstance combineInstance = new CombineInstance();
				Mesh mesh = new Mesh();
				skinnedMeshRenderer.BakeMesh(mesh);
				mesh.uv = spriteMeshInstance.spriteMesh.sprite.uv;
				combineInstance.mesh = mesh;
				meshIndex[i] = combineInstance.mesh.vertexCount;
				combineInstance.transform = skinnedMeshRenderer.localToWorldMatrix;
				combineInstances.Add(combineInstance);
				
				skinnedMeshRenderer.gameObject.SetActive(false);
			}
		}
		
		List<Matrix4x4> bindposes = new List<Matrix4x4>();
		
		for( int b = 0; b < bones.Count; b++ ) {
			bindposes.Add( bones[b].worldToLocalMatrix * transform.worldToLocalMatrix );
		}
		
		SkinnedMeshRenderer combinedSkinnedRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
		Mesh combinedMesh = new Mesh();
		combinedMesh.CombineMeshes( combineInstances.ToArray(), true, true );
		combinedSkinnedRenderer.sharedMesh = combinedMesh;
		combinedSkinnedRenderer.bones = bones.ToArray();
		combinedSkinnedRenderer.sharedMesh.boneWeights = boneWeights.ToArray();
		combinedSkinnedRenderer.sharedMesh.bindposes = bindposes.ToArray();
		combinedSkinnedRenderer.sharedMesh.RecalculateBounds();

		combinedSkinnedRenderer.materials = spriteMeshInstances[0].sharedMaterials;

		transform.position = l_position;
		transform.rotation = l_rotation;
		transform.localScale = l_scale;
	}

	void OnWillRenderObject()
	{
		if(cachedSkinnedRenderer)
		{
			if(materialPropertyBlock != null)
			{
				materialPropertyBlock.SetTexture("_MainTex", spriteMeshInstances[0].spriteMesh.sprite.texture);
				
				cachedSkinnedRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}
}