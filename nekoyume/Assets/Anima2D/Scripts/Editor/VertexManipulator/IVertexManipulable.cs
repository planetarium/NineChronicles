using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public interface IVertexManipulable
	{
		int GetManipulableVertexCount();
		Vector3 GetManipulableVertex(int index);
		void SetManipulatedVertex(int index, Vector3 vertex);
	}
}
