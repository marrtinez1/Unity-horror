using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementStandalone : MonoBehaviour
{
	[SerializeField] float speed = 3f;

	[SerializeField] CharacterController controller;

	Vector3 velocity;

	private void Start()
	{
		velocity.y = -9.81f;
	}

	// Update is called once per frame
	void Update()
	{
		//Pohyb hraca
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		Vector3 move = transform.right * x + transform.forward * z;

		if (move != Vector3.zero)
		{
			controller.Move(move * speed * Time.deltaTime);
		}

		controller.Move(velocity * Time.deltaTime);
	}

	public void SetSpeed(float pSpeed)
	{
		speed = pSpeed;
	}
}
