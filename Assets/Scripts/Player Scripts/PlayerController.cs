using Unity.VisualScripting;
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
    [SerializeField] private float wallAcceleration = 20f;
    [Header("Wall run settings")]
    [SerializeField] private float wallRunSpeed = 5f;
    [SerializeField] private float wallRunGravity = 0f;
    [SerializeField] private float wallRunGravityIncrease = 2f;
    [SerializeField] private float pushOffForce = 5f;
    [SerializeField] private float pushForwardForce = 2f;
    [Header("Ledge grab settings")]
    [SerializeField] private float ledgeMoveSpeed = 2f;

    private Vector3 wallNormal;

    private float turnSmoothVelocity;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Vector3 moveVelocity;
    private Transform cameraTransform;
    private LedgeDetection ledgeDetection;

    private bool isRotationLocked = false;
    private bool isOnWall = false;
    private bool isOnLedge = false;
    private bool climbRequested;
    private bool dropRequested;

    [HideInInspector] public bool ledgeDetected;
    [HideInInspector] public Collider wallToGrab;
    [HideInInspector] public bool wallDetected;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        ledgeDetection = GetComponentInChildren<LedgeDetection>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector3 newInput = context.ReadValue<Vector2>();


        if (isOnLedge)
        {
            if (moveInput.y <= 0.5f && newInput.y > 0.5f)
            {
                climbRequested = true;
            }
            if (moveInput.y >= -0.5f && newInput.y < -0.5f)
            {
                dropRequested = true;
            }
        }

        moveInput = newInput;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (isOnLedge)
        {
            Vector3 jumpDirection = wallNormal.normalized;

            LedgeGrabbing(false);

            moveVelocity = jumpDirection * pushOffForce;

            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);

            if (ledgeDetection != null)
            {
                ledgeDetection.IgnoreUntilClear();
            }

            return;
        }
        else if (isOnWall)
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

            return;
        }
        else if (controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        if (context.canceled && velocity.y > 0)
        {
            velocity.y *= 0.5f; // Reduce upward velocity when jump is released early
        }
    }

    void Update()
    {
        if (isOnLedge)
        {
            moveVelocity = Vector3.zero;
            velocity = Vector3.zero;

            MoveAlongLedge();

            return;
        }

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
        else if ((moveInput == Vector2.zero) && (isRotationLocked) && (isOnWall))
        {
            WallRunning(false);
        }

        float currentMoveSpeed;

        if (isOnWall)
        {
            currentMoveSpeed = wallRunSpeed;
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
        else if (moveDirection != Vector3.zero && isOnWall)
        {
            Vector3 targetVelocity = moveDirection * currentMoveSpeed;

            moveVelocity = Vector3.MoveTowards(moveVelocity, targetVelocity, wallAcceleration * Time.deltaTime);
        }
        else if (moveDirection != Vector3.zero && !controller.isGrounded)
        {
            Vector3 targetVelocity = moveDirection * currentMoveSpeed;

            moveVelocity = Vector3.MoveTowards(moveVelocity, targetVelocity, airAcceleration * Time.deltaTime);
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
        // Move the player based on the calculated velocity
        controller.Move(velocity * Time.deltaTime);

        if (wallDetected)
        {
            Debug.Log("Wall detected");
        }
        if (ledgeDetected && !isOnLedge)
        {
            LedgeGrabbing(true);
        }
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

    private void LedgeGrabbing(bool onLedge)
    {
        if (onLedge)
        {
            if (wallToGrab == null)
            {
                Debug.LogWarning("Wall to grab is null");
                return;
            }

            isOnLedge = true;
            isRotationLocked = true;

            moveVelocity = Vector3.zero;
            velocity = Vector3.zero;

            Vector3 closestPoint = wallToGrab.ClosestPoint(transform.position);
            wallNormal = transform.position - closestPoint;
            wallNormal.y = 0f; // Ignore vertical component for wall normal

            if (wallNormal.sqrMagnitude > 0.001f)
            {
                wallNormal.Normalize();

                transform.rotation = Quaternion.LookRotation(-wallNormal);
            }
        }
        else
        {
            isOnLedge = false;
            isRotationLocked = false;
        }
    }

    private void MoveAlongLedge()
    {
        if (wallToGrab == null)
        {
            LedgeGrabbing(false);
            return;
        }

        if (climbRequested)
        {
            climbRequested = false;

            //Play climbing animation and teleport the player up the ledge
            Debug.Log("Climb requested");
            return;
        }

        if (dropRequested)
        {
            dropRequested = false;
            LedgeGrabbing(false);

            if (ledgeDetection != null)
            {
                ledgeDetection.IgnoreUntilClear();
            }

            moveVelocity = Vector3.zero;
            velocity = Vector3.zero;

            return;
        }

        Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal).normalized;

        float horizontalInput = -moveInput.x;

        Vector3 shimmyDirection = wallRight * horizontalInput;

        controller.Move(shimmyDirection * ledgeMoveSpeed * Time.deltaTime);
    }
}
