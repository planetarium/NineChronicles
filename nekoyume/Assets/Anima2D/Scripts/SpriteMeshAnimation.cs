using UnityEngine;
using System.Collections;

namespace Anima2D
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(SpriteMeshInstance))]
	public class SpriteMeshAnimation : MonoBehaviour
	{
		[SerializeField]
		float m_Frame = 0f;

		[SerializeField]
		SpriteMesh[] m_Frames;

		int m_OldFrame = 0;

		public SpriteMesh[] frames {
			get {
				return m_Frames;
			}
			set {
				m_Frames = value;
			}
		}

		SpriteMeshInstance m_SpriteMeshInstance;
		public SpriteMeshInstance cachedSpriteMeshInstance {
			get {
				if(!m_SpriteMeshInstance)
				{
					m_SpriteMeshInstance = GetComponent<SpriteMeshInstance>();
				}

				return m_SpriteMeshInstance;
			}
		}

		public int frame {
			get {
				return (int)m_Frame;
			}
			set {
				m_Frame = (float)value;
			}
		}

		void LateUpdate()
		{
			if(m_OldFrame != frame &&
			   m_Frames != null &&
			   m_Frames.Length > 0 && m_Frames.Length > frame &&
			   cachedSpriteMeshInstance)
			{
				m_OldFrame = frame;
				cachedSpriteMeshInstance.spriteMesh = m_Frames[frame];
			}
		}
	}
}
