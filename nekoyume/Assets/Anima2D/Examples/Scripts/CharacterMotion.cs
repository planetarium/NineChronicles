using UnityEngine;
using System.Collections;

public class CharacterMotion : MonoBehaviour
{
	Animator animator;

	void Start()
	{
		animator = GetComponent<Animator>();
	}

	void Update ()
	{
		float xAxis = Input.GetAxis("Horizontal");

		Vector3 eulerAngles = transform.localEulerAngles;

		if(xAxis < 0f)
		{
			eulerAngles.y = 180f;
		}else if(xAxis > 0f)
		{
			eulerAngles.y = 0f;
		}

		transform.localRotation = Quaternion.Euler(eulerAngles);

		animator.SetFloat("Forward", Mathf.Abs(xAxis));
	}
}
