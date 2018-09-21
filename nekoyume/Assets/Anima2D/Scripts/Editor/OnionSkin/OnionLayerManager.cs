using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Anima2D.Pool;

namespace Anima2D
{
	public class PreviewGameObjectCreationPolicy : InstantiateCreationPolicy<GameObject>
	{
		public PreviewGameObjectCreationPolicy(GameObject go) : base(go) { }
		
		public override GameObject Create()
		{
			bool active = original.activeSelf;
			
			original.SetActive(true);
			
			GameObject l_instance = base.Create();
			
			original.SetActive(active);
			
			if(l_instance)
			{
				EditorExtra.InitInstantiatedPreviewRecursive(l_instance);
			}
			
			return l_instance;
		}
	}

	public class OnionLayerCreationPolicy : DefaultCreationPolicy<OnionLayer>
	{
		public override void Destroy(OnionLayer onionLayer)
		{
			onionLayer.Destroy();
		}
	}

	public class OnionLayerGameObjectPool : ObjectPool< GameObject >
	{
		public OnionLayerGameObjectPool(GameObject go) : base( new OnionLayerGameObjectCreationPolicy(go) ) {}
	}
	
	public class OnionLayerPool : ObjectPool< OnionLayer >
	{
		public OnionLayerPool() : base( new OnionLayerCreationPolicy() ) {}
	}
	
	public class OnionLayerManager
	{
		Dictionary<int,OnionLayer> m_OnionLayers = new Dictionary<int,OnionLayer>();
		
		OnionLayerGameObjectPool m_GameObjectPool;
		OnionLayerPool m_OnionLayerPool = new OnionLayerPool();
		
		GameObject m_Source;
		
		public GameObject source
		{
			get {
				return m_Source;
			}
			set {
				if(m_Source != value)
				{
					m_Source = value;
					
					m_OnionLayers.Clear();
					
					m_OnionLayerPool.Clear();
					
					if(m_GameObjectPool != null)
					{
						m_GameObjectPool.Clear();
						m_GameObjectPool = null;
					}
					
					if(m_Source)
					{
						m_GameObjectPool = new OnionLayerGameObjectPool(m_Source);
					}
				}
			}
		}
		
		OnionLayer GetOnionLayer(int frame, AnimationClip clip)
		{
			OnionLayer l_onionLayer = null;
			
			if(!m_OnionLayers.TryGetValue(frame, out l_onionLayer))
			{
				l_onionLayer = m_OnionLayerPool.Get();

				if(!l_onionLayer.previewInstance)
				{
					l_onionLayer.SetPreviewInstance(m_GameObjectPool.Get(), source);
				}
				
				l_onionLayer.previewInstance.transform.position = source.transform.position;
				l_onionLayer.previewInstance.transform.rotation = source.transform.rotation;
				l_onionLayer.previewInstance.transform.localScale = source.transform.localScale;

				l_onionLayer.previewInstance.SetActive(true);

				l_onionLayer.SetFrame(frame,clip);
				
				m_OnionLayers.Add(frame,l_onionLayer);
			}
			
			return l_onionLayer;
		}
		
		void ReturnOnionLayers(int minFrame, int maxFrame, int step)
		{
			List< KeyValuePair<int,OnionLayer> > l_returnOnionLayers = new List< KeyValuePair<int,OnionLayer> >();
			
			if(minFrame <= maxFrame && step > 0)
			{
				foreach(KeyValuePair<int,OnionLayer> pair in m_OnionLayers)
				{
					if(pair.Key < minFrame || pair.Key > maxFrame || pair.Key % step != 0)
					{
						l_returnOnionLayers.Add(pair);
					}
				}
				
				foreach(KeyValuePair<int,OnionLayer> pair in l_returnOnionLayers)
				{
					OnionLayer onionLayer = pair.Value;
					int frame = pair.Key;
					
					onionLayer.previewInstance.SetActive(false);
					
					m_OnionLayerPool.Return(onionLayer);
					m_OnionLayers.Remove(frame);
				}
			}
		}
		
		public void ResampleOnionLayers(AnimationClip clip)
		{
			if(!AnimationMode.InAnimationMode())
			{
				return;
			}

			foreach(KeyValuePair<int,OnionLayer> pair in m_OnionLayers)
			{
				OnionLayer onionLayer = pair.Value;
				int frame = pair.Key;
				
				onionLayer.previewInstance.transform.position = source.transform.position;
				onionLayer.previewInstance.transform.rotation = source.transform.rotation;
				onionLayer.previewInstance.transform.localScale = source.transform.localScale;
				
				pair.Value.SetFrame(frame,clip);
			}
		}
		
		public void UpdateOnionLayers(AnimationClip clip, int frame, int offset, int step, float alphaMultiplier, Color colorPrevFrames, Color colorNextFrames)
		{
			OnionLayer onionLayer;

			if(!AnimationMode.InAnimationMode())
			{
				return;
			}
			
			int frameCount = (int)(AnimationWindowExtra.activeAnimationClip.length * AnimationWindowExtra.activeAnimationClip.frameRate);

			
			int minFrame = Mathf.Max(0,frame - offset * step);
			int maxFrame = Mathf.Min(frame + offset * step,frameCount);
			
			int numLayersPerSide = ((maxFrame - minFrame) / step);

			int depth = numLayersPerSide;

			ReturnOnionLayers(minFrame,maxFrame,step);
			
			int l_frame = step * Mathf.CeilToInt(frame / (float) step);
			for(int i = l_frame - step; i >= minFrame; i -= step)
			{
				onionLayer = GetOnionLayer(i,clip);
				onionLayer.previewInstance.SetActive(true);

				float alpha = 1f - (depth - numLayersPerSide) / (float)numLayersPerSide;
				onionLayer.SetDepth(depth);
				onionLayer.SetAlpha(alpha  * alpha * alpha * alphaMultiplier);
				onionLayer.SetColor(colorPrevFrames);
				
				depth++;
			}
			
			depth = 1;
			
			l_frame = step * (frame / step);
			for(int i = l_frame+step; i <= maxFrame; i += step)
			{
				onionLayer = GetOnionLayer(i,clip);
				onionLayer.previewInstance.SetActive(true);

				float alpha = 1 - (depth - 1) / (float)numLayersPerSide;
				onionLayer.SetDepth(depth);
				onionLayer.SetAlpha(alpha * alpha * alpha * alphaMultiplier);
				onionLayer.SetColor(colorNextFrames);
				
				depth++;
			}
		}
	}
}
