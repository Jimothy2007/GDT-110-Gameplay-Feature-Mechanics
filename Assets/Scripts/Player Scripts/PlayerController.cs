using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float wallRunGravity = 0f;
    [SerializeField] private float wallRunGravityIncrease = 2f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Transform cameraTransform;

    private bool isRotationLocked = false;
    private bool isOnWall = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        //Debug.Log($"Move Input: {moveInput}");
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        //Debug.Log($"Jumping: {context.performed} - isGrounded: {controller.isGrounded}");
        if (context.performed && controller.isGrounded)
        {
            //Debug.Log("We are supposed to jump");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }
        else if (context.performed && isOnWall)
        {
            //Debug.Log("We are supposed to jump off the wall");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            WallRunning(false);
        }
    }

    void Update()
    {
        // Apply gravity and reset vertical velocity when grounded
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 moveDirection = Vector3.zero;

        // Calculate movement direction based on input and camera orientation and rotate the player towards the movement direction
        if ((moveInput != Vector2.zero) && (!isRotationLocked))
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        else if ((moveInput != Vector2.zero) && (isRotationLocked))
        {
            // If the player is on the wall, lock rotation and move in the direction of the wall
            moveDirection = transform.forward;
        }
        else if ((moveInput == Vector2.zero) && (isRotationLocked))
        {
            WallRunning(false);
        }

        // Move the player
        controller.Move(moveDirection * Time.deltaTime * moveSpeed);

        //Apply gravity if the player is not on the wall
        if (!isOnWall)
        {
            velocity.y += gravityValue * Time.deltaTime;
        }
        else if (isOnWall)
        {
            wallRunGravity += wallRunGravityIncrease * Time.deltaTime;
            wallRunGravity = Mathf.Clamp(wallRunGravity, 0f, Mathf.Abs(gravityValue));
            velocity.y -= wallRunGravity * Time.deltaTime;
        }

        // Move the player based on the calculated velocity
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("RunnableWall"))
        {
            Vector3 direction1 = other.transform.GetChild(0).transform.position - transform.position;
            Vector3 direction2 = other.transform.GetChild(1).transform.position - transform.position;

            direction1.y = 0f; // Ignore vertical component for wall running direction
            direction2.y = 0f; // Ignore vertical component for wall running direction

            direction1 = Vector3.Normalize(direction1);
            direction2 = Vector3.Normalize(direction2);

            float dot1 = Mathf.Abs(Vector3.Dot(direction1, transform.forward));
            float dot2 = Mathf.Abs(Vector3.Dot(direction2, transform.forward));

            if (dot1 > 0.5f || dot2 > 0.5f) // Adjust the threshold as needed
            {
                Debug.Log("Player is facing the wall and can start wall running.");
                WallRunning(true, other.gameObject);
            }
            else
            {
                Debug.Log("Player is not facing the wall enough to start wall running.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("RunnableWall"))
        {
            WallRunning(false);
        }
    }

    private void WallRunning(bool isRunning)
    {
        isOnWall = isRunning;
        isRotationLocked = isRunning;
    }

    private void WallRunning(bool isRunning, GameObject wallTrigger)
    {
        isOnWall = isRunning;
        isRotationLocked = isRunning;

        wallRunGravity = 0f; // Reset wall run gravity when starting or stopping wall running

        Vector3 wallRunDirection = GetWallRunDirection(wallTrigger);

        if (isRunning)
        {
            velocity.y = 0f; // Reset vertical velocity when starting wall running
            transform.rotation = Quaternion.LookRotation(wallRunDirection);
        }
    }

    private Vector3 GetWallRunDirection(GameObject wallTrigger)
    {
        Vector3 direction1 = wallTrigger.transform.GetChild(0).transform.position - transform.position;
        Vector3 direction2 = wallTrigger.transform.GetChild(1).transform.position - transform.position;

        direction1.y = 0f; // Ignore vertical component for wall running direction
        direction2.y = 0f; // Ignore vertical component for wall running direction

        direction1 = Vector3.Normalize(direction1);
        direction2 = Vector3.Normalize(direction2);

        // Calculate the direction to run along the wall based on the wall's position and the player's position
        if (Vector3.Dot(direction1, transform.forward) > Vector3.Dot(direction2, transform.forward))
        {
            return direction1;
        }
        else
        {
            return direction2;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
    }
}
