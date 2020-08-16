using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomItemSpawner : MonoBehaviour
{
	[SerializeField] GameObject[] prefabsForSpawn;
	ItemSpawnPoint[] itemSpawnPoints;

    void Awake()
    {
		itemSpawnPoints = FindObjectsOfType<ItemSpawnPoint>();

		SpawnItems();
    }

	private void SpawnItems()
	{
		for (int i = 0; i < prefabsForSpawn.Length; i++)
		{
			int randNumb = Random.Range(0, itemSpawnPoints.Length - 1);

			ItemSpawnPoint spawnPoint = itemSpawnPoints[randNumb];

			if (!spawnPoint.isUsed)
			{
				GameObject item =  Instantiate(prefabsForSpawn[i], spawnPoint.transform.position + new Vector3(0, 0, 0.05f), Quaternion.Euler(90f, 0, 0));
				item.transform.SetParent(spawnPoint.GetComponent<Transform>());
				spawnPoint.isUsed = true;
			}
			else
			{
				i--;
			}
		}
	}
}
