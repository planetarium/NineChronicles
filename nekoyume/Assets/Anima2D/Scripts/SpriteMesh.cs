using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D 
{
	public class SpriteMesh : ScriptableObject
	{
		public const int api_version = 4;

		[SerializeField][HideInInspector]
		int m_ApiVersion;

		[SerializeField][FormerlySerializedAs("sprite")]
		Sprite m_Sprite;

		[SerializeField]
		Mesh m_SharedMesh;

		public Sprite sprite { get { return m_Sprite; } }
		public Mesh sharedMesh { get { return m_SharedMesh; } }

#region DEPRECATED
#if UNITY_EDITOR
		[Serializable]
		public class Vertex
		{
			public Vector2 vertex;
			public BoneWeight2 boneWeight;
		}

		[Serializable]
		public class BoneWeight2
		{
			public float weight0 = 0f;
			public float weight1 = 0f;
			public float weight2 = 0f;
			public float weight3 = 0f;
			public int boneIndex0 = 0;
			public int boneIndex1 = 0;
			public int boneIndex2 = 0;
			public int boneIndex3 = 0;
		}

		[Serializable]
		public class IndexedEdge
		{
			public int index1;
			public int index2;
		}

		[Serializable]
		public class Hole
		{
			public Vector2 vertex;
		}

		[Serializable]
		public class BindInfo
		{
			public Matrix4x4 bindPose;
			public float boneLength;
			public string path;
			public string name;
			public Color color;
			public int zOrder;
		}

		[SerializeField][HideInInspector] Vector2 pivotPoint;
		[SerializeField][HideInInspector] Vertex[] texVertices;
		[SerializeField][HideInInspector] IndexedEdge[] edges;
		[SerializeField][HideInInspector] Hole[] holes;
		[SerializeField][HideInInspector] int[] indices;
		[SerializeField][HideInInspector] BindInfo[] bindPoses;
		[SerializeField][HideInInspector] Material[] m_SharedMaterials;

#endif
#endregion
	}
}
