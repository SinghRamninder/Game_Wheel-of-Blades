using UnityEngine;
using UnityEngine.SceneManagement;

// Main menu music. It survives the switch to the game scene and fades out smoothly there, then removes itself.
[RequireComponent(typeof(AudioSource))]
public class MenuMusic : MonoBehaviour
{
    [SerializeField] private float fadeOutTime = 2f;

    private static MenuMusic current;

    private AudioSource source;
    private string menuSceneName;
    private float startVolume;
    private bool fadingOut = false;

    private void Awake()
    {
        // Coming back to the menu while an older copy is still fading: the new one takes over.
        if (current != null && current != this)
        {
            Destroy(current.gameObject);
        }
        current = this;

        source = GetComponent<AudioSource>();
        startVolume = source.volume;
        menuSceneName = gameObject.scene.name;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (current == this) current = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != menuSceneName)
        {
            fadingOut = true;
        }
    }

    private void Update()
    {
        if (!fadingOut) return;

        // Unscaled so the game's hit-stop slow motion can't stretch the fade.
        source.volume = Mathf.MoveTowards(source.volume, 0f, startVolume / fadeOutTime * Time.unscaledDeltaTime);
        if (source.volume <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
