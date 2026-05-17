using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject upgradePanel;

    [Header("Health UI")]
    public TextMeshProUGUI healthLevelText;
    public TextMeshProUGUI healthCostText;
    public Button healthUpgradeButton;

    [Header("Damage UI")]
    public TextMeshProUGUI damageLevelText;
    public TextMeshProUGUI damageCostText;
    public Button damageUpgradeButton;

    [Header("Crit UI")]
    public TextMeshProUGUI critLevelText;
    public TextMeshProUGUI critCostText;
    public Button critUpgradeButton;

    private PlayerManager player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Ensure it starts hidden
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Assign button listeners
        healthUpgradeButton.onClick.AddListener(OnUpgradeHealthClicked);
        damageUpgradeButton.onClick.AddListener(OnUpgradeDamageClicked);
        critUpgradeButton.onClick.AddListener(OnUpgradeCritClicked);
    }

    public void OpenUI(PlayerManager playerManager)
    {
        player = playerManager;
        upgradePanel.SetActive(true);
        RefreshUI();

        // Optional: lock player movement/attack while UI is open
        if (player != null)
        {
            player.canAttack = false;
        }
    }

    public void CloseUI()
    {
        upgradePanel.SetActive(false);

        // Optional: unlock player
        if (player != null)
        {
            player.canAttack = true;
        }
    }

    public void RefreshUI()
    {
        if (UpgradeManager.Instance == null || UpgradeManager.Instance.settings == null) return;
        if (SaveManager.Instance == null) return;

        SaveData data = SaveManager.Instance.currentSave;
        UpgradeSettings settings = UpgradeManager.Instance.settings;

        // --- Health ---
        healthLevelText.text = $"Lv {data.healthUpgradeLevel} / {settings.maxUpgradeLevel}";
        if (data.healthUpgradeLevel >= settings.maxUpgradeLevel)
        {
            healthCostText.text = "MAX";
            healthUpgradeButton.interactable = false;
        }
        else
        {
            int cost = settings.GetUpgradeCost(data.healthUpgradeLevel);
            healthCostText.text = $"{cost} Essence";
            healthUpgradeButton.interactable = data.soulEssence >= cost;
        }

        // --- Damage ---
        damageLevelText.text = $"Lv {data.damageUpgradeLevel} / {settings.maxUpgradeLevel}";
        if (data.damageUpgradeLevel >= settings.maxUpgradeLevel)
        {
            damageCostText.text = "MAX";
            damageUpgradeButton.interactable = false;
        }
        else
        {
            int cost = settings.GetUpgradeCost(data.damageUpgradeLevel);
            damageCostText.text = $"{cost} Essence";
            damageUpgradeButton.interactable = data.soulEssence >= cost;
        }

        // --- Crit ---
        critLevelText.text = $"Lv {data.critUpgradeLevel} / {settings.maxUpgradeLevel}";
        if (data.critUpgradeLevel >= settings.maxUpgradeLevel)
        {
            critCostText.text = "MAX";
            critUpgradeButton.interactable = false;
        }
        else
        {
            int cost = settings.GetUpgradeCost(data.critUpgradeLevel);
            critCostText.text = $"{cost} Essence";
            critUpgradeButton.interactable = data.soulEssence >= cost;
        }
    }

    private void OnUpgradeHealthClicked()
    {
        if (UpgradeManager.Instance.TryUpgradeHealth())
        {
            RefreshAll();
        }
    }

    private void OnUpgradeDamageClicked()
    {
        if (UpgradeManager.Instance.TryUpgradeDamage())
        {
            RefreshAll();
        }
    }

    private void OnUpgradeCritClicked()
    {
        if (UpgradeManager.Instance.TryUpgradeCrit())
        {
            RefreshAll();
        }
    }

    private void RefreshAll()
    {
        RefreshUI();

        // Find and update the HUD so the top right updates immediately when spending points
        SoulEssenceHUD hud = FindFirstObjectByType<SoulEssenceHUD>();
        if (hud != null)
        {
            hud.RefreshHUD();
        }
    }
}