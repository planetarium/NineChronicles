using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class IkGroup : MonoBehaviour
	{
		[SerializeField][HideInInspector]
		List<Ik2D> m_IkComponents = new List<Ik2D>();

		public void UpdateGroup()
		{
			for (int i = 0; i < m_IkComponents.Count; i++)
			{
				Ik2D ik = m_IkComponents[i];

				if(ik)
				{
					ik.enabled = false;
					ik.UpdateIK();
				}
			}
		}

		void LateUpdate()
		{
			UpdateGroup();
		}
	}
}
