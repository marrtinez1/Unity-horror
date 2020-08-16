using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathHandler : MonoBehaviour
{
	DayHandler dayHandler;
	EnemyAI zombie;

    // Start is called before the first frame update
    void Start()
    {
		dayHandler = FindObjectOfType<DayHandler>();
		zombie = FindObjectOfType<EnemyAI>();
    }

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Enemy"))
		{
			other.GetComponent<Animator>().SetBool("AttackNew", true);
			GetComponentInParent<PlayerMovementStandalone>().SetSpeed(0);
			ETCInput.SetAxisSensitivity("VerticalMovement", 0);
			ETCInput.SetAxisSensitivity("HorizontalMovement", 0);
			zombie.navMeshAgent.enabled = false;
			
			Invoke("RestartDay", 2f);
		}
	}

	private void RestartDay()
	{
		dayHandler.CaughtByZombie();
	}
}
