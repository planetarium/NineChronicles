using UnityEngine;
using System.Collections;
using System;

namespace Anima2D
{
	[Serializable]
	public class SpriteMeshData : ScriptableObject
	{
		[SerializeField]
		Vector2 m_PivotPoint;

		[SerializeField]
		Vector2[] m_Vertices = new Vector2[0];

		[SerializeField]
		BoneWeight[] m_BoneWeights = new BoneWeight[0];

		[SerializeField]
		IndexedEdge[] m_Edges = new IndexedEdge[0];

		[SerializeField]
		Vector2[] m_Holes = new Vector2[0];

		[SerializeField]
		int[] m_Indices = new int[0];

		[SerializeField]
		BindInfo[] m_BindPoses = new BindInfo[0];

		[SerializeField]
		BlendShape[] m_Blendshapes = new BlendShape[0];

		public Vector2 pivotPoint
		{
			get {
				return m_PivotPoint;
			}
			set {
				m_PivotPoint = value;
			}
		}

		public Vector2[] vertices
		{
			get {
				return m_Vertices;
			}
			set {
				m_Vertices = value;
			}
		}

		public BoneWeight[] boneWeights
		{
			get {
				return m_BoneWeights;
			}
			set {
				m_BoneWeights = value;
			}
		}

		public IndexedEdge[] edges
		{
			get {
				return m_Edges;
			}
			set {
				m_Edges = value;
			}
		}

		public int[] indices
		{
			get {
				return m_Indices;
			}
			set {
				m_Indices = value;
			}
		}

		public Vector2[] holes
		{
			get {
				return m_Holes;
			}
			set {
				m_Holes = value;
			}
		}

		public BindInfo[] bindPoses
		{
			get {
				return m_BindPoses;
			}
			set {
				m_BindPoses = value;
			}
		}

		public BlendShape[] blendshapes
		{
			get {
				return m_Blendshapes;
			}
			set {
				m_Blendshapes = value;
			}
		}
	}
}
