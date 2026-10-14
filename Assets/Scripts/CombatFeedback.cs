using UnityEngine;

// Game-feel helpers for hits and parries: hit-stop (brief slow-motion), camera shake and effect spawning.
public class CombatFeedback : MonoBehaviour
{
    private static CombatFeedback instance;
    private float hitStopEndTime = -1f;

    private static CombatFeedback Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("CombatFeedback").AddComponent<CombatFeedback>();
            }
            return instance;
        }
    }

    public static void HitStop(float duration, float timeScale = 0.05f)
    {
        CombatFeedback feedback = Instance;
        feedback.hitStopEndTime = Mathf.Max(feedback.hitStopEndTime, Time.unscaledTime + duration);
        Time.timeScale = timeScale;
    }

    public static void Shake(float amplitude, float duration)
    {
        CameraController.Shake(amplitude, duration);
    }

    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab != null)
        {
            Instantiate(prefab, position, rotation);
        }
    }

    private void Update()
    {
        if (hitStopEndTime > 0f && Time.unscaledTime >= hitStopEndTime)
        {
            hitStopEndTime = -1f;
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        if (hitStopEndTime > 0f) Time.timeScale = 1f;
        if (instance == this) instance = null;
    }
}
