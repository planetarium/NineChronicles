using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class PoseManager : MonoBehaviour
	{
		[SerializeField][HideInInspector]
		List<Pose> m_Poses;
	}
}
