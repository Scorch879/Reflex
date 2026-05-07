using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;

public class RewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CanvasGroup rewardCanvasGroup; // For fading in/out the reward UI
    [SerializeField] private BuffCardUI[] cardUI; // 3 UI slots for the buff cards
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Card Pool")]
    [SerializeField] private BuffCardData[] allAvailableCards;

    void Update()
    {
        // For testing: Press 'K' to simulate clearing a floor
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            OpenRewardScreen();
        }
    }

    public void OpenRewardScreen()
    {
        StartCoroutine(FadeInUI());

        foreach (var card in cardUI)
        {
            card.ClearBuffText();
        }

        // Pick 3 unique random cards
        var choices = allAvailableCards.OrderBy(x => Random.value).Take(3).ToList();

        // assign each card to a socket
        for (int i = 0; i < choices.Count; i++)
        {
            cardUI[i].Setup(choices[i]);
        }
    }

    private IEnumerator FadeInUI()
    {
        float duration = fadeInDuration;
        float elapsed = 0f;

        rewardCanvasGroup.interactable = true;
        rewardCanvasGroup.blocksRaycasts = true;

        while (elapsed < duration)
        {
            rewardCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            Time.timeScale = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rewardCanvasGroup.alpha = 1f;
        Time.timeScale = 0f;
    }

    private IEnumerator FadeOutUI()
    {
        float duration = fadeOutDuration;
        float elapsed = 0f;

        rewardCanvasGroup.interactable = false;
        rewardCanvasGroup.blocksRaycasts = false;

        Time.timeScale = 1f;

        while (elapsed < duration)
        {
            rewardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rewardCanvasGroup.alpha = 0f;
    }

    public void SelectCard(BuffCardData card)
    {
        // Apply the additive bonuses to PlayerManager
        playerManager.cardAtkBonus += card.atkBonus;
        playerManager.cardCritChance += card.critBonus;
        playerManager.cardEssenceMult += card.essenceBonus;
        playerManager.cardVampChance += card.vampiricBonus;
        playerManager.cardComboWindowBonus += card.comboWindowBonus;

        // Fleet Foot (Dash) bonuses[cite: 1]
        playerManager.cardDashCDReduction += card.dashCDReduction;
        playerManager.cardDashDistanceBonus += card.dashDistanceBonus;

        if (card.isGlassCannon) playerManager.ApplyGlassCannon();

        StartCoroutine(FadeOutUI());
    }
}