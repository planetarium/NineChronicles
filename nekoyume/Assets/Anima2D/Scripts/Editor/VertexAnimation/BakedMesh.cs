using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class BakedMesh
	{
		Mesh m_ProxyMesh;
	 	Mesh proxyMesh
		{
			get {
				if(!m_ProxyMesh)
				{
					m_ProxyMesh = new Mesh();
					m_ProxyMesh.hideFlags = HideFlags.DontSave;
					m_ProxyMesh.MarkDynamic();
				}

				return m_ProxyMesh;
			}
		}

		public Vector3[] vertices {
			get {
				return proxyMesh.vertices;
			}
			set {
				proxyMesh.vertices = value;
			}
		}

		SkinnedMeshRenderer m_SkinnedMeshRenderer;
		public SkinnedMeshRenderer skinnedMeshRenderer {
			get { return m_SkinnedMeshRenderer; }
			set {
				if(m_SkinnedMeshRenderer != value)
				{
					m_SkinnedMeshRenderer = value;
					Bake();
				}
			}
		}

		public void Bake()
		{
			if(skinnedMeshRenderer)
			{
				skinnedMeshRenderer.BakeMesh(proxyMesh);
			}
		}

		public void Destroy()
		{
			if(m_ProxyMesh)
			{
				GameObject.DestroyImmediate(m_ProxyMesh);	
			}
		}

	}
}
