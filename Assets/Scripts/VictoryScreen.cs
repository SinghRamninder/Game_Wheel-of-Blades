using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform medallion;
    [SerializeField] private Image winnerIcon;
    [SerializeField] private RectTransform glow;
    [SerializeField] private RectTransform banner;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private CanvasGroup mainMenuButton;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip winnerMusic;
    [SerializeField] private float musicVolume = 0.6f;

    [Header("Scene")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private float timer = 0f;
    private bool showing = false;

    private void Awake()
    {
        if (!showing)
        {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    public void Show(string winnerName, Sprite icon)
    {
        winnerText.text = winnerName + " Wins the Match!";
        winnerIcon.sprite = icon;
        timer = 0f;
        showing = true;
        gameObject.SetActive(true);

        musicSource.clip = winnerMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();

        Animate();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void Update()
    {
        if (!showing) return;

        timer += Time.unscaledDeltaTime;
        Animate();
    }

    private void Animate()
    {
        canvasGroup.alpha = Mathf.Clamp01(timer / 0.5f);

        float pop = Mathf.Clamp01((timer - 0.1f) / 0.6f);
        medallion.localScale = Vector3.one * Mathf.LerpUnclamped(1.8f, 1f, EaseOutBack(pop));

        glow.localRotation = Quaternion.Euler(0f, 0f, -timer * 12f);
        glow.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(timer * 2f)) * Mathf.Clamp01(timer / 0.8f);

        float unroll = Mathf.Clamp01((timer - 0.35f) / 0.4f);
        banner.localScale = new Vector3(EaseOutBack(unroll), 1f, 1f);

        float text = Mathf.Clamp01((timer - 0.6f) / 0.3f);
        winnerText.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1.5f, 1f, EaseOutBack(text));
        winnerText.alpha = text;

        float button = Mathf.Clamp01((timer - 1.1f) / 0.4f);
        mainMenuButton.alpha = button;
        mainMenuButton.interactable = button >= 1f;
        mainMenuButton.blocksRaycasts = button >= 1f;
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
