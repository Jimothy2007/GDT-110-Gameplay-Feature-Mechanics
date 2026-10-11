using UnityEngine;

public class LedgeDetection : MonoBehaviour
{
    [SerializeField] private float ledgeDetectionRadius = 0.3f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private PlayerController player;
    private Collider wallCollider;
    private bool isLedgeDetected = false;
    private bool canDetect = true;

    private void Start()
    {
        if (wallLayer == 0)
        {
            wallLayer = LayerMask.GetMask("Walls");
        }
        if (player == null)
        {
            player = gameObject.GetComponentInParent<PlayerController>();
        }
    }

    private void Update()
    {
        if (canDetect)
        {
            Collider[] wallColliders = new Collider[1];
            int numDetected = Physics.OverlapSphereNonAlloc(transform.position, ledgeDetectionRadius, wallColliders, wallLayer);

            if (numDetected > 0 && wallColliders[0] != null)
            {
                if (wallColliders[0].gameObject.GetComponent<WallScript>().CanBeLedgeGrabbed())
                {
                    Debug.Log("Ledge detected");
                    player.ledgeDetected = true;
                    player.wallToGrab = wallColliders[0];
                    return;
                }
            }
            else if (numDetected <= 0)
            {
                player.ledgeDetected = false;
                player.wallToGrab = null;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Debug.Log("Ledge detection switched off");
            canDetect = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Debug.Log("Ledge detection switched on");
            canDetect = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, ledgeDetectionRadius);
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        Gizmos.DrawWireCube(transform.position + collider.center, collider.size);
    }
}
