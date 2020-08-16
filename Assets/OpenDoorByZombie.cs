using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoorByZombie : MonoBehaviour
{
	Animator animator;

	private void Start()
	{
		animator = GetComponent<Animator>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Upper Body"))
		{
			animator.SetBool("open", true);
		}
	}
}
