using UnityEngine;
using System;

namespace Anima2D 
{
	[Serializable]
	public class BindInfo : ICloneable
	{
		public Matrix4x4 bindPose;
		public float boneLength;

		public Vector3 position { get { return bindPose.inverse * new Vector4 (0f, 0f, 0f, 1f); } }
		public Vector3 endPoint { get { return bindPose.inverse * new Vector4 (boneLength, 0f, 0f, 1f); } }

		public string path;
		public string name;

		public Color color;
		public int zOrder;

		public object Clone()
		{
			return this.MemberwiseClone();
		}

		public override bool Equals(System.Object obj) 
		{
			if (obj == null || GetType() != obj.GetType()) 
				return false;
			
			BindInfo p = (BindInfo)obj;
			
			return Mathf.Approximately((position-p.position).sqrMagnitude,0f) && Mathf.Approximately((endPoint-p.endPoint).sqrMagnitude,0f);
		}
		
		public override int GetHashCode() 
		{
			return position.GetHashCode() ^ endPoint.GetHashCode();
		}

		public static implicit operator bool(BindInfo b)
		{
			return b != null;
		}
	}
}
