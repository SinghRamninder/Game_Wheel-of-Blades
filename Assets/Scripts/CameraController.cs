using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject wheel;

    private static float shakeAmplitude;
    private static float shakeDuration;
    private static float shakeEndTime;

    private float baseY;
    private float baseZ;

    private void Start()
    {
        baseY = transform.position.y;
        baseZ = transform.position.z;
    }

    public static void Shake(float amplitude, float duration)
    {
        shakeAmplitude = Mathf.Max(amplitude, Time.unscaledTime < shakeEndTime ? shakeAmplitude : 0f);
        shakeDuration = duration;
        shakeEndTime = Time.unscaledTime + duration;
    }

    private void LateUpdate()
    {
        Vector3 shake = Vector3.zero;
        if (Time.unscaledTime < shakeEndTime)
        {
            float strength = (shakeEndTime - Time.unscaledTime) / shakeDuration;
            shake = (Vector3)(Random.insideUnitCircle * shakeAmplitude * strength);
        }

        transform.position = new Vector3(wheel.transform.position.x, baseY, baseZ) + shake;
    }
}
