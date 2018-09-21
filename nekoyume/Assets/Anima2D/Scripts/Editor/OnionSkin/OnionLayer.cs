#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class OnionLayerGameObjectCreationPolicy : PreviewGameObjectCreationPolicy
	{
		public OnionLayerGameObjectCreationPolicy(GameObject go) : base(go) {}
	}

	public class OnionLayer
	{
		GameObject m_PreviewInstance;
		Renderer[] m_Renderers;
		MaterialCache[] m_MaterialCache;
		
		public Renderer[] renderers { get { return m_Renderers; } private set { m_Renderers = value; } }
		public MaterialCache[] materialCache { get { return m_MaterialCache; } private set { m_MaterialCache = value; } }

		public GameObject previewInstance {
			get { return m_PreviewInstance; }
		}

		public void SetPreviewInstance(GameObject previewInstance, GameObject sourceGameObject)
		{
			if(m_PreviewInstance != previewInstance)
			{
				Destroy();

				m_PreviewInstance = previewInstance;

				if(m_PreviewInstance)
				{
					InitializeRenderers();
				}
			}
		}

		public void Destroy()
		{
			if(m_MaterialCache != null)
			{
				foreach(MaterialCache materialCache in m_MaterialCache)
				{
					if(materialCache != null)
					{
						materialCache.Destroy();
					}
				}
			}

			m_PreviewInstance = null;
		}
		
		void InitializeRenderers()
		{
			Renderer[] l_renderers = m_PreviewInstance.GetComponentsInChildren<Renderer>();
			
			List<string> l_sortingLayerNames = new List<string>(l_renderers.Length);
			
			foreach(Renderer l_renderer in l_renderers)
			{
				l_sortingLayerNames.Add(l_renderer.sortingLayerName);
			}
			
			//Find the deepest used layer
			List<string> editorSortingLayers = EditorExtra.GetSortingLayerNames();
			
			int deepestLayerIndex = int.MaxValue;
			
			foreach(string layerName in l_sortingLayerNames)
			{
				int index = editorSortingLayers.IndexOf(layerName);
				
				if(index < deepestLayerIndex)
				{
					deepestLayerIndex = index;
				}
			}
			
			string deepestLayer = "Default";
			
			if(deepestLayerIndex >= 0 && deepestLayerIndex < editorSortingLayers.Count)
			{
				deepestLayer = editorSortingLayers[deepestLayerIndex];
			}
			
			//Sort renderers front to back taking sorting layer and sorting order into account
			//Set deepest layer to all renderers and calculate the sorting order
			List< KeyValuePair<Renderer, double> > l_renderersOrder = new List< KeyValuePair<Renderer, double> >();
			
			for(int i = 0; i < l_renderers.Length; ++i)
			{
				Renderer l_renderer = l_renderers[i];
				int l_sortingOrder = l_renderer.sortingOrder;
				int l_layerIndex = editorSortingLayers.IndexOf(l_renderer.sortingLayerName);
				
				l_renderer.sortingLayerName = deepestLayer;
				l_renderersOrder.Add(new KeyValuePair<Renderer, double>(l_renderer,(l_layerIndex * 2.0) + (l_sortingOrder / (double)32767)));
			}
			
			l_renderersOrder = l_renderersOrder.OrderByDescending( (s) => s.Value ).ToList();
			
			//Store renderers in order
			renderers = l_renderersOrder.ConvertAll( s => s.Key ).ToArray();
			
			//Create temp materials for non sprite renderers
			List<MaterialCache> l_materialCacheList = new List<MaterialCache>();
			
			foreach(Renderer renderer in renderers)
			{
				l_materialCacheList.Add(new MaterialCache(renderer));
			}
			
			materialCache = l_materialCacheList.ToArray();
		}
		
		public void SetDepth(int depth)
		{
			int l_order = 0;
			
			if(renderers != null)
			{
				foreach(Renderer renderer in renderers)
				{
					renderer.sortingOrder = -(depth * renderers.Length * 2) - l_order;
					l_order++;
				}
			}
		}
		
		public void SetFrame(int frame, AnimationClip clip)
		{
			if(m_PreviewInstance && clip)
			{
				clip.SampleAnimation(m_PreviewInstance, AnimationWindowExtra.FrameToTime(frame));

				IkUtils.UpdateIK(m_PreviewInstance,"",false);
			}
		}
		
		public void SetColor(Color color)
		{
			if(renderers != null)
			{
				foreach(Renderer renderer in renderers)
				{
					SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
					
					if(spriteRenderer)
					{
						color.a = spriteRenderer.color.a;
						spriteRenderer.color = color;
					}
				}
			}
			
			if(materialCache != null)
			{
				foreach(MaterialCache l_materialCache in materialCache)
				{
					l_materialCache.SetColor(color);
				}
			}
		}
		
		public void SetAlpha(float alpha)
		{
			if(renderers != null)
			{
				foreach(Renderer renderer in renderers)
				{
					SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
					
					if(spriteRenderer)
					{
						Color c = spriteRenderer.color;
						c.a = alpha;
						spriteRenderer.color = c;
					}
				}
			}
			
			if(materialCache != null)
			{
				foreach(MaterialCache l_materialCache in materialCache)
				{
					l_materialCache.SetAlpha(alpha);
				}
			}
		}
	}
}
#endif