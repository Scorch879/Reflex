using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("Health Bar References")]
    [SerializeField] private Image greenHPBarFill;
    [SerializeField] private Image redHPBarFill;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("HP Bar Settings")]
    [SerializeField] private float redLerpSpeed = 5f; // Speed of the health bar animation
    [SerializeField] private float greenLerpSpeed = 5f; // Speed of the health bar animation

    [Header("Canvas References")]
    [SerializeField] private CanvasGroup inGameUICanvasGroup;
    [SerializeField] private CanvasGroup PauseUICanvasGroup;

    [Header("Status Messaging")]
    [SerializeField] private TextMeshProUGUI statusMessageText;
    [SerializeField] private CanvasGroup statusMessageCanvasGroup;
    [SerializeField] private float statusMessageFadeInDuration = 0.1f;
    [SerializeField] private float statusMessageHoldDuration = 1.8f;
    [SerializeField] private float statusMessageFadeOutDuration = 0.35f;


    private Coroutine healthAnimationCoroutine;
    private Coroutine statusMessageCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Centralized public method to safely update health. 
    /// Call this directly from external scripts instead of starting a coroutine there.
    /// </summary>
    public void UpdateHealth(float currentHp, float maxHp)
    {
        UpdateHPText(currentHp, maxHp);

        // Stop any currently running health bar animations to prevent overlapping race conditions
        if (healthAnimationCoroutine != null)
        {
            StopCoroutine(healthAnimationCoroutine);
        }

        // Start the managed animation routine on this persistent UI manager
        healthAnimationCoroutine = StartCoroutine(HealthBarRoutine(currentHp, maxHp));
    }

    public void SetHealthImmediate(float currentHp, float maxHp)
    {
        float safeMaxHp = Mathf.Max(0.01f, maxHp);
        float targetFill = Mathf.Clamp01(currentHp / safeMaxHp);

        if (healthAnimationCoroutine != null)
        {
            StopCoroutine(healthAnimationCoroutine);
            healthAnimationCoroutine = null;
        }

        if (greenHPBarFill != null)
        {
            greenHPBarFill.fillAmount = targetFill;
        }

        if (redHPBarFill != null)
        {
            redHPBarFill.fillAmount = targetFill;
        }

        UpdateHPText(currentHp, safeMaxHp);
    }

    private IEnumerator HealthBarRoutine(float currentHp, float maxHp)
    {
        float targetFill = Mathf.Clamp01(currentHp / maxHp);

        // 1. Smoothly transition the green bar to the target fill
        // Using Time.unscaledDeltaTime ensures it moves even if the game pauses/slows down on death
        while (Mathf.Abs(greenHPBarFill.fillAmount - targetFill) > 0.001f)
        {
            greenHPBarFill.fillAmount = Mathf.Lerp(greenHPBarFill.fillAmount, targetFill, Time.unscaledDeltaTime * greenLerpSpeed);
            yield return null;
        }
        greenHPBarFill.fillAmount = targetFill;

        // If health drops to 0, skip the 1.5s delay so the red bar drains immediately 
        if (targetFill > 0f)
        {
            // Wait 1.5 seconds using unscaled real time before the red bar catches up
            yield return new WaitForSecondsRealtime(1.5f);
        }

        // 2. Smoothly transition the red bar to match the target fill
        while (Mathf.Abs(redHPBarFill.fillAmount - targetFill) > 0.001f)
        {
            redHPBarFill.fillAmount = Mathf.MoveTowards(redHPBarFill.fillAmount, targetFill, redLerpSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        redHPBarFill.fillAmount = targetFill;
    }

    public void UpdateHPText(float currentHp, float maxHp)
    {
        if (hpText == null) return;
        // Clamp currentHp to 0 so it doesn't display negative values if overkill damage happens
        hpText.text = $"{Mathf.RoundToInt(Mathf.Max(0, currentHp))}/{Mathf.RoundToInt(maxHp)}";
    }

    public void ShowPauseUI()
    {
        inGameUICanvasGroup.alpha = 0f;
        inGameUICanvasGroup.interactable = false;
        inGameUICanvasGroup.blocksRaycasts = false;

        PauseUICanvasGroup.alpha = 1f;
        PauseUICanvasGroup.interactable = true;
        PauseUICanvasGroup.blocksRaycasts = true;
    }

    public void HidePauseUI()
    {
        inGameUICanvasGroup.alpha = 1f;
        inGameUICanvasGroup.interactable = true;
        inGameUICanvasGroup.blocksRaycasts = true;

        PauseUICanvasGroup.alpha = 0f;
        PauseUICanvasGroup.interactable = false;
        PauseUICanvasGroup.blocksRaycasts = false;
    }

    public void ShowStatusMessage(string message, Color color)
    {
        if (statusMessageText == null || statusMessageCanvasGroup == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (statusMessageCoroutine != null)
        {
            StopCoroutine(statusMessageCoroutine);
        }

        statusMessageCoroutine = StartCoroutine(StatusMessageRoutine(message, color));
    }

    private IEnumerator StatusMessageRoutine(string message, Color color)
    {
        statusMessageText.text = message;
        statusMessageText.color = color;

        float fadeInDuration = Mathf.Max(0.01f, statusMessageFadeInDuration);
        float holdDuration = Mathf.Max(0f, statusMessageHoldDuration);
        float fadeOutDuration = Mathf.Max(0.01f, statusMessageFadeOutDuration);

        statusMessageCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            statusMessageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        statusMessageCanvasGroup.alpha = 1f;
        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            statusMessageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        statusMessageCanvasGroup.alpha = 0f;
        statusMessageCoroutine = null;
    }
}
