using UnityEngine;
using System.Collections;

namespace Anima2D
{
	public class TransformTracker
	{
		Transform m_Transform;
		Vector3 m_Position;
		Quaternion m_Rotation;
		Vector3 m_LocalScale;

		public TransformTracker(Transform transform)
		{
			m_Transform = transform;
			m_Position = m_Transform.position;
			m_Rotation = m_Transform.rotation;
			m_LocalScale = m_Transform.localScale;
		}

		public bool changed
		{
			get {
				return !m_Transform ||
					m_Transform.position != m_Position ||
					m_Transform.rotation != m_Rotation ||
					m_Transform.localScale != m_LocalScale;
			}
		}
	}
}
