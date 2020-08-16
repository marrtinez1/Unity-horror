using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
	[SerializeField] List<Transform> waypoints;
	[SerializeField] GameObject trapPrefab;

	[SerializeField] float patrolSpeed;
	[SerializeField] float patrolAnimatorSpeed;
	[SerializeField] float chasePlayerSpeed;
	[SerializeField] float chasePlayerAnimatorSpeed;
	[SerializeField] float offMeshLinkSpeed = 0.5f;

	PlayerInteractionWithObjects playerTarget;
	public NavMeshAgent navMeshAgent;
	Animator animator;

	float distanceToTarget = 0.5f;
	int currentWP;

	float trapTime = 25f;
	bool isTriggeredByNoise;
	bool isProvoke;

	private void Start()
    {
		playerTarget = FindObjectOfType<PlayerInteractionWithObjects>();
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

		currentWP = 0;
	}

	void Update()
    {
		PatrolOrChase();

		trapTime -= Time.deltaTime;
		if (trapTime <= 0)
		{
			PutTrap();
			trapTime = 25f;
		}
	}

	private void PatrolOrChase()
	{
		if (waypoints.Count == 0) return;

		if (Vector3.Distance(waypoints[currentWP].position, transform.position) < distanceToTarget)
		{
			currentWP++;
			if (currentWP >= waypoints.Count)
			{
				waypoints.Reverse();
				currentWP = 0;
			}
		}

		if (navMeshAgent.enabled)
		{
			if (isProvoke & !playerTarget.GetHiding())
			{
				ChasePlayer();
			}
			else
			{
				Patrol();
			}

			if (navMeshAgent.isOnOffMeshLink) { navMeshAgent.speed = offMeshLinkSpeed; }
		}
	}

	private void Patrol()
	{
		navMeshAgent.SetDestination(waypoints[currentWP].position);

		navMeshAgent.speed = patrolSpeed;
		animator.SetFloat("Speed", patrolAnimatorSpeed);
	}

	private void ChasePlayer()
	{
		navMeshAgent.SetDestination(playerTarget.transform.position);

		navMeshAgent.speed = chasePlayerSpeed;
		animator.SetFloat("Speed", chasePlayerAnimatorSpeed);
	}

	public void SetTriggerByNoise(bool pIsTriggeredByNoise)
	{
		isTriggeredByNoise = pIsTriggeredByNoise;

	}

	public void SetProvoke(bool pIsProvoke)
	{
		isProvoke = pIsProvoke;
	}

	public void NewDayState()
	{
		navMeshAgent.enabled = false;

		int randNumb = Random.Range(0, waypoints.Count);
		Vector3 positionNextDay = waypoints[randNumb].transform.position;
		transform.position = positionNextDay;
		isProvoke = false;

		navMeshAgent.enabled = true;
	}

	private void PutTrap()
	{
		Vector3 trapPosition = transform.position;
		trapPosition.y -= 0.1f;
		GameObject trap = Instantiate(trapPrefab, trapPosition, Quaternion.identity);
		Destroy(trap, 30f);
	}

	private void OnTriggerStay(Collider other)
	{
		if (isTriggeredByNoise & other.CompareTag("Player"))
		{
			isProvoke = true;
		}
	}
}
