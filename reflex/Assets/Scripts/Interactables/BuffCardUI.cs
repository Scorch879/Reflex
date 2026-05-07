using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuffCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RewardManager manager;
    private BuffCardData cardData;

    public void Setup(BuffCardData data)
    {
        cardData = data;

        nameText.text = data.cardName;
        descriptionText.text = data.description;
    }

    public void ClearBuffText()
    {
        nameText.text = "";
        descriptionText.text = "";
    }

    public void OnCardClicked()
    {
        manager.SelectCard(cardData);
    }
}