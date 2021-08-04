using UnityEngine;
using UnityEngine.Events;


public class PlayerMovement : MonoBehaviour
{
	public PlayerController controller;
	public float runSpeed = 40f;
	float horizontalMove = 0f;
	bool jump = false;

	void Update()
    {
		horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

		if (Input.GetButtonDown("Jump"))
        {
			jump = true;
        }
    }
	void FixedUpdate()
    {
		controller.Move(horizontalMove * Time.deltaTime, false, jump);
		jump = false;

    }

}
	