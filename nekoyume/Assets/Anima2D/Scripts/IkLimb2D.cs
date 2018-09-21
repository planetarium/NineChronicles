using UnityEngine;
using System.Collections;

namespace Anima2D
{
	public class IkLimb2D : Ik2D
	{
		public bool flip = false;

		[SerializeField] IkSolver2DLimb m_Solver = new IkSolver2DLimb();
		
		protected override IkSolver2D GetSolver()
		{
			return m_Solver;
		}

		protected override void Validate()
		{
			numBones = 2;
		}

		protected override int ValidateNumBones(int numBones)
		{
			return 2;
		}

		protected override void OnIkUpdate()
		{
			base.OnIkUpdate();

			m_Solver.flip = flip;
		}

		void OnValidate()
		{
			numBones = 2;
		}
	}
}
