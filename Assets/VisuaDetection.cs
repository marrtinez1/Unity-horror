using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisuaDetection : MonoBehaviour
{
	EnemyAI zombie;
	PlayerInteractionWithObjects player;

	private void Start()
	{
		zombie = GetComponentInParent<EnemyAI>();
		player = FindObjectOfType<PlayerInteractionWithObjects>();
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player") & !player.GetHiding())
		{
			zombie.SetProvoke(true);
		}
	}
}
