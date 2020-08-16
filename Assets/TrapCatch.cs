using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrapCatch : MonoBehaviour
{
	[SerializeField] GameObject HUDTrapEscapeControl;
	[SerializeField] Image ProgressImage;
	PlayerMovementStandalone playerMovement;
	PlayerInteractionWithObjects playerInteraction;
	EnemyAI zombie;
	GameObject trap;

	[SerializeField] float rescueTime = 1.5f;
	float rescueTimeProgress;
	bool isCatchedToTrap;

	private void Start()
	{
		playerMovement = FindObjectOfType<PlayerMovementStandalone>();
		playerInteraction = FindObjectOfType<PlayerInteractionWithObjects>();
		zombie = FindObjectOfType<EnemyAI>();
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.G) & isCatchedToTrap)
		{
			IncrementRescueProgress();
		}
		else
		{
			rescueTimeProgress -= Time.deltaTime;
			if (rescueTimeProgress <= 0)
			{
				rescueTimeProgress = 0;
			}
		}
		RescueImageProgress();
	}

	private void IncrementRescueProgress()
	{
		rescueTimeProgress += Time.deltaTime;
		
		if (rescueTimeProgress >= rescueTime)
		{
			playerMovement.SetSpeed(3f);
			ETCInput.SetAxisSensitivity("VerticalMovement", 3f);
			ETCInput.SetAxisSensitivity("HorizontalMovement", 3f);
			playerInteraction.enabled = true;
			HUDTrapEscapeControl.SetActive(false);

			isCatchedToTrap = false;
			if (trap != null)
			{
				Destroy(trap);
			}
		}
	}

	private void RescueImageProgress()
	{
		float pct = rescueTimeProgress / rescueTime;
		ProgressImage.fillAmount = pct;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Trap"))
		{
			playerMovement.SetSpeed(0);
			ETCInput.SetAxisSensitivity("VerticalMovement", 0);
			ETCInput.SetAxisSensitivity("HorizontalMovement", 0);
			playerInteraction.enabled = false;
			HUDTrapEscapeControl.SetActive(true);

			trap = other.gameObject;
			zombie.SetProvoke(true);
			isCatchedToTrap = true;
		}
	}
}
