using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionWithObjects : MonoBehaviour
{
	[SerializeField] Camera mainCamera;
	[SerializeField] GameObject cameraAsGO;
	GameObject hidingCamera;

	PlayerMovementStandalone playerMovementStandalone;
	EnemyAI zombie;

	[SerializeField] LayerMask layerMask;
	[SerializeField] Transform itemHolder;
	[SerializeField] GameObject meleeZone;
	Transform holdingItem;

	[SerializeField] GameObject HUDCrosshairInteraction;
	[SerializeField] GameObject HUDInteraction;
	[SerializeField] GameObject HUDHideControll;
	[SerializeField] GameObject HUDCrouchControl;
	[SerializeField] GameObject HUDDropItem;

	[SerializeField] GameObject imageHide;
	[SerializeField] GameObject imageUnhide;

	[SerializeField] GameObject ImageCrouch;
	[SerializeField] GameObject ImageStand;

	[SerializeField] bool mobileInput;

	RaycastHit hit;
	bool inTrigger;
	bool isHiding;
	bool isCrouching;
	bool isItemEquipted;

	// Start is called before the first frame update
	void Start()
    {
		playerMovementStandalone = GetComponent<PlayerMovementStandalone>();
		zombie = FindObjectOfType<EnemyAI>();
	}

    // Update is called once per frame
    void Update()
	{
		ProcessInteraction();
		HideInput();
		CrouchInput();
		DropInput();
	}

	private void ProcessInteraction()
	{
		Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

		if (Physics.Raycast(ray, out hit, 2.5f, layerMask))
		{
			HUDCrosshairInteraction.SetActive(true);
			HUDInteraction.SetActive(true);

			if (Input.GetKeyDown(KeyCode.E))
			{
				InteractWithObject();
			}
		}
		else
		{
			HUDCrosshairInteraction.SetActive(false);
			HUDInteraction.SetActive(false);
		}
	}

	public void InteractWithObject()
	{
		if (!isItemEquipted && hit.collider.CompareTag("Pickup"))
		{
			holdingItem = hit.transform;
			holdingItem.SetParent(itemHolder);
			holdingItem.GetComponent<Rigidbody>().isKinematic = true;

			holdingItem.localPosition = Vector3.zero;
			holdingItem.localRotation = Quaternion.Euler(Vector3.zero);
			holdingItem.localScale = new Vector3(0.1f, 0.1f, 0.1f);

			HUDDropItem.SetActive(true);
			isItemEquipted = true;
		}
		else
		{
			Animator animator = hit.collider.GetComponent<Animator>();
			if (animator != null)
			{
				bool isOpen = animator.GetBool("open");
				animator.SetBool("open", !isOpen);
			}
		}
	}

	private void HideInput()
	{
		if (inTrigger && Input.GetKeyDown(KeyCode.H))
		{
			Hide();
		}
	}

	public void Hide()
	{
		if (!mobileInput)
		{
			playerMovementStandalone.enabled = isHiding;
		}
		mainCamera.enabled = isHiding;
		hidingCamera.SetActive(!isHiding);

		HUDCrouchControl.SetActive(isHiding);
		HUDInteraction.SetActive(isHiding);

		imageHide.SetActive(isHiding);
		imageUnhide.SetActive(!isHiding);

		zombie.SetProvoke(false);
		meleeZone.SetActive(isHiding);

		isHiding = !isHiding;
	}

	private void CrouchInput()
	{
		if (Input.GetKeyDown(KeyCode.C))
		{
			Crouch();
		}
	}

	public void Crouch()
	{
		if (!isCrouching)
		{
			mainCamera.transform.localPosition = new Vector3(0, 0.35f, 0);
			playerMovementStandalone.SetSpeed(1.5f);
			ETCInput.SetAxisSensitivity("VerticalMovement", 1.5f);
			ETCInput.SetAxisSensitivity("HorizontalMovement", 1.5f);
		}
		else
		{
			mainCamera.transform.localPosition = new Vector3(0, 0.8f, 0);
			playerMovementStandalone.SetSpeed(3f);
			ETCInput.SetAxisSensitivity("VerticalMovement", 3f);
			ETCInput.SetAxisSensitivity("HorizontalMovement", 3f);
		}
		ImageCrouch.SetActive(isCrouching);
		ImageStand.SetActive(!isCrouching);
		isCrouching = !isCrouching;
	}

	private void DropInput()
	{
		if (Input.GetKeyDown(KeyCode.O) && isItemEquipted)
		{
			Drop();
		}
	}

	public void Drop()
	{
		if (holdingItem != null && !isHiding)
		{
			holdingItem.transform.SetParent(null);
			holdingItem.GetComponent<Rigidbody>().isKinematic = false;

			HUDDropItem.SetActive(false);
			isItemEquipted = false;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("HidingPlace"))
		{
			HUDHideControll.SetActive(true);
			inTrigger = true;

			hidingCamera = other.transform.Find("cameraHolder").gameObject;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("HidingPlace"))
		{
			HUDHideControll.SetActive(false);
			inTrigger = false;
		}
	}

	public bool IsMobileInput()
	{
		return mobileInput;
	}

	public bool GetHiding()
	{
		return isHiding;
	}
}
