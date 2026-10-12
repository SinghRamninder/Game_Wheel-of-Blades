using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject wheel;

    private void LateUpdate()
    {
        transform.position = new Vector3(wheel.transform.position.x, transform.position.y, transform.position.z);
    }
}
