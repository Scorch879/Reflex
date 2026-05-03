using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

public class RewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameObject rewardUIPanel;
    [SerializeField] private Transform cardContainer; // Where the cards will be spawned
    [SerializeField] private GameObject cardPrefab;    // A UI button prefab for the card

    [Header("Card Pool")]
    [SerializeField] private List<BuffCardData> allAvailableCards;

    void Update()
    {
        // For testing: Press 'K' to simulate clearing a floor
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            OpenRewardScreen();
        }
    }

    // Call this when the floor is cleared
    public void OpenRewardScreen()
    {
        rewardUIPanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game

        // Clear previous cards in the UI
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        // Pick 3 unique random cards
        List<BuffCardData> choices = allAvailableCards.OrderBy(x => Random.value).Take(3).ToList();

        foreach (BuffCardData card in choices)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            // We will create this helper script in Step 3
            cardObj.GetComponent<BuffCardUI>().Setup(card, this);
        }
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

        // Close UI and resume
        rewardUIPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}