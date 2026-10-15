using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject wheel;

    private static float shakeAmplitude;
    private static float shakeDuration;
    private static float shakeEndTime;

    private float baseY;
    private float baseZ;
    private float shakeOffsetX;

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

        // Scenes without a wheel (e.g. the main menu) keep the camera where it is.
        float x = wheel != null ? wheel.transform.position.x : transform.position.x - shakeOffsetX;
        shakeOffsetX = shake.x;
        transform.position = new Vector3(x, baseY, baseZ) + shake;
    }
}
