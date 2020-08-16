using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLookHiding : MonoBehaviour
{
	PlayerInteractionWithObjects playerInteractionWithObjects;

	//Standalone
	public float mouseSensitivity = 100f;

	float xRotation = 0f;
	float yRotation = 0f;
	
	bool mobileInput;

	//Mobile
	[SerializeField] GameObject touchPad;
	[SerializeField] Camera cam;

	Touch initTouch = new Touch();

	//private float rotX = 0f;
	private float rotY = 0f;
	private Vector3 origRot;

	public float rotSpeed = 0.5f;

	private void Awake()
	{
		playerInteractionWithObjects = FindObjectOfType<PlayerInteractionWithObjects>();
		mobileInput = playerInteractionWithObjects.IsMobileInput();

		touchPad.SetActive(false);
		origRot = cam.transform.eulerAngles;
		//rotX = origRot.x;
		rotY = origRot.y;
	}

	private void Update()
	{
		if (!playerInteractionWithObjects.IsMobileInput())
		{
			float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
			float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

			yRotation += mouseX;

			xRotation -= mouseY;
			xRotation = Mathf.Clamp(xRotation, -90f, 90f);

			transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
		}
		else
		{
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Began)
				{
					initTouch = touch;
				}
				else if (touch.phase == TouchPhase.Moved)
				{
					float deltaX = initTouch.position.x - touch.position.x;
					//float deltaY = initTouch.position.y - touch.position.y;

					//rotX -= deltaY * rotSpeed * Time.deltaTime * -1;
					rotY += deltaX * Time.deltaTime * rotSpeed * -1;
					rotY = Mathf.Clamp(rotY, 0, 40f);

					cam.transform.eulerAngles = new Vector3(0f, rotY, 0f);
				}
				else if (touch.phase == TouchPhase.Ended)
				{
					initTouch = new Touch();
				}
			}
		}
	}

	private void OnEnable()
	{
		if (playerInteractionWithObjects.IsMobileInput())
		{
			touchPad.SetActive(false);
		}
	}

	private void OnDisable()
	{
		if (playerInteractionWithObjects.IsMobileInput())
		{
			touchPad.SetActive(true);
		}
	}
}
