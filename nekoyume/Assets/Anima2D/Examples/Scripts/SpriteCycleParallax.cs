using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteCycle))]
public class SpriteCycleParallax : MonoBehaviour
{
	public Transform target;
	public Vector2 factor;

	SpriteCycle spriteCicle;
	
	void Awake()
	{
		spriteCicle = GetComponent<SpriteCycle>();
	}

	void Start()
	{
		if(!target)
		{
			if(Camera.main)
			{
				target = Camera.main.transform;
			}
		}
	}

	void Update()
	{
		if(target && spriteCicle)
		{
			spriteCicle.position = target.position.x*factor.x;

			Vector3 localPosition = transform.localPosition;
			localPosition.y = target.position.y*factor.y;
			transform.localPosition = localPosition;
		}
	}
}
