using Unity.VisualScripting;
using UnityEngine;

public class LedgeDetection : MonoBehaviour
{
    [SerializeField] private float ledgeDetectionRadius = 0.3f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private PlayerController player;

    private bool canDetect = true;
    private bool ignoreUntilClear = false;

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
        if (player == null || !canDetect)  return;

        Collider[] hits = Physics.OverlapSphere(transform.position, ledgeDetectionRadius, wallLayer);

        Collider validWall = null;

        foreach (Collider hit in hits)
        {
            WallScript wall = hit.GetComponent<WallScript>();

            if (wall != null && wall.CanBeLedgeGrabbed())
            {
                validWall = hit;
                break;
            }
        }

        if (validWall == null)
        {
            player.ledgeDetected = false;
            player.wallToGrab = null;
            ignoreUntilClear = false;
            return;
        }

        if (ignoreUntilClear)
        {
            player.ledgeDetected = false;
            player.wallToGrab = null;
            return;
        }

        player.wallToGrab = validWall;
        player.ledgeDetected = true;
    }

    public void IgnoreUntilClear()
    {
        ignoreUntilClear = true;

        if (player != null)
        {
            player.ledgeDetected = false;
            player.wallToGrab = null;
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
