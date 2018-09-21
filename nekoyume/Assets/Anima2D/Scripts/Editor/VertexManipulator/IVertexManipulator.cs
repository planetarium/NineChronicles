using UnityEngine;
using System.Collections;

namespace Anima2D
{
	public interface IVertexManipulator
	{
		void AddVertexManipulable(IVertexManipulable vertexManipulable);
		void Clear();
		void DoManipulate();
	}
}
