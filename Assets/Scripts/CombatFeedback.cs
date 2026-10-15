using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    private static CombatFeedback instance;
    private float hitStopEndTime = -1f;

    private const int VoiceCount = 8;
    private AudioSource[] voices;
    private int nextVoice = 0;

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

    public static void PlaySound(AudioClip[] clips, float volume, float pitch = 1f)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        Instance.PlayVoice(clip, volume, pitch * Random.Range(0.94f, 1.06f));
    }

    private void PlayVoice(AudioClip clip, float volume, float pitch)
    {
        if (voices == null)
        {
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
        }

        AudioSource source = voices[nextVoice];
        nextVoice = (nextVoice + 1) % VoiceCount;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();
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
