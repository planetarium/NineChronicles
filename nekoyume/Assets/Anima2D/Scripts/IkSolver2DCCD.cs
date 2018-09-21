using UnityEngine;
using System;
using System.Collections;

namespace Anima2D
{
	[Serializable]
	public class IkSolver2DCCD : IkSolver2D
	{
		public int iterations = 10;
		public float damping = 0.8f;

		protected override void DoSolverUpdate()
		{
			if(!rootBone) return;

			for(int i = 0; i < solverPoses.Count; ++i)
			{
				SolverPose solverPose = solverPoses[i];
				
				if(solverPose != null && solverPose.bone)
				{
					solverPose.solverRotation = solverPose.bone.transform.localRotation;
					solverPose.solverPosition = rootBone.transform.InverseTransformPoint(solverPose.bone.transform.position);
				}
			}
		
			Vector3 localEndPosition = rootBone.transform.InverseTransformPoint(solverPoses[solverPoses.Count-1].bone.endPosition);
			Vector3 localTargetPosition = rootBone.transform.InverseTransformPoint(targetPosition);
			
			damping = Mathf.Clamp01(damping);

			float l_damping = 1f - Mathf.Lerp(0f,0.99f,damping);

			for(int i = 0; i < iterations; ++i)
			{
				for(int j = solverPoses.Count-1; j >= 0; --j)
				{
					SolverPose solverPose = solverPoses[j];

					Vector3 toTarget = localTargetPosition - solverPose.solverPosition;
					Vector3 toEnd = localEndPosition - solverPose.solverPosition;
					toTarget.z = 0f;
					toEnd.z = 0f;
					
					float localAngleDelta = MathUtils.SignedAngle(toEnd, toTarget, Vector3.forward);

					localAngleDelta *=  l_damping;

					Quaternion localRotation = Quaternion.AngleAxis(localAngleDelta,Vector3.forward);
					
					solverPose.solverRotation = solverPose.solverRotation * localRotation;
					
					Vector3 pivotPosition = solverPose.solverPosition;

					localEndPosition = RotatePositionFrom(localEndPosition,pivotPosition,localRotation);

					for(int k = solverPoses.Count-1; k > j; --k)
					{
						SolverPose sp = solverPoses[k];
						sp.solverPosition = RotatePositionFrom(sp.solverPosition,pivotPosition,localRotation);
					}

				}
			}
		}

		Vector3 RotatePositionFrom(Vector3 position, Vector3 pivot, Quaternion rotation)
		{
			Vector3 v = position - pivot;
			v = rotation * v;
			return pivot + v;
		}
	}
}
