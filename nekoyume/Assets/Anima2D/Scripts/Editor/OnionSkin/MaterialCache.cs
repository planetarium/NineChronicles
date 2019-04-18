using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class MaterialCache
	{
		Material[] m_Materials;
		
		public Material[] materials { get { return m_Materials; } private set { m_Materials = value; } }
		
		public MaterialCache(Renderer renderer)
		{
			if(renderer as SpriteRenderer)
			{
				return;
			}
			
			List<Material> l_materialList = new List<Material>();
			
			foreach(Material material in renderer.sharedMaterials)
			{
				Material materialInstance = null;
				
				if(material)
				{
					materialInstance = GameObject.Instantiate<Material>(material);
					materialInstance.hideFlags = HideFlags.DontSave;
					materialInstance.shader = Shader.Find("Sprites/Default");
				}
				
				l_materialList.Add(materialInstance);
			}
			
			m_Materials = l_materialList.ToArray();
			
			renderer.sharedMaterials = materials;
		}
		
		public void Destroy()
		{
			if(m_Materials != null)
			{
				foreach(Material material in m_Materials)
				{
					if(material)
					{
						GameObject.DestroyImmediate(material);
					}
				}
			}
		}
		
		public void SetColor(Color color)
		{
			if(materials != null)
			{
				foreach(Material material in materials)
				{
					if (material)
					{
						color.a = material.color.a;
						material.color = color;
					}
				}
			}
		}
		
		public void SetAlpha(float alpha)
		{
			if(materials != null)
			{
				foreach(Material material in materials)
				{
					if (material)
					{
						Color color = material.color;
						color.a = alpha;
						material.color = color;
					}
				}
			}
		}
	}
}