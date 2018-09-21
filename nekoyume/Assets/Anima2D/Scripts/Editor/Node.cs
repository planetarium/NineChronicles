using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D 
{
	[Serializable]
	public class Node : ScriptableObject
	{
		public int index = -1;

		public static Node Create(int index)
		{
			Node node = ScriptableObject.CreateInstance<Node>();
			node.hideFlags = HideFlags.DontSave;
			node.index = index;
			return node;
		}
	}
}
