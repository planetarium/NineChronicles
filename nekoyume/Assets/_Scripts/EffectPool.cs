using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EffectPool : MonoBehaviour
{
	public int initCount = 5;

    private void Start()
    {
        Init();
    }

	private void Init()
	{
		Effect.Pool = this;
		for (int i = 0; i < initCount; ++i)
		{
			Create();
		}
	}

	public GameObject Get()
	{
		for (int i = 0; i < transform.childCount; ++i)
		{
			GameObject go = transform.GetChild(i).gameObject;
			if (go.activeInHierarchy == false)
			{
				go.SetActive(true);
				return go;
			}
		}
		return Create();
	}

	private GameObject Create()
	{
		GameObject go = new GameObject();
		go.name = "Effect";
		go.transform.parent = transform;
		go.AddComponent<SpriteRenderer>();
		go.AddComponent<Effect>();
		go.SetActive(false);
		return go;
	}
}
