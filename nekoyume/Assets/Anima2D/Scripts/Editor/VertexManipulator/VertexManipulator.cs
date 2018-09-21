using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class VertexManipulator : IVertexManipulator
	{
		List<IVertexManipulable> m_VertexManipulables = new List<IVertexManipulable>();

		public List<IVertexManipulable> manipulables {
			get { return m_VertexManipulables; }	
		}

		public void AddVertexManipulable(IVertexManipulable vertexManipulable)
		{
			if(vertexManipulable != null)
			{
				m_VertexManipulables.Add(vertexManipulable);
			}
		}

		public void Clear()
		{
			m_VertexManipulables.Clear();
		}

		public virtual void DoManipulate() {}
	}
}
