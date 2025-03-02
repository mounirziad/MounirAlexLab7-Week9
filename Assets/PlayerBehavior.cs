using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerBehavior : MonoBehaviour
{
    public float speed = 5f;
    public float jumpHeight = 0.5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Check if the player is on the ground
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            controller = GetComponent<CharacterController>();
            velocity.y = -2f; // Small negative value to keep the player grounded
        }

        // Get input for movement using KeyCode
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.S))
        {
            move += transform.forward;
        }
        if (Input.GetKey(KeyCode.W))
        {
            move -= transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            move -= transform.right;
        }
        if (Input.GetKey(KeyCode.A))
        {
            move += transform.right;
        }

        // Normalize the movement vector to maintain consistent speed
        if (move.magnitude > 1)
        {
            move.Normalize();
        }

        controller.Move(move * speed * Time.deltaTime);

        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}