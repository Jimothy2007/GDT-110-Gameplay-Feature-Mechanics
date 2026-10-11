using UnityEngine;

public class WallScript : MonoBehaviour
{
    [SerializeField] private bool canBeLedgeGrabbed;

    public bool CanBeLedgeGrabbed() { return canBeLedgeGrabbed; }
}
