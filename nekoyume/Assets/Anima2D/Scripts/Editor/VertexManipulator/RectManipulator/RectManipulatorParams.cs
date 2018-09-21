using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[Serializable]
	public struct RectManipulatorParams
	{
		public Vector3 position;
		public Quaternion rotation;

		public RectManipulatorParams(Vector3 _position, Quaternion _rotation)
		{
			position = _position;
			rotation = _rotation;
		}

		public RectManipulatorParams(RectManipulatorParams p)
		{
			position = p.position;
			rotation = p.rotation;
		}
	}
}
