using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HudControlls : MonoBehaviour
{
	[SerializeField] GameObject pauseView;

	public void PauseView()
	{
		pauseView.SetActive(true);
		Cursor.lockState = CursorLockMode.None;
		Time.timeScale = 0;
	}

	public void ResumeGame()
	{
		pauseView.SetActive(false);
		//Cursor.lockState = CursorLockMode.Locked;
		Time.timeScale = 1;
	}

	public void Restart()
	{
		Time.timeScale = 1;
		SceneManager.LoadScene(1);
	}
}
