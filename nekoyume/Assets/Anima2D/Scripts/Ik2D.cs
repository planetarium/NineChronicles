using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D 
{
	public abstract class Ik2D : MonoBehaviour
	{
		//Deprecated
		[SerializeField][HideInInspector]
		Bone2D m_Target;

		[SerializeField]
		bool m_Record = false;

		[SerializeField]
		Transform m_TargetTransform;

		[SerializeField] int m_NumBones = 0;
		[SerializeField] float m_Weight = 1f;
		[SerializeField] bool m_RestoreDefaultPose = true;
		[SerializeField] bool m_OrientChild = true;

		public IkSolver2D solver { get { return GetSolver(); } }

		public bool record { get { return m_Record; } }

		Bone2D m_CachedTarget;

		public Bone2D target {
			get {
				if(m_Target)
				{
					target = m_Target;
				}
				
				if(m_CachedTarget && m_TargetTransform != m_CachedTarget.transform)
				{
					m_CachedTarget = null;
				}
				
				if(!m_CachedTarget && m_TargetTransform)
				{
					m_CachedTarget = m_TargetTransform.GetComponent<Bone2D>();
				}
				
				return m_CachedTarget;
			}
			set {
				m_CachedTarget = value;
				m_TargetTransform = value.transform;

				if(!m_Target)
				{
					InitializeSolver();
				}

				m_Target = null;
			}
		}
		
		public int numBones
		{
			get { return ValidateNumBones(m_NumBones); }
			set {
				int l_numBones = ValidateNumBones(value);

				if(l_numBones != m_NumBones)
				{
					m_NumBones = l_numBones;
					InitializeSolver();
				}
			}
		}
		
		public float weight
		{
			get { return m_Weight; }
			set { m_Weight = value; }
		}

		public bool restoreDefaultPose
		{
			get { return m_RestoreDefaultPose; }
			set { m_RestoreDefaultPose = value; }
		}

		public bool orientChild
		{
			get { return m_OrientChild; }
			set { m_OrientChild = value; }
		}

		void OnDrawGizmos()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			if(enabled && target && numBones > 0)
			{
				Gizmos.DrawIcon(transform.position,"ikGoal");
			}else{
				Gizmos.DrawIcon(transform.position,"ikGoalDisabled");
			}
		}

		void OnValidate()
		{
			Validate();
		}

		void Start()
		{
			OnStart();
		}

		void Update()
		{
			SetAttachedIK(this);

			OnUpdate();
		}

		void LateUpdate()
		{
			OnLateUpdate();

			UpdateIK();
		}

		void SetAttachedIK(Ik2D ik2D)
		{
			for (int i = 0; i < solver.solverPoses.Count; i++)
			{
				IkSolver2D.SolverPose pose = solver.solverPoses[i];
				
				if(pose.bone)
				{
					pose.bone.attachedIK = ik2D;
				}
			}
		}

		public void UpdateIK()
		{
			OnIkUpdate();

			solver.Update();

			if(orientChild && target.child)
			{
				target.child.transform.rotation = transform.rotation;
			}
		}

		protected virtual void OnIkUpdate()
		{
			solver.weight = weight;
			solver.targetPosition = transform.position;
			solver.restoreDefaultPose = restoreDefaultPose;
		}

		void InitializeSolver()
		{
			Bone2D rootBone = Bone2D.GetChainBoneByIndex(target, numBones-1);

			SetAttachedIK(null);

			solver.Initialize(rootBone,numBones);
		}

		protected virtual int ValidateNumBones(int numBones) { return numBones; }
		protected virtual void Validate() {}
		protected virtual void OnStart() {}
		protected virtual void OnUpdate() {}
		protected virtual void OnLateUpdate() {}

		protected abstract IkSolver2D GetSolver();
	}
}
