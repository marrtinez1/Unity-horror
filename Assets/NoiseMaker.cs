using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
	private Rigidbody rigidbody;
	private EnemyAI zombie;

	Vector3 originalPos;
	[SerializeField] Quaternion originalRot;
	bool isFallen;

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		zombie = FindObjectOfType<EnemyAI>();
		originalPos = transform.position;
		originalRot = transform.rotation;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && !isFallen)
		{
			rigidbody.isKinematic = false;
			isFallen = true;
			zombie.SetTriggerByNoise(true);
			StartCoroutine(ChangeZombieState());
		}
	}

	IEnumerator ChangeZombieState()
	{
		yield return new WaitForSeconds(0.2f);
		zombie.SetTriggerByNoise(false);
	}

	public void SetIsFallen(bool pIsFallen)
	{
		isFallen = pIsFallen;
	}

	public void SetOriginalParameters()
	{
		rigidbody.isKinematic = true;
		transform.position = originalPos;
		transform.rotation = originalRot;
		isFallen = false;
	}
}
