using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace Anima2D
{
	[Serializable]
	public class Edge : ScriptableObject
	{
		public Node node1;
		public Node node2;

		public static Edge Create(Node vertex1, Node vertex2)
		{
			Edge edge = ScriptableObject.CreateInstance<Edge>();
			edge.hideFlags = HideFlags.DontSave;
			edge.node1 = vertex1;
			edge.node2 = vertex2;
			
			return edge;
		}

		public bool ContainsNode(Node node)
		{
			return node1 == node || node2 == node;
		}

		public override bool Equals(System.Object obj) 
		{
			if (obj == null || GetType() != obj.GetType()) 
				return false;
			
			Edge p = (Edge)obj;
			
			return (node1 == p.node1) && (node2 == p.node2) || (node1 == p.node2) && (node2 == p.node1);
		}
		
		public override int GetHashCode() 
		{
			return node1.GetHashCode() ^ node2.GetHashCode();
		}

		public static implicit operator bool(Edge e)
		{
			return e != null;
		}
	}
}
