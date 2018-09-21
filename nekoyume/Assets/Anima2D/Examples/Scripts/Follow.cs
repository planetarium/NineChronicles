using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Follow : MonoBehaviour
{
	public Transform target;
	public Vector3 offset;

	void LateUpdate()
	{
		if(target)
		{
			transform.position = target.position + offset;
		}
	}
}
