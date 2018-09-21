using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class SpriteMeshInstanceTracker
	{
		List<TransformTracker> m_TransformTrackers = new List<TransformTracker>();

		Dictionary<int,float> m_BlendShapeWeightTracker = new Dictionary<int, float>();

		SpriteMeshInstance m_SpriteMeshInstance;

		SpriteMesh m_SpriteMesh;

		public SpriteMeshInstance spriteMeshInstance
		{
			get {
				return m_SpriteMeshInstance;
			}
			set {
				m_SpriteMeshInstance = value;
				Update();
			}
		}

		public void Update()
		{
			m_TransformTrackers.Clear();
			m_BlendShapeWeightTracker.Clear();
			m_SpriteMesh = null;

			if(m_SpriteMeshInstance && m_SpriteMeshInstance.spriteMesh)
			{
				m_SpriteMesh = m_SpriteMeshInstance.spriteMesh;

				m_TransformTrackers.Add( new TransformTracker(m_SpriteMeshInstance.transform) );

				foreach(Bone2D bone in m_SpriteMeshInstance.bones)
				{
					m_TransformTrackers.Add( new TransformTracker(bone.transform) );
				}

				if(m_SpriteMeshInstance.cachedSkinnedRenderer)
				{
					int blendShapeCount = m_SpriteMeshInstance.sharedMesh.blendShapeCount;

					for(int i = 0; i < blendShapeCount; ++i)
					{
						m_BlendShapeWeightTracker.Add( i, m_SpriteMeshInstance.cachedSkinnedRenderer.GetBlendShapeWeight(i) );
					}
				}
			}
		}

		public bool spriteMeshChanged {
			get {
				if(m_SpriteMeshInstance)
				{
					return m_SpriteMesh != m_SpriteMeshInstance.spriteMesh;
				}

				return false;
			}	
		}

		public bool changed {
			get {

				if(spriteMeshChanged)
				{
					return true;
				}

				if(m_SpriteMeshInstance)
				{
					if(m_SpriteMesh && m_SpriteMeshInstance.cachedSkinnedRenderer)
					{
						int blendShapeCount = m_SpriteMeshInstance.sharedMesh.blendShapeCount;

						if(blendShapeCount != m_BlendShapeWeightTracker.Count)
						{
							return true;
						}

						for(int i = 0; i < blendShapeCount; ++i)
						{
							float weight = 0f;

							if(m_BlendShapeWeightTracker.TryGetValue(i, out weight))
							{
								if(m_SpriteMeshInstance.cachedSkinnedRenderer.GetBlendShapeWeight(i) != weight)
								{
									return true;
								}
							}
						}

						foreach(TransformTracker tracker in m_TransformTrackers)
						{
							if(tracker.changed)
							{
								return true;
							}
						}
					}
				}

				return false;
			}
		}
	}
}
