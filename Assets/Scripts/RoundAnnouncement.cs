using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundAnnouncement : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform icon;
    [SerializeField] private RectTransform banner;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private Image winnerIcon;
    [SerializeField] private Sprite player1Winner;
    [SerializeField] private Sprite player2Winner;
    [SerializeField] private Sprite defaultIcon;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float holdTime = 1.4f;
    [SerializeField] private float fadeOutTime = 0.4f;

    private float timer = 0f;
    private bool showing = false;

    public bool IsShowing => showing;

    private void Awake()
    {
        roundText.enableAutoSizing = true;
        roundText.fontSizeMax = roundText.fontSize;
        roundText.fontSizeMin = roundText.fontSize * 0.45f;

        if (!showing)
        {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    public void Show(int round, bool finalRound)
    {
        ShowMessage(finalRound ? "Final Round" : "Round " + round);
    }

    public void ShowMessage(string message, float delay = 0f)
    {
        roundText.text = message;
        winnerIcon.sprite = GetWinnerIcon(message);

        timer = -delay;
        showing = true;
        gameObject.SetActive(true);
        Animate();
    }

    // The icon that belongs to the player named in the text (also used by the victory screen).
    public Sprite GetWinnerIcon(string text)
    {
        if (text.Contains("Player 1", StringComparison.OrdinalIgnoreCase))
        {
            return player1Winner;
        }
        if (text.Contains("Player 2", StringComparison.OrdinalIgnoreCase))
        {
            return player2Winner;
        }
        return defaultIcon;
    }

    private void Update()
    {
        if (!showing) return;

        timer += Time.deltaTime;
        if (timer >= fadeInTime + holdTime + fadeOutTime)
        {
            showing = false;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            return;
        }

        Animate();
    }

    private void Animate()
    {
        float fadeOutStart = fadeInTime + holdTime;
        if (timer < fadeInTime)
        {
            canvasGroup.alpha = timer / fadeInTime;
        }
        else if (timer > fadeOutStart)
        {
            canvasGroup.alpha = 1f - (timer - fadeOutStart) / fadeOutTime;
        }
        else
        {
            canvasGroup.alpha = 1f;
        }

        float iconProgress = Mathf.Clamp01(timer / (fadeInTime * 1.4f));
        icon.localScale = Vector3.one * Mathf.LerpUnclamped(1.6f, 1f, EaseOutBack(iconProgress));

        float bannerProgress = Mathf.Clamp01((timer - fadeInTime * 0.5f) / 0.35f);
        banner.localScale = new Vector3(EaseOutBack(bannerProgress), 1f, 1f);

        float textProgress = Mathf.Clamp01((timer - fadeInTime) / 0.25f);
        roundText.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1.5f, 1f, EaseOutBack(textProgress));
        roundText.alpha = textProgress;
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
