using UnityEngine;
using System;
using System.Collections;

namespace Anima2D 
{
	[Serializable]
	public class Hole : ICloneable
	{
		public Vector2 vertex = Vector2.zero;

		public Hole(Vector2 vertex)
		{
			this.vertex = vertex;
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}

		public static implicit operator bool(Hole h)
		{
			return h != null;
		}
	}
}
