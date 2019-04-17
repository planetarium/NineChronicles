using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Anima2D
{
	[ExecuteInEditMode]
	public class SpriteMeshInstance : MonoBehaviour
	{
		[SerializeField]
		SpriteMesh m_SpriteMesh;
		
		[SerializeField]
		Color m_Color = Color.white;
		
		[SerializeField]
		Material[] m_Materials;
		
		[SerializeField]
		int m_SortingLayerID = 0;
		
		[SerializeField]
		int m_SortingOrder = 0;

		[SerializeField][HideInInspector]
		Transform[] m_BoneTransforms;

		public SpriteMesh spriteMesh {
			get { return m_SpriteMesh; }
			set { m_SpriteMesh = value; }
		}
		
		public Material sharedMaterial {
			get {
				if(m_Materials.Length > 0)
				{
					return m_Materials[0];
				}
				return null;
			}
			set {
				m_Materials = new Material[] { value };
			}
		}
		
		public Material[] sharedMaterials {
			get { return m_Materials; }
			set { m_Materials = value; }
		}
		
		public Color color {
			get { return m_Color; }
			set { m_Color = value; }
		}
		
		public int sortingLayerID {
			get { return m_SortingLayerID; }
			set { m_SortingLayerID = value; }
		}

		public string sortingLayerName {
			get {
				if(cachedRenderer)
				{
					return cachedRenderer.sortingLayerName;
				}

				return "Default";
			}
			set {
				if(cachedRenderer)
				{
					cachedRenderer.sortingLayerName = value;
					sortingLayerID = cachedRenderer.sortingLayerID;
				}
			}
		}

		public int sortingOrder {
			get { return m_SortingOrder; }
			set { m_SortingOrder = value; }
		}

		List<Bone2D> m_CachedBones = new List<Bone2D>();

		public List<Bone2D> bones {
			get
			{
				if(m_BoneTransforms != null && m_CachedBones.Count != m_BoneTransforms.Length)
				{
					m_CachedBones = new List<Bone2D>(m_BoneTransforms.Length);

					for (int i = 0; i < m_BoneTransforms.Length; i++)
					{
						Bone2D l_Bone = null;

						if(m_BoneTransforms[i])
						{
							l_Bone = m_BoneTransforms[i].GetComponent<Bone2D>();
						}

						m_CachedBones.Add(l_Bone);
					}
				}

				for (int i = 0; i < m_CachedBones.Count; i++)
				{
					if(m_CachedBones[i] && m_BoneTransforms[i] != m_CachedBones[i].transform)
					{
						m_CachedBones[i] = null;
					}
					if(!m_CachedBones[i] && m_BoneTransforms[i])
					{
						m_CachedBones[i] = m_BoneTransforms[i].GetComponent<Bone2D>();
					}
				}

				return m_CachedBones;
			}
			
			set
			{
				m_CachedBones = new List<Bone2D>(value);
				m_BoneTransforms = new Transform[m_CachedBones.Count];

				for (int i = 0; i < m_CachedBones.Count; i++)
				{
					Bone2D bone = m_CachedBones[i];
					if(bone)
					{
						m_BoneTransforms[i] = bone.transform;
					}
				}

				if(cachedSkinnedRenderer)
				{
					cachedSkinnedRenderer.bones = m_BoneTransforms;
				}
			}
		}
		
		MaterialPropertyBlock m_MaterialPropertyBlock;
		MaterialPropertyBlock materialPropertyBlock {
			get {
				if(m_MaterialPropertyBlock == null)
				{
					m_MaterialPropertyBlock = new MaterialPropertyBlock();
				}
				
				return m_MaterialPropertyBlock;
			}
		}
		
		Renderer mCachedRenderer;
		public Renderer cachedRenderer {
			get {
				if(!mCachedRenderer)
				{
					mCachedRenderer = GetComponent<Renderer>();
				}
				
				return mCachedRenderer;
			}
		}
		
		MeshFilter mCachedMeshFilter;
		public MeshFilter cachedMeshFilter {
			get {
				if(!mCachedMeshFilter)
				{
					mCachedMeshFilter = GetComponent<MeshFilter>();
				}
				
				return mCachedMeshFilter;
			}
		}
		
		SkinnedMeshRenderer mCachedSkinnedRenderer;
		public SkinnedMeshRenderer cachedSkinnedRenderer {
			get {
				if(!mCachedSkinnedRenderer)
				{
					mCachedSkinnedRenderer = GetComponent<SkinnedMeshRenderer>();
				}
				
				return mCachedSkinnedRenderer;
			}
		}
		
		Texture2D spriteTexture {
			get {
				if(spriteMesh && spriteMesh.sprite)
				{
					return spriteMesh.sprite.texture;
				}
				
				return null;
			}
		}
		
		Mesh m_InitialMesh = null;
		Mesh m_CurrentMesh = null;

		public Mesh sharedMesh {
			get {
				if(m_InitialMesh)
				{
					return m_InitialMesh;
				}
				
				return null;
			}
		}

		public Mesh mesh {
			get {
				if(m_CurrentMesh)
				{
					return GameObject.Instantiate(m_CurrentMesh);
				}

				return null;
			}
		}

#if UNITY_EDITOR
		ulong m_AssetTimeStamp = 0;
#endif

		void OnDestroy()
		{
			if(m_CurrentMesh)
			{
#if UNITY_EDITOR
				if(Application.isPlaying)
				{
					Destroy(m_CurrentMesh);
				}else{
					DestroyImmediate(m_CurrentMesh);
				}
#else
				Destroy(m_CurrentMesh);
#endif
			}
		}
	
#if UNITY_EDITOR
		void UpdateTimestamp()
		{
			if(!Application.isPlaying && spriteMesh)
			{
				m_AssetTimeStamp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteMesh)).assetTimeStamp;
			}
		}
#endif

		void Awake()
		{
#if UNITY_EDITOR
			UpdateTimestamp();
#endif
			UpdateCurrentMesh();
		}

		void UpdateInitialMesh()
		{
			m_InitialMesh = null;

			if(spriteMesh && spriteMesh.sharedMesh)
			{
				m_InitialMesh = spriteMesh.sharedMesh;
			}
		}

		void UpdateCurrentMesh()
		{
			UpdateInitialMesh();

			if(m_InitialMesh)
			{
				if(!m_CurrentMesh)
				{
					m_CurrentMesh = new Mesh();
					m_CurrentMesh.hideFlags = HideFlags.DontSave;
					m_CurrentMesh.MarkDynamic();
				}

				m_CurrentMesh.Clear();
				m_CurrentMesh.name = m_InitialMesh.name;
				m_CurrentMesh.vertices = m_InitialMesh.vertices;
				m_CurrentMesh.uv = m_InitialMesh.uv;
				m_CurrentMesh.normals = m_InitialMesh.normals;
				m_CurrentMesh.tangents = m_InitialMesh.tangents;
				m_CurrentMesh.boneWeights = m_InitialMesh.boneWeights;
				m_CurrentMesh.bindposes = m_InitialMesh.bindposes;
				m_CurrentMesh.bounds = m_InitialMesh.bounds;
				m_CurrentMesh.colors = m_InitialMesh.colors;

				for(int i = 0; i < m_InitialMesh.subMeshCount; ++i)
				{
					m_CurrentMesh.SetTriangles(m_InitialMesh.GetTriangles(i),i);
				}


#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2)

				m_CurrentMesh.ClearBlendShapes();

				for(int i = 0; i < m_InitialMesh.blendShapeCount; ++i)
				{
					string blendshapeName = m_InitialMesh.GetBlendShapeName(i);

					for(int j = 0; j < m_InitialMesh.GetBlendShapeFrameCount(i); ++j)
					{
						float weight = m_InitialMesh.GetBlendShapeFrameWeight(i,j);

						Vector3[] vertices = new Vector3[m_InitialMesh.vertexCount];

						m_InitialMesh.GetBlendShapeFrameVertices(i,j,vertices,null,null);

						m_CurrentMesh.AddBlendShapeFrame(blendshapeName, weight, vertices, null, null);
					}
				}
#endif
				m_CurrentMesh.hideFlags = HideFlags.DontSave;

			}else{
				m_InitialMesh = null;

				if(m_CurrentMesh)
				{
					m_CurrentMesh.Clear();
				}
			}
			
			if(m_CurrentMesh)
			{
				if(spriteMesh && spriteMesh.sprite && spriteMesh.sprite.packed)
				{
					SetSpriteUVs(m_CurrentMesh,spriteMesh.sprite);
				}

				m_CurrentMesh.UploadMeshData(false);
			}

			UpdateRenderers();

#if UNITY_EDITOR
			UpdateTimestamp();
#endif
		}

		void SetSpriteUVs(Mesh mesh, Sprite sprite)
		{
			Vector2[] spriteUVs = sprite.uv;

			if(mesh.vertexCount == spriteUVs.Length)
			{
				mesh.uv = sprite.uv;
			}
		}

		void UpdateRenderers()
		{
			Mesh l_mesh = null;

			if(m_InitialMesh)
			{
				l_mesh = m_CurrentMesh;	
			}

			if(cachedSkinnedRenderer)
			{
				cachedSkinnedRenderer.sharedMesh = l_mesh;
			}else if(cachedMeshFilter)
			{
				cachedMeshFilter.sharedMesh = l_mesh;
			}
		}

		void LateUpdate()
		{
			if(!spriteMesh || (spriteMesh && spriteMesh.sharedMesh != m_InitialMesh))
			{
				UpdateCurrentMesh();
			}

#if UNITY_EDITOR
			if(!Application.isPlaying && spriteMesh)
			{
				ulong l_AssetTimeStamp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteMesh)).assetTimeStamp;

				if(m_AssetTimeStamp != l_AssetTimeStamp)
				{
					UpdateCurrentMesh();
				}
			}
#endif	
		}

		void OnWillRenderObject()
		{
			UpdateRenderers();

			if(cachedRenderer)
			{
				cachedRenderer.sortingLayerID = sortingLayerID;
				cachedRenderer.sortingOrder = sortingOrder;
				cachedRenderer.sharedMaterials = m_Materials;
				cachedRenderer.GetPropertyBlock(materialPropertyBlock);

				if(spriteTexture)
				{
					materialPropertyBlock.SetTexture("_MainTex", spriteTexture);
				}

				materialPropertyBlock.SetColor("_Color",color);
				
				cachedRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}
}
