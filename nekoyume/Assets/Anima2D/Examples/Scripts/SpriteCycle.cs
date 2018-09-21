using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SpriteCycle : MonoBehaviour
{
	public List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

	[Range(0,1)]
	public float offset = 0f;
	
	float totalWidth = 1f;

	float mPosition = 0f;
	public float position
	{
		get{
			return mPosition;
		}
		set {

			float scaleX = transform.localScale.x;

			mPosition = value;

			if(scaleX > 0f)
			{
				mPosition /= scaleX;
			}

			Vector3 l_position = Vector3.zero;
			
			totalWidth = 0f;
			
			for(int i = 0; i < spriteRenderers.Count; ++i)
			{
				SpriteRenderer sr = spriteRenderers[i];
				
				if(sr)
				{
					if(sr.sprite)
					{
						sr.transform.localPosition = l_position;
						l_position.x += sr.sprite.bounds.size.x;
						totalWidth += sr.sprite.bounds.size.x;
					}
				}
			}

			float dx = mPosition % totalWidth;

			for(int i = 0; i < spriteRenderers.Count; ++i)
			{
				SpriteRenderer sr = spriteRenderers[i];
				
				if(sr)
				{
					if(sr.sprite)
					{
						Vector3 localPos = sr.transform.localPosition + Vector3.right*dx;

						if(localPos.x <= -sr.sprite.bounds.size.x)
						{
							localPos.x += totalWidth;

						}else if(localPos.x > totalWidth)
						{
							localPos.x -= totalWidth;
						}

						localPos.x -= offset*totalWidth;

						sr.transform.localPosition = localPos;
					}
			    }
			}
		}
	}
	
	void Awake()
	{
		position = 0f;
	}

	void OnValidate()
	{
		position = 0f;
	}
}
