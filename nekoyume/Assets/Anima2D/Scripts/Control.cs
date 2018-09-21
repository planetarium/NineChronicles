using UnityEngine;
using System.Collections;

namespace Anima2D
{
	public class Control : MonoBehaviour
	{
		[SerializeField]
		Transform m_BoneTransform;

		public Color color {
			get {
				if(m_CachedBone)
				{
					Color color = m_CachedBone.color;
					color.a = 1f;
					return color;
				}

				return Color.white;
			}
		}

		Bone2D m_CachedBone;
		public Bone2D bone {
			get {
				if(m_CachedBone && m_BoneTransform != m_CachedBone.transform)
				{
					m_CachedBone = null;
				}
				
				if(!m_CachedBone && m_BoneTransform)
				{
					m_CachedBone = m_BoneTransform.GetComponent<Bone2D>();
				}

				return m_CachedBone;
			}
			set {
				m_BoneTransform = value.transform;
			}
		}

		void Start()
		{

		}

		void LateUpdate()
		{
			if(bone)
			{
				transform.position = bone.transform.position;
				transform.rotation = bone.transform.rotation;
			}
		}
	}
}
