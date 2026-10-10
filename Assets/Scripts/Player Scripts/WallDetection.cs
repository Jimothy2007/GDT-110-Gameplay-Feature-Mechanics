using UnityEngine;

public class WallDetection : MonoBehaviour
{ 
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private GameObject player;
    private bool isWallDetected = false;

    private void Start()
    {
        if (wallLayer == 0)
        {
            Debug.LogWarning("Wall layer is not set. Please assign a wall layer in the inspector.");
        }
        if (player == null)
        {
            player = gameObject.GetComponentInParent<GameObject>();
        }
    }

    private void Update()
    {
        player.GetComponent<PlayerController>().wallDetected = isWallDetected;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (wallLayer.Equals("Wall"))
        {
            isWallDetected = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (wallLayer.Equals("Wall"))
        {
            isWallDetected = false;
        }
    }
}
