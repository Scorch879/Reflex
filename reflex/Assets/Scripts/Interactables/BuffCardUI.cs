using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuffCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    private BuffCardData cardData;
    private RewardManager manager;

    public void Setup(BuffCardData data, RewardManager rewardManager)
    {
        cardData = data;
        manager = rewardManager;

        nameText.text = data.cardName;
        descriptionText.text = data.description;

        GetComponent<Button>().onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        manager.SelectCard(cardData);
    }
}