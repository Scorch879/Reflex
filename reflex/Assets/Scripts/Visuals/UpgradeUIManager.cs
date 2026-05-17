using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject upgradePanel;

    [Header("Essence UI")]
    public TextMeshProUGUI soulEssenceText;

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

    [Header("Temporary Runtime UI")]
    [SerializeField] private bool enableTemporaryRuntimeUI = true;
    [SerializeField] private bool allowEscapeToClose = true;
    [SerializeField] private string temporaryTitle = "Upgrade Station";
    [SerializeField] private Rect temporaryWindowRect = new Rect(0f, 0f, 560f, 420f);

    private bool temporaryUiOpen;
    private PlayerManager player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<UpgradeUIManager>() != null)
        {
            return;
        }

        GameObject uiManagerObject = new GameObject("UpgradeUIManager");
        uiManagerObject.AddComponent<UpgradeUIManager>();
    }

    public bool IsOpen
    {
        get
        {
            bool panelOpen = upgradePanel != null && upgradePanel.activeSelf;
            return panelOpen || temporaryUiOpen;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        HidePanelImmediate();
    }

    private void Start()
    {
        BindButtonIfAvailable(healthUpgradeButton, OnUpgradeHealthClicked);
        BindButtonIfAvailable(damageUpgradeButton, OnUpgradeDamageClicked);
        BindButtonIfAvailable(critUpgradeButton, OnUpgradeCritClicked);
    }

    private void Update()
    {
        if (!allowEscapeToClose || !IsOpen || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseUI();
        }
    }

    public void OpenUI(PlayerManager playerManager)
    {
        player = playerManager;

        if (HasSceneUIBindings())
        {
            upgradePanel.SetActive(true);
        }
        else if (enableTemporaryRuntimeUI)
        {
            temporaryUiOpen = true;
        }
        else
        {
            Debug.LogWarning("Upgrade UI has no scene bindings and temporary UI is disabled.");
            return;
        }

        RefreshUI();

        // Keep attacks disabled while the upgrade UI is open.
        if (player != null)
        {
            player.canAttack = false;
        }
    }

    public void CloseUI()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        temporaryUiOpen = false;

        if (player != null)
        {
            player.canAttack = true;
        }
    }

    public void RefreshUI()
    {
        if (UpgradeManager.Instance == null || SaveManager.Instance == null || SaveManager.Instance.currentSave == null)
        {
            return;
        }

        SaveData data = SaveManager.Instance.currentSave;
        UpgradeManager upgrades = UpgradeManager.Instance;
        int maxLevel = upgrades.GetMaxUpgradeLevel();

        if (soulEssenceText != null)
        {
            soulEssenceText.text = "Essence: " + data.soulEssence;
        }

        RefreshUpgradeRow(
            data.healthUpgradeLevel,
            maxLevel,
            healthLevelText,
            healthCostText,
            healthUpgradeButton);

        RefreshUpgradeRow(
            data.damageUpgradeLevel,
            maxLevel,
            damageLevelText,
            damageCostText,
            damageUpgradeButton);

        RefreshUpgradeRow(
            data.critUpgradeLevel,
            maxLevel,
            critLevelText,
            critCostText,
            critUpgradeButton);
    }

    private void OnGUI()
    {
        if (!temporaryUiOpen || !enableTemporaryRuntimeUI)
        {
            return;
        }

        if (UpgradeManager.Instance == null || SaveManager.Instance == null || SaveManager.Instance.currentSave == null)
        {
            return;
        }

        const float margin = 24f;
        temporaryWindowRect.width = Mathf.Max(temporaryWindowRect.width, 520f);
        temporaryWindowRect.height = Mathf.Max(temporaryWindowRect.height, 360f);
        temporaryWindowRect.x = (Screen.width - temporaryWindowRect.width) * 0.5f;
        temporaryWindowRect.y = (Screen.height - temporaryWindowRect.height) * 0.5f;

        GUI.Box(temporaryWindowRect, temporaryTitle);

        Rect content = new Rect(
            temporaryWindowRect.x + margin,
            temporaryWindowRect.y + 38f,
            temporaryWindowRect.width - (margin * 2f),
            temporaryWindowRect.height - 52f);

        GUILayout.BeginArea(content);

        SaveData data = SaveManager.Instance.currentSave;
        UpgradeManager upgrades = UpgradeManager.Instance;

        GUILayout.Label("Soul Essence: " + data.soulEssence);
        GUILayout.Space(8f);

        DrawTemporaryUpgradeEntry("Health", data.healthUpgradeLevel, upgrades.GetMaxUpgradeLevel(), upgrades.TryUpgradeHealth);
        DrawTemporaryUpgradeEntry("Damage", data.damageUpgradeLevel, upgrades.GetMaxUpgradeLevel(), upgrades.TryUpgradeDamage);
        DrawTemporaryUpgradeEntry("Crit", data.critUpgradeLevel, upgrades.GetMaxUpgradeLevel(), upgrades.TryUpgradeCrit);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Height(36f)))
        {
            CloseUI();
        }

        GUILayout.EndArea();
    }

    private void DrawTemporaryUpgradeEntry(string label, int currentLevel, int maxLevel, System.Func<bool> tryUpgrade)
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label(label + " Lv " + currentLevel + " / " + maxLevel);

        if (currentLevel >= maxLevel)
        {
            GUILayout.Label("Cost: MAX");
            GUI.enabled = false;
            GUILayout.Button("MAXED", GUILayout.Height(30f));
            GUI.enabled = true;
            GUILayout.EndVertical();
            return;
        }

        int cost = UpgradeManager.Instance.GetUpgradeCost(currentLevel);
        GUILayout.Label("Cost: " + cost + " Essence");

        bool canAfford = SaveManager.Instance.currentSave.soulEssence >= cost;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = canAfford;

        if (GUILayout.Button("Upgrade " + label, GUILayout.Height(30f)))
        {
            if (tryUpgrade())
            {
                RefreshAll();
            }
        }

        GUI.enabled = previousEnabled;
        GUILayout.EndVertical();
    }

    private void RefreshUpgradeRow(
        int currentLevel,
        int maxLevel,
        TextMeshProUGUI levelText,
        TextMeshProUGUI costText,
        Button button)
    {
        if (levelText != null)
        {
            levelText.text = "Lv " + currentLevel + " / " + maxLevel;
        }

        if (costText == null || button == null)
        {
            return;
        }

        if (currentLevel >= maxLevel)
        {
            costText.text = "MAX";
            button.interactable = false;
            return;
        }

        int cost = UpgradeManager.Instance.GetUpgradeCost(currentLevel);
        costText.text = cost + " Essence";
        button.interactable = SaveManager.Instance.currentSave.soulEssence >= cost;
    }

    private bool HasSceneUIBindings()
    {
        return upgradePanel != null;
    }

    private void HidePanelImmediate()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        temporaryUiOpen = false;
    }

    private void BindButtonIfAvailable(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null || callback == null)
        {
            return;
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    private void OnUpgradeHealthClicked()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeHealth())
        {
            RefreshAll();
        }
    }

    private void OnUpgradeDamageClicked()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeDamage())
        {
            RefreshAll();
        }
    }

    private void OnUpgradeCritClicked()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeCrit())
        {
            RefreshAll();
        }
    }

    private void RefreshAll()
    {
        RefreshUI();

        SoulEssenceHUD hud = FindFirstObjectByType<SoulEssenceHUD>();
        if (hud != null)
        {
            hud.RefreshHUD();
        }
    }
}
