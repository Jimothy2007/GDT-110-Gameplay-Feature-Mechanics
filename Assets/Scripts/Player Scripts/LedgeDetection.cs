using UnityEngine;

public class LedgeDetection : MonoBehaviour
{
    [SerializeField] private float ledgeDetectionDistance = 1f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private GameObject player;
    private Collider wallCollider;
    private bool isLedgeDetected = false;

    private void Start()
    {
        if (wallLayer == 0)
        {
            Debug.Log("Set the wallLayer for ledgeDetection.");
        }
        if (player == null)
        {
            player = gameObject.GetComponentInParent<GameObject>();
        }
    }

    private void FixedUpdate()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, ledgeDetectionDistance, wallLayer))
        {
            if (hit.collider.GetComponent<WallScript>().CanBeLedgeGrabbed())
            {
                Debug.DrawRay(transform.position, transform.forward * ledgeDetectionDistance, Color.red);
                wallCollider = hit.collider;
                isLedgeDetected = true;
            }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.forward * ledgeDetectionDistance, Color.green);
            wallCollider = null;
            isLedgeDetected = false;
        }

        player.GetComponent<PlayerController>().ledgeDetected = isLedgeDetected;
        player.GetComponent<PlayerController>().wallToGrab = wallCollider;
    }
}
