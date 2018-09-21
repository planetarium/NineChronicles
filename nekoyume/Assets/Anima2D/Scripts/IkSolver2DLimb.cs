using UnityEngine;
using System;
using System.Collections;

namespace Anima2D
{
	[Serializable]
	public class IkSolver2DLimb : IkSolver2D
	{
		public bool flip = false;

		protected override void DoSolverUpdate()
		{
			if(!rootBone || solverPoses.Count != 2) return;

			SolverPose pose0 = solverPoses[0];
			SolverPose pose1 = solverPoses[1];

			Vector3 localTargetPosition = targetPosition - rootBone.transform.position;
			localTargetPosition.z = 0f;

			float distanceMagnitude = localTargetPosition.magnitude;
			
			float angle0 = 0f;
			float angle1 = 0f;
			
			float sqrDistance = localTargetPosition.sqrMagnitude;
			
			float sqrParentLength = (pose0.bone.length * pose0.bone.length);
			float sqrTargetLength = (pose1.bone.length * pose1.bone.length);
			
			float angle0Cos = (sqrDistance + sqrParentLength - sqrTargetLength) / (2f * pose0.bone.length * distanceMagnitude);
			float angle1Cos = (sqrDistance - sqrParentLength - sqrTargetLength) / (2f * pose0.bone.length * pose1.bone.length);
			
			if((angle0Cos >= -1f && angle0Cos <= 1f) && (angle1Cos >= -1f && angle1Cos <= 1f))
			{
				angle0 = Mathf.Acos(angle0Cos) * Mathf.Rad2Deg;
				angle1 = Mathf.Acos(angle1Cos) * Mathf.Rad2Deg;
			}
			
			float flipSign = flip ? -1f : 1f;

			Vector3 rootBoneToTarget = Vector3.ProjectOnPlane(targetPosition - rootBone.transform.position,rootBone.transform.forward);

			if(rootBone.transform.parent)
			{
				rootBoneToTarget = rootBone.transform.parent.InverseTransformDirection(rootBoneToTarget);
			}

			float baseAngle = Mathf.Atan2(rootBoneToTarget.y, rootBoneToTarget.x) * Mathf.Rad2Deg;

			pose0.solverRotation = Quaternion.Euler(0f,0f, baseAngle - flipSign * angle0);
			pose1.solverRotation = Quaternion.Euler(0f,0f, flipSign * angle1);
		}
	}
}
