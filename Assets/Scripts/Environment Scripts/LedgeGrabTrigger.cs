using UnityEngine;

public class LedgeGrabTrigger : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>().bounds.size);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.GetChild(0).position, 0.1f);
        Gizmos.DrawSphere(transform.GetChild(1).position, 0.1f);
    }
}
