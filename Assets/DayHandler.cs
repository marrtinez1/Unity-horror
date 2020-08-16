using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class DayHandler : MonoBehaviour
{
	private string[] playerSpeechEng = 
	{	"Where am I? What is this place?",
		"My God, I had a terrible dream... Wait... Nooo!",
		"No! Please not again. What they want from me?",
		"Oh God! I have to get out of this place.",
		"Oh God! The last day! I’m going to die here!"
	};

	private string[] playerSpeechCz =
	{   "Kde jsem? Co je to za místo??",
		"Panebože! Měl jsem hrozný sen ... počkat ... Nééé!",
		"Ne! Znovu ne. Co od mě chtějí?",
		"Pane Bože! Musím opustit toto místo.",
		"Bože poslední den! Já tu zemřu!"
	};

	[SerializeField] GameObject HUDMessage;
	[SerializeField] GameObject RespawnView;
	[SerializeField] GameObject GameOverView;
	[SerializeField] Text HUDMessageText;
	[SerializeField] Text DayCountText;

	private Vector3 playerStartingPos = new Vector3(-4.877f, 4.954f, -12.317f);
	private PlayerInteractionWithObjects player;
	private PlayerMovementStandalone playerMovementStandalone;
	private EnemyAI zombie;
	private float playerSpeed = 3f;

	private GameObject[] doors;
	private NoiseMaker[] noiseMakingObjects;

	private int dayCounter;
	private float timeToDisableDayUI = 10f;

	void Start()
	{
		player = FindObjectOfType<PlayerInteractionWithObjects>();
		playerMovementStandalone = FindObjectOfType<PlayerMovementStandalone>();
		zombie = FindObjectOfType<EnemyAI>();
		doors = GameObject.FindGameObjectsWithTag("Door");
		noiseMakingObjects = FindObjectsOfType<NoiseMaker>();

		dayCounter = 1;
		StartNewDayUI();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			CaughtByZombie();
		}
	}

	private void NewDay()
	{
		zombie.NewDayState();
		player.Drop();
		player.transform.position = playerStartingPos;
		ResetMovementSpeed();
		StartNewDayUI();

		foreach (GameObject door in	doors)
		{
			door.GetComponent<Animator>().SetBool("open", false);
		}

		foreach (NoiseMaker item in noiseMakingObjects)
		{
			item.SetOriginalParameters();
		}

		GameObject[] traps = GameObject.FindGameObjectsWithTag("Trap");
		foreach (GameObject trap in traps)
		{
			Destroy(trap);
		}
	}

	private void StartNewDayUI()
	{
		DisableDayUI();
		StopCoroutine(DisableDayUICoroutine());

		RespawnView.SetActive(true);
		Invoke("SpeechTextFadeIn", 4f);

		DayCountText.text = "Day " + dayCounter.ToString();
		HUDMessageText.text = playerSpeechEng[dayCounter - 1];

		StartCoroutine(DisableDayUICoroutine());
	}

	public void CaughtByZombie()
	{
		if (dayCounter >= 5)
		{
			GameOverView.SetActive(true);
			Invoke("RestartGame", 2.5f);
		}
		else
		{
			NewDay();
			dayCounter++;
		}
	}

	private void SpeechTextFadeIn()
	{
		HUDMessage.SetActive(true);
	}

	IEnumerator DisableDayUICoroutine()
	{
		yield return new WaitForSeconds(timeToDisableDayUI);
		DisableDayUI();
	}

	private void DisableDayUI()
	{
		HUDMessage.SetActive(false);
		RespawnView.SetActive(false);
	}

	private void ResetMovementSpeed()
	{
		zombie.GetComponent<Animator>().SetBool("AttackNew", false);
		playerMovementStandalone.SetSpeed(3f);
		ETCInput.SetAxisSensitivity("VerticalMovement", 3f);
		ETCInput.SetAxisSensitivity("HorizontalMovement", 3f);
	}

	private void RestartGame()
	{
		SceneManager.LoadScene(1);
	}
}
