using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float airControlMultiplier = 0.5f;
    [SerializeField] private float groundAcceleration = 20f;
    [SerializeField] private float airAcceleration = 5f;
    [SerializeField] private float ledgeAcceleration = 10f;
    [Header("Wall run settings")]
    [SerializeField] private float wallRunSpeed = 5f;
    [SerializeField] private float wallRunGravity = 0f;
    [SerializeField] private float wallRunGravityIncrease = 2f;
    [SerializeField] private float pushOffForce = 5f;
    [SerializeField] private float pushForwardForce = 2f;
    [Header("Ledge grab settings")]
    [SerializeField] private float ledgeGrabSpeed = 2f;

    private Vector3 wallNormal;

    private float turnSmoothVelocity;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Vector3 moveVelocity;
    private Transform cameraTransform;

    private bool isRotationLocked = false;
    private bool isOnWall = false;
    private bool isLedgeGrabbing = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log($"Move Input: {moveInput}");
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && controller.isGrounded)
        {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }
        else if (context.started && isOnWall)
        {
            Vector3 runDirection = moveVelocity;
            runDirection.y = 0f; // Ignore vertical component for wall jump direction

            if (runDirection.magnitude > 0.01f)
            {
                runDirection.Normalize();
            }
            else
            {
                runDirection = transform.forward; // Default to forward if no movement input
            }

            moveVelocity += wallNormal * pushOffForce; // Push away from the wall

            moveVelocity += runDirection * pushForwardForce; // Add forward momentum

            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);

            WallRunning(false);
        }

        if (context.canceled && velocity.y > 0)
        {
            velocity.y *= 0.5f; // Reduce upward velocity when jump is released early
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

        float currentMoveSpeed;

        if (isOnWall)
        {
            currentMoveSpeed = wallRunSpeed;
        }
        else if (isLedgeGrabbing)
        {
            currentMoveSpeed = ledgeGrabSpeed;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
        }

        if (controller.isGrounded)
        {
            Vector3 targetVelocity = moveDirection * currentMoveSpeed;

            moveVelocity = Vector3.MoveTowards(moveVelocity, targetVelocity, groundAcceleration * Time.deltaTime);
        }
        else if (moveDirection != Vector3.zero && !controller.isGrounded)
        {
            Vector3 targetVelocity = moveDirection * currentMoveSpeed;

            moveVelocity = Vector3.MoveTowards(moveVelocity, targetVelocity, airAcceleration * Time.deltaTime);
        }
        else if (moveDirection != Vector3.zero && isLedgeGrabbing)
        {
            if (moveInput.y > 0)
            {
                // Allow the player to move up while ledge grabbing
            }
            else if (moveInput.y < 0)
            {
                LedgeGrabbing(false); // Release ledge grab if the player is trying to move down
            }
            else
            {
                Vector3 targetVelocity = moveDirection * currentMoveSpeed;
                moveVelocity = Vector3.MoveTowards(moveVelocity, targetVelocity, ledgeAcceleration * Time.deltaTime);
            }
        }

            // Move the player
            controller.Move(moveVelocity * Time.deltaTime);

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
        else if (isLedgeGrabbing)
        {
            velocity.y = 0f; // Reset vertical velocity when ledge grabbing
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
        else if (other.gameObject.CompareTag("Ledge"))
        {
            Debug.Log("Player is in range of a ledge and can start ledge grabbing.");
            LedgeGrabbing(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("RunnableWall"))
        {
            WallRunning(false);
        }
        else if (other.gameObject.CompareTag("Ledge"))
        {
            LedgeGrabbing(false);
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

        if (isRunning)
        {
            Vector3 closestPoint = wallTrigger.GetComponent<Collider>().ClosestPoint(transform.position);
            wallNormal = transform.position - closestPoint;
            wallNormal.y = 0f; // Ignore vertical component for wall normal
            wallNormal.Normalize();

            Vector3 wallRunDirection = GetWallRunDirection(wallTrigger);

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

    private void LedgeGrabbing(bool isGrabbing)
    {
        isLedgeGrabbing = isGrabbing;
        isRotationLocked = isGrabbing;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
    }
}
