using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[Serializable]
	public abstract class IkSolver2D
	{
		[Serializable]
		public class SolverPose 
		{
			[SerializeField]
			Transform m_BoneTransform;

			Bone2D m_CachedBone;
			
			public Bone2D bone
			{
				get
				{					
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
				
				set
				{
					m_CachedBone = value;
					m_BoneTransform = null;

					if(value)
					{
						m_BoneTransform = m_CachedBone.transform;
					}
				}
			}
			public Vector3 solverPosition = Vector3.zero;
			public Quaternion solverRotation = Quaternion.identity;
			public Quaternion defaultLocalRotation = Quaternion.identity;

			public void StoreDefaultPose()
			{
				defaultLocalRotation = bone.transform.localRotation;
			}

			public void RestoreDefaultPose()
			{
				if(bone)
				{
					bone.transform.localRotation = defaultLocalRotation;
				}
			}
		}

		[SerializeField]
		Transform m_RootBoneTransform;

		[SerializeField] List<SolverPose> m_SolverPoses = new List<SolverPose>();
		[SerializeField] float m_Weight = 1f;
		[SerializeField] bool m_RestoreDefaultPose = true;

		Bone2D m_CachedRootBone;

		public Bone2D rootBone {
			get
			{	
				if(m_CachedRootBone && m_RootBoneTransform != m_CachedRootBone.transform)
				{
					m_CachedRootBone = null;
				}
				
				if(!m_CachedRootBone && m_RootBoneTransform)
				{
					m_CachedRootBone = m_RootBoneTransform.GetComponent<Bone2D>();
				}
				
				return m_CachedRootBone;
			}
			private set
			{
				m_CachedRootBone = value;
				m_RootBoneTransform = null;

				if(value)
				{
					m_RootBoneTransform = value.transform;
				}
			}
		}

		public List<SolverPose> solverPoses { get { return m_SolverPoses; } }
		public float weight { 
			get { return m_Weight; } 
			set { 
				m_Weight = Mathf.Clamp01(value);
			}
		}

		public bool restoreDefaultPose {
			get {
				return m_RestoreDefaultPose;
			}
			set {
				m_RestoreDefaultPose = value;
			}
		}

		public Vector3 targetPosition;

		public void Initialize(Bone2D _rootBone, int numChilds)
		{
			rootBone = _rootBone;

			Bone2D bone = rootBone;
			solverPoses.Clear();

			for(int i = 0; i < numChilds; ++i)
			{
				if(bone)
				{
					SolverPose solverPose = new SolverPose();
					solverPose.bone = bone;
					solverPoses.Add(solverPose);
					bone = bone.child;
				}
			}

			StoreDefaultPoses();
		}

		public void Update()
		{
			if(weight > 0f)
			{
				if(restoreDefaultPose)
				{
					RestoreDefaultPoses();
				}

				DoSolverUpdate();
				UpdateBones();
			}
		}

		public void StoreDefaultPoses()
		{
			for (int i = 0; i < solverPoses.Count; i++)
			{
				SolverPose pose = solverPoses [i];
				
				if(pose != null)
				{
					pose.StoreDefaultPose();
				}
			}
		}

		public void RestoreDefaultPoses()
		{
			for (int i = 0; i < solverPoses.Count; i++)
			{
				SolverPose pose = solverPoses [i];
				
				if(pose != null)
				{
					pose.RestoreDefaultPose();
				}
			}
		}

		void UpdateBones()
		{
			for(int i = 0; i < solverPoses.Count; ++i)
			{
				SolverPose solverPose = solverPoses[i];
				
				if(solverPose != null && solverPose.bone)
				{
					if(weight == 1f)
					{
						solverPose.bone.transform.localRotation = solverPose.solverRotation;
					}else{
						solverPose.bone.transform.localRotation = Quaternion.Slerp(solverPose.bone.transform.localRotation,
						                                                           solverPose.solverRotation,
						                                                           weight);
					}
				}
			}
		}

		protected abstract void DoSolverUpdate();
	}
}
