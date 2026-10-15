using UnityEngine;

public class FollowCameraX : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float snapStep = 0f;

    private float offsetX;

    private void Start()
    {
        if (target == null)
        {
            target = Camera.main.transform;
        }
        offsetX = transform.position.x - target.position.x;
    }

    private void LateUpdate()
    {
        float x = target.position.x + offsetX;
        if (snapStep > 0f)
        {
            x = Mathf.Round(x / snapStep) * snapStep;
        }
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
