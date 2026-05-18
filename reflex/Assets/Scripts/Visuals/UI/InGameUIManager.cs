using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    private const string DefaultLobbySceneName = "Lobby";
    private const string DefaultMainMenuSceneName = "Main Menu";
    private const string GameOverCanvasName = "Game Over Canvas";
    private const string AuthoredTitlePath = "Main Box/Header";
    private const string AuthoredReturnToLobbyButtonPath = "Lobby btn";
    private const string PauseMenuOptionsName = "Pause Menu Options";
    private const string SettingsButtonName = "Settings";
    private const string ReturnToMenuButtonName = "Return";
    private const string MusicSettingsPanelName = "Music Settings Panel";
    private const string MusicSettingsCloseButtonName = "Music Settings Close Button";

    [Header("Health Bar References")]
    [SerializeField] private Image greenHPBarFill;
    [SerializeField] private Image redHPBarFill;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Weapon Icon")]
    public Image weaponIcon;

    [Header("HP Bar Settings")]
    [SerializeField] private float redLerpSpeed = 5f; // Speed of the health bar animation
    [SerializeField] private float greenLerpSpeed = 5f; // Speed of the health bar animation

    [Header("Canvas References")]
    [SerializeField] private CanvasGroup inGameUICanvasGroup;
    [SerializeField] private CanvasGroup PauseUICanvasGroup;

    [Header("Pause Menu")]
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;
    [SerializeField] private Button returnToMainMenuButton;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private Button settingsButton;
    [SerializeField] private RectTransform musicSettingsPanel;
    [SerializeField] private CanvasGroup musicSettingsCanvasGroup;
    [SerializeField] private Toggle muteMusicToggle;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeValueText;
    [SerializeField] private Button closeMusicSettingsButton;

    [Header("Game Over Screen")]
    [SerializeField] private string lobbySceneName = DefaultLobbySceneName;
    [SerializeField] private bool generateFreshRunOnReturn = true;
    [SerializeField] private RectTransform gameOverCanvasRoot;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private TextMeshProUGUI gameOverDetailsText;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;

    [Header("Game Over Summary Fields")]
    [SerializeField] private TextMeshProUGUI runtimeValueText;
    [SerializeField] private TextMeshProUGUI floorsClearedValueText;
    [SerializeField] private TextMeshProUGUI stagesClearedValueText;
    [SerializeField] private TextMeshProUGUI enemiesKilledValueText;
    [SerializeField] private TextMeshProUGUI killEssenceValueText;
    [SerializeField] private TextMeshProUGUI baseClearValueText;
    [SerializeField] private TextMeshProUGUI floorDepthValueText;
    [SerializeField] private TextMeshProUGUI rawSubtotalValueText;
    [SerializeField] private TextMeshProUGUI runMultiplierValueText;
    [SerializeField] private TextMeshProUGUI rewardTotalValueText;
    [SerializeField] private TextMeshProUGUI composureBonusValueText;
    [SerializeField] private TextMeshProUGUI otherBonusValueText;
    [SerializeField] private TextMeshProUGUI soulEssenceEarnedValueText;

    [Header("Status Messaging")]
    [SerializeField] private TextMeshProUGUI statusMessageText;
    [SerializeField] private CanvasGroup statusMessageCanvasGroup;
    [SerializeField] private float statusMessageFadeInDuration = 0.1f;
    [SerializeField] private float statusMessageHoldDuration = 1.8f;
    [SerializeField] private float statusMessageFadeOutDuration = 0.35f;

    [Header("Pop-up Text")]
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private float popupFadeInDuration = 0.1f;
    [SerializeField] private float popupFadeOutDuration = 0.35f;

    private Coroutine healthAnimationCoroutine;
    private Coroutine statusMessageCoroutine;
    private PlayerManager observedPlayer;
    private BackgroundMusic backgroundMusic;
    private Vector3 gameOverCanvasVisibleScale = Vector3.one;
    private bool useScaleVisibilityForGameOverCanvas;
    private bool isUpdatingMusicSettingsUI;
    private bool isGameOverShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeBackgroundMusic();
        ResolveSettingsBindings();
        SetMusicSettingsVisible(false);
        ResolveGameOverBindings();
        SetGameOverCanvasVisible(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryBindPlayer();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindPlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (observedPlayer == null)
        {
            TryBindPlayer();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isGameOverShowing)
        {
            InitializeBackgroundMusic();
            ResolveSettingsBindings();
            SetMusicSettingsVisible(false);
            ResolveGameOverBindings();
            SetGameOverCanvasVisible(false);

            bool isMainMenuScene = SceneNameEquals(scene.name, mainMenuSceneName);
            SetCanvasGroupVisible(inGameUICanvasGroup, !isMainMenuScene);
            SetPauseCanvasVisible(false);
        }

        TryBindPlayer();
    }

    private void TryBindPlayer()
    {
        PlayerManager player = FindFirstObjectByType<PlayerManager>();
        if (player == null || observedPlayer == player)
        {
            return;
        }

        UnbindPlayer();
        observedPlayer = player;
        observedPlayer.PlayerDied += HandlePlayerDied;
    }

    private void UnbindPlayer()
    {
        if (observedPlayer == null)
        {
            return;
        }

        observedPlayer.PlayerDied -= HandlePlayerDied;
        observedPlayer = null;
    }

    private void HandlePlayerDied(PlayerManager deadPlayer)
    {
        ShowGameOver(deadPlayer);
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

    public void UpdateWeaponIcon(Sprite newIcon)
    {
        if (weaponIcon != null)
        {
            weaponIcon.sprite = newIcon;
            weaponIcon.SetNativeSize();
        }
    }

    // display popup text canvas group with fade in transition
    public void ShowPopupText(string text)
    {
        if (popupText == null || popupCanvasGroup == null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        popupText.text = text;
        StartCoroutine(OnPopupTextRoutine());
    }

    public void HidePopupText()
    {
        if (popupCanvasGroup == null)
        {
            return;
        }

        StartCoroutine(OffPopupTextRoutine());
    }

    private IEnumerator OnPopupTextRoutine()
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < popupFadeInDuration)
        {
            popupCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / popupFadeInDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        popupCanvasGroup.alpha = 1f;
        popupCanvasGroup.interactable = true;
        popupCanvasGroup.blocksRaycasts = true;
    }

    public IEnumerator OffPopupTextRoutine()
    {
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;
        // Fade out
        float elapsed = 0f;
        while (elapsed < popupFadeOutDuration)
        {
            popupCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / popupFadeOutDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        popupCanvasGroup.alpha = 0f;
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
        float targetFill = Mathf.Clamp01(currentHp / Mathf.Max(0.01f, maxHp));

        // 1. Smoothly transition the green bar to the target fill
        // Using Time.unscaledDeltaTime ensures it moves even if the game pauses/slows down on death
        while (greenHPBarFill != null && Mathf.Abs(greenHPBarFill.fillAmount - targetFill) > 0.001f)
        {
            greenHPBarFill.fillAmount = Mathf.Lerp(greenHPBarFill.fillAmount, targetFill, Time.unscaledDeltaTime * greenLerpSpeed);
            yield return null;
        }

        if (greenHPBarFill != null)
        {
            greenHPBarFill.fillAmount = targetFill;
        }

        // If health drops to 0, skip the 1.5s delay so the red bar drains immediately 
        if (targetFill > 0f)
        {
            // Wait 1.5 seconds using unscaled real time before the red bar catches up
            yield return new WaitForSecondsRealtime(1.5f);
        }

        // 2. Smoothly transition the red bar to match the target fill
        while (redHPBarFill != null && Mathf.Abs(redHPBarFill.fillAmount - targetFill) > 0.001f)
        {
            redHPBarFill.fillAmount = Mathf.MoveTowards(redHPBarFill.fillAmount, targetFill, redLerpSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        if (redHPBarFill != null)
        {
            redHPBarFill.fillAmount = targetFill;
        }
    }

    public void UpdateHPText(float currentHp, float maxHp)
    {
        if (hpText == null) return;
        // Clamp currentHp to 0 so it doesn't display negative values if overkill damage happens
        hpText.text = $"{Mathf.RoundToInt(Mathf.Max(0, currentHp))}/{Mathf.RoundToInt(maxHp)}";
    }

    public void ShowPauseUI()
    {
        if (isGameOverShowing)
        {
            return;
        }

        ResolveSettingsBindings();
        RefreshMusicSettingsUI();
        SetMusicSettingsVisible(false);
        SetCanvasGroupVisible(inGameUICanvasGroup, false);
        SetPauseCanvasVisible(true);
    }

    public void HidePauseUI()
    {
        if (isGameOverShowing)
        {
            return;
        }

        SetMusicSettingsVisible(false);
        SetCanvasGroupVisible(inGameUICanvasGroup, true);
        SetPauseCanvasVisible(false);
    }

    private void InitializeBackgroundMusic()
    {
        backgroundMusic = BackgroundMusic.EnsureInstance(backgroundMusicClip);
    }

    private BackgroundMusic GetBackgroundMusic()
    {
        if (backgroundMusic == null)
        {
            InitializeBackgroundMusic();
        }

        return backgroundMusic;
    }

    private void ResolveSettingsBindings()
    {
        Transform pauseRoot = PauseUICanvasGroup != null ? PauseUICanvasGroup.transform : transform;
        Transform optionsRoot = FindDescendantByName(pauseRoot, PauseMenuOptionsName);
        Button discoveredSettingsButton = FindButtonByName(optionsRoot != null ? optionsRoot : pauseRoot, SettingsButtonName);
        Button discoveredReturnButton = FindButtonByName(optionsRoot != null ? optionsRoot : pauseRoot, ReturnToMenuButtonName);
        BindSettingsButton(settingsButton != null ? settingsButton : discoveredSettingsButton);
        BindReturnToMainMenuButton(returnToMainMenuButton != null ? returnToMainMenuButton : discoveredReturnButton);

        if (musicSettingsPanel == null)
        {
            Transform discoveredPanel = FindDescendantByName(pauseRoot, MusicSettingsPanelName);
            musicSettingsPanel = discoveredPanel as RectTransform;
        }

        if (musicSettingsPanel == null)
        {
            musicSettingsPanel = BuildMusicSettingsPanel(pauseRoot);
        }

        ConfigureMusicSettingsPanelLayout(musicSettingsPanel, pauseRoot);

        if (musicSettingsPanel != null && musicSettingsCanvasGroup == null)
        {
            musicSettingsCanvasGroup = musicSettingsPanel.GetComponent<CanvasGroup>();
            if (musicSettingsCanvasGroup == null)
            {
                musicSettingsCanvasGroup = musicSettingsPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        EnsureMusicSettingsCloseButton();
        BindMusicControls();
        RefreshMusicSettingsUI();
    }

    private void BindSettingsButton(Button button)
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(HandleSettingsPressed);
        }

        settingsButton = button;
        if (settingsButton == null)
        {
            return;
        }

        settingsButton.onClick.RemoveListener(HandleSettingsPressed);
        settingsButton.onClick.AddListener(HandleSettingsPressed);
    }

    private void BindReturnToMainMenuButton(Button button)
    {
        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.RemoveListener(HandleReturnToMainMenuPressed);
        }

        returnToMainMenuButton = button;
        if (returnToMainMenuButton == null)
        {
            return;
        }

        returnToMainMenuButton.onClick.RemoveListener(HandleReturnToMainMenuPressed);
        returnToMainMenuButton.onClick.AddListener(HandleReturnToMainMenuPressed);
    }

    private RectTransform BuildMusicSettingsPanel(Transform pauseRoot)
    {
        if (pauseRoot == null)
        {
            return null;
        }

        GameObject panelObject = CreateUIObject(MusicSettingsPanelName, pauseRoot, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup));

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        ConfigureMusicSettingsPanelLayout(panelRect, pauseRoot);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.015f, 0.015f, 0.02f, 0.92f);

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.85f, 0.05f, 0.05f, 0.85f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        musicSettingsCanvasGroup = panelObject.GetComponent<CanvasGroup>();

        CreateSettingsLabel(panelRect, "Music Settings Title", "MUSIC SETTINGS", 34f, TextAlignmentOptions.Center, 48f);
        closeMusicSettingsButton = CreateMusicSettingsCloseButton(panelRect);

        RectTransform muteRow = CreateSettingsRow(panelRect, "Mute Music Row", 54f);
        TextMeshProUGUI muteLabel = CreateSettingsLabel(muteRow, "Mute Music Label", "Mute Music", 28f, TextAlignmentOptions.Left, 48f);
        SetFlexibleWidth(muteLabel.gameObject, 1f);
        muteMusicToggle = CreateMusicToggle(muteRow);

        RectTransform volumeHeaderRow = CreateSettingsRow(panelRect, "Music Volume Header Row", 42f);
        TextMeshProUGUI volumeLabel = CreateSettingsLabel(volumeHeaderRow, "Music Volume Label", "Music Volume", 24f, TextAlignmentOptions.Left, 38f);
        SetFlexibleWidth(volumeLabel.gameObject, 1f);
        musicVolumeValueText = CreateSettingsLabel(volumeHeaderRow, "Music Volume Value", "70%", 24f, TextAlignmentOptions.Right, 38f);
        SetPreferredWidth(musicVolumeValueText.gameObject, 96f);

        musicVolumeSlider = CreateMusicVolumeSlider(panelRect);
        return panelRect;
    }

    private void ConfigureMusicSettingsPanelLayout(RectTransform panelRect, Transform pauseRoot)
    {
        if (panelRect == null || pauseRoot == null)
        {
            return;
        }

        if (panelRect.parent != pauseRoot)
        {
            panelRect.SetParent(pauseRoot, false);
        }

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 20f);
        panelRect.sizeDelta = new Vector2(520f, 320f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.SetAsLastSibling();
    }

    private RectTransform CreateSettingsRow(Transform parent, string objectName, float preferredHeight)
    {
        GameObject rowObject = CreateUIObject(objectName, parent, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, preferredHeight);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        return rowRect;
    }

    private TextMeshProUGUI CreateSettingsLabel(Transform parent, string objectName, string text, float fontSize, TextAlignmentOptions alignment, float preferredHeight)
    {
        GameObject labelObject = CreateUIObject(objectName, parent, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        ApplyPauseMenuTextStyle(label);

        LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        return label;
    }

    private Toggle CreateMusicToggle(Transform parent)
    {
        GameObject toggleObject = CreateUIObject("Mute Music Toggle", parent, typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(48f, 48f);

        LayoutElement toggleLayout = toggleObject.GetComponent<LayoutElement>();
        toggleLayout.preferredWidth = 48f;
        toggleLayout.preferredHeight = 48f;

        GameObject backgroundObject = CreateUIObject("Background", toggleRect, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        StretchToParent(backgroundRect);
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.08f, 0.08f, 0.1f, 1f);

        Outline backgroundOutline = backgroundObject.AddComponent<Outline>();
        backgroundOutline.effectColor = new Color(0.9f, 0.12f, 0.12f, 0.9f);
        backgroundOutline.effectDistance = new Vector2(2f, -2f);

        GameObject checkObject = CreateUIObject("Checkmark", backgroundRect, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(9f, 9f);
        checkRect.offsetMax = new Vector2(-9f, -9f);
        Image checkImage = checkObject.GetComponent<Image>();
        checkImage.color = new Color(0.92f, 0.05f, 0.05f, 1f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkImage;
        return toggle;
    }

    private Button CreateMusicSettingsCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUIObject(MusicSettingsCloseButtonName, parent, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-30f, -30f);
        buttonRect.sizeDelta = new Vector2(44f, 44f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.1f, 0.02f, 0.025f, 0.98f);

        Outline buttonOutline = buttonObject.AddComponent<Outline>();
        buttonOutline.effectColor = new Color(0.9f, 0.12f, 0.12f, 0.95f);
        buttonOutline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        TextMeshProUGUI closeLabel = CreateSettingsLabel(buttonRect, "Close Label", "X", 28f, TextAlignmentOptions.Center, 44f);
        RectTransform labelRect = closeLabel.rectTransform;
        StretchToParent(labelRect);
        closeLabel.raycastTarget = false;

        return button;
    }

    private Slider CreateMusicVolumeSlider(Transform parent)
    {
        GameObject sliderObject = CreateUIObject("Music Volume Slider", parent, typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 50f);

        LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
        sliderLayout.preferredHeight = 50f;

        GameObject backgroundObject = CreateUIObject("Background", sliderRect, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.offsetMin = new Vector2(10f, -5f);
        backgroundRect.offsetMax = new Vector2(-10f, 5f);
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.08f, 0.08f, 0.1f, 1f);

        GameObject fillAreaObject = CreateUIObject("Fill Area", sliderRect, typeof(RectTransform));
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(10f, -5f);
        fillAreaRect.offsetMax = new Vector2(-10f, 5f);

        GameObject fillObject = CreateUIObject("Fill", fillAreaRect, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        StretchToParent(fillRect);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.9f, 0.08f, 0.08f, 1f);

        GameObject handleAreaObject = CreateUIObject("Handle Slide Area", sliderRect, typeof(RectTransform));
        RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
        StretchToParent(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handleObject = CreateUIObject("Handle", handleAreaRect, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30f, 30f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private GameObject CreateUIObject(string objectName, Transform parent, params Type[] components)
    {
        GameObject uiObject = new GameObject(objectName, components);
        uiObject.layer = parent != null ? parent.gameObject.layer : gameObject.layer;
        if (parent != null)
        {
            uiObject.transform.SetParent(parent, false);
        }

        return uiObject;
    }

    private void ApplyPauseMenuTextStyle(TextMeshProUGUI target)
    {
        if (target == null || settingsButton == null)
        {
            return;
        }

        TextMeshProUGUI source = settingsButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (source == null)
        {
            return;
        }

        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
        target.fontStyle = source.fontStyle;
    }

    private void BindMusicControls()
    {
        if (closeMusicSettingsButton != null)
        {
            closeMusicSettingsButton.onClick.RemoveListener(HandleCloseMusicSettingsPressed);
            closeMusicSettingsButton.onClick.AddListener(HandleCloseMusicSettingsPressed);
        }

        if (muteMusicToggle != null)
        {
            muteMusicToggle.onValueChanged.RemoveListener(HandleMuteMusicChanged);
            muteMusicToggle.onValueChanged.AddListener(HandleMuteMusicChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        }
    }

    private void HandleSettingsPressed()
    {
        ResolveSettingsBindings();
        SetMusicSettingsVisible(!IsMusicSettingsVisible());
    }

    private void HandleCloseMusicSettingsPressed()
    {
        SetMusicSettingsVisible(false);
    }

    private void HandleReturnToMainMenuPressed()
    {
        Time.timeScale = 1f;
        isGameOverShowing = false;
        SetMusicSettingsVisible(false);
        SetGameOverCanvasVisible(false);
        SetCanvasGroupVisible(inGameUICanvasGroup, false);
        SetPauseCanvasVisible(false);

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResetPauseState();
        }

        string targetScene = string.IsNullOrWhiteSpace(mainMenuSceneName) ? DefaultMainMenuSceneName : mainMenuSceneName;
        if (!TemporaryLoadingUI.LoadSceneWithOverlay(targetScene, LoadSceneMode.Single))
        {
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }
    }

    private void HandleMuteMusicChanged(bool muted)
    {
        if (isUpdatingMusicSettingsUI)
        {
            return;
        }

        BackgroundMusic music = GetBackgroundMusic();
        if (music == null)
        {
            return;
        }

        music.SetMuted(muted);
        RefreshMusicSettingsUI();
    }

    private void HandleMusicVolumeChanged(float volume)
    {
        if (isUpdatingMusicSettingsUI)
        {
            return;
        }

        BackgroundMusic music = GetBackgroundMusic();
        if (music == null)
        {
            return;
        }

        music.SetVolume(volume);
        UpdateMusicVolumeText(volume);
    }

    private void RefreshMusicSettingsUI()
    {
        BackgroundMusic music = GetBackgroundMusic();
        if (music == null)
        {
            return;
        }

        isUpdatingMusicSettingsUI = true;
        if (muteMusicToggle != null)
        {
            muteMusicToggle.SetIsOnWithoutNotify(music.IsMuted);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(music.Volume);
        }

        UpdateMusicVolumeText(music.Volume);
        isUpdatingMusicSettingsUI = false;
    }

    private void UpdateMusicVolumeText(float volume)
    {
        if (musicVolumeValueText != null)
        {
            musicVolumeValueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
        }
    }

    private bool IsMusicSettingsVisible()
    {
        return musicSettingsCanvasGroup != null && musicSettingsCanvasGroup.alpha > 0.5f;
    }

    private void SetMusicSettingsVisible(bool visible)
    {
        if (musicSettingsCanvasGroup == null)
        {
            return;
        }

        musicSettingsCanvasGroup.alpha = visible ? 1f : 0f;
        musicSettingsCanvasGroup.interactable = visible;
        musicSettingsCanvasGroup.blocksRaycasts = visible;

        if (musicSettingsPanel != null)
        {
            musicSettingsPanel.localScale = visible ? Vector3.one : Vector3.zero;
            if (visible)
            {
                musicSettingsPanel.SetAsLastSibling();
            }
        }
    }

    private void EnsureMusicSettingsCloseButton()
    {
        if (musicSettingsPanel == null)
        {
            return;
        }

        if (closeMusicSettingsButton == null)
        {
            Transform closeRoot = FindDescendantByName(musicSettingsPanel, MusicSettingsCloseButtonName);
            closeMusicSettingsButton = closeRoot != null ? closeRoot.GetComponent<Button>() : null;
        }

        if (closeMusicSettingsButton == null)
        {
            closeMusicSettingsButton = CreateMusicSettingsCloseButton(musicSettingsPanel);
        }

        closeMusicSettingsButton.transform.SetAsLastSibling();
    }

    private static void SetFlexibleWidth(GameObject target, float flexibleWidth)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.flexibleWidth = flexibleWidth;
    }

    private static void SetPreferredWidth(GameObject target, float preferredWidth)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = preferredWidth;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
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

    public void ShowGameOver(PlayerManager deadPlayer)
    {
        if (isGameOverShowing)
        {
            return;
        }

        ResolveGameOverBindings();
        if (!HasUsableGameOverBindings())
        {
            Debug.LogWarning("InGameUIManager cannot show game over because the UI Manager Game Over Canvas is missing required bindings.");
            return;
        }

        RunRewardSummary summary = BuildFallbackSummary();
        if (RewardManager.Instance != null &&
            RewardManager.Instance.TryGetRunRewardSummary(out RunRewardSummary runSummary))
        {
            summary = runSummary;
        }

        if (HasAnyGameOverSummaryField())
        {
            PopulateStructuredSummary(summary);
        }
        else if (gameOverDetailsText != null)
        {
            gameOverDetailsText.text = BuildSummaryDetailsText(summary);
        }

        if (gameOverTitleText != null)
        {
            gameOverTitleText.text = "GAME OVER";
        }

        SetCanvasGroupVisible(inGameUICanvasGroup, false);
        SetPauseCanvasVisible(false);
        SetGameOverCanvasVisible(true);
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.interactable = true;
        }

        isGameOverShowing = true;
        Time.timeScale = 0f;
    }

    private void ResolveGameOverBindings()
    {
        if (TryBindGameOverCanvas(gameOverCanvasRoot))
        {
            return;
        }

        RectTransform discoveredRoot = FindGameOverCanvasRoot();
        TryBindGameOverCanvas(discoveredRoot);
    }

    private RectTransform FindGameOverCanvasRoot()
    {
        Transform directChild = transform.Find(GameOverCanvasName);
        if (directChild != null)
        {
            return directChild as RectTransform;
        }

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.name == GameOverCanvasName)
            {
                return canvas.transform as RectTransform;
            }
        }

        return null;
    }

    private bool TryBindGameOverCanvas(RectTransform canvasRoot)
    {
        if (canvasRoot == null)
        {
            ClearGameOverSummaryBindings();
            return false;
        }

        gameOverCanvasRoot = canvasRoot;
        gameOverCanvasGroup = canvasRoot.GetComponent<CanvasGroup>();
        if (gameOverCanvasGroup == null)
        {
            gameOverCanvasGroup = canvasRoot.gameObject.AddComponent<CanvasGroup>();
        }

        gameOverTitleText = gameOverTitleText != null ? gameOverTitleText : FindTextByPath(canvasRoot, AuthoredTitlePath);
        gameOverDetailsText = gameOverDetailsText != null ? gameOverDetailsText : FindTextByPath(canvasRoot, "Main Box/Details");

        Transform returnButtonTransform = FindDescendantByName(canvasRoot, AuthoredReturnToLobbyButtonPath);
        Button discoveredButton = returnButtonTransform != null ? returnButtonTransform.GetComponent<Button>() : null;
        BindReturnToLobbyButton(returnToLobbyButton != null ? returnToLobbyButton : discoveredButton != null ? discoveredButton : canvasRoot.GetComponentInChildren<Button>(true));

        CacheGameOverCanvasVisibility(canvasRoot, true);
        TryBindStructuredSummaryFields(canvasRoot);
        return HasUsableGameOverBindings();
    }

    private bool HasUsableGameOverBindings()
    {
        return gameOverCanvasRoot != null &&
               gameOverCanvasGroup != null &&
               returnToLobbyButton != null;
    }

    private void TryBindStructuredSummaryFields(Transform root)
    {
        ClearGameOverSummaryBindings();
        if (root == null)
        {
            return;
        }

        Transform mainBox = FindDescendantByName(root, "Main Box");
        Transform summaryRoot = mainBox != null ? mainBox : root;

        runtimeValueText = FindValueTextUnderLabel(summaryRoot, "Runtime");
        floorsClearedValueText = FindValueTextUnderLabel(summaryRoot, "Floors Cleared");
        stagesClearedValueText = FindValueTextUnderLabel(summaryRoot, "Stage Cleared");
        enemiesKilledValueText = FindValueTextUnderLabel(summaryRoot, "Enemies Killed");
        killEssenceValueText = FindValueTextUnderLabel(summaryRoot, "Kill Essence");
        baseClearValueText = FindValueTextUnderLabel(summaryRoot, "Base Clear");
        floorDepthValueText = FindValueTextUnderLabel(summaryRoot, "Floor Depth");
        rawSubtotalValueText = FindValueTextUnderLabel(summaryRoot, "Floors Cleared", 1);
        runMultiplierValueText = FindValueTextUnderLabel(summaryRoot, "Multiplier");
        rewardTotalValueText = FindValueTextUnderLabel(summaryRoot, "Reward");
        composureBonusValueText = FindValueTextUnderLabel(summaryRoot, "Composure Bonus");
        otherBonusValueText = FindValueTextUnderLabel(summaryRoot, "Others");
        soulEssenceEarnedValueText = FindValueTextUnderLabel(summaryRoot, "Soul Essence");
    }

    private void ClearGameOverSummaryBindings()
    {
        runtimeValueText = null;
        floorsClearedValueText = null;
        stagesClearedValueText = null;
        enemiesKilledValueText = null;
        killEssenceValueText = null;
        baseClearValueText = null;
        floorDepthValueText = null;
        rawSubtotalValueText = null;
        runMultiplierValueText = null;
        rewardTotalValueText = null;
        composureBonusValueText = null;
        otherBonusValueText = null;
        soulEssenceEarnedValueText = null;
    }

    private bool HasAnyGameOverSummaryField()
    {
        return runtimeValueText != null ||
               floorsClearedValueText != null ||
               stagesClearedValueText != null ||
               enemiesKilledValueText != null ||
               killEssenceValueText != null ||
               baseClearValueText != null ||
               floorDepthValueText != null ||
               rawSubtotalValueText != null ||
               runMultiplierValueText != null ||
               rewardTotalValueText != null ||
               composureBonusValueText != null ||
               otherBonusValueText != null ||
               soulEssenceEarnedValueText != null;
    }

    private void PopulateStructuredSummary(RunRewardSummary summary)
    {
        int otherEssence = Mathf.Max(0, summary.totalEssenceEarned - summary.stageRewardEssence - summary.composureBonusEssence);
        int stagesCleared = Mathf.Max(summary.stagesCleared, summary.stageReached);
        int floorsCleared = Mathf.Max(0, summary.floorReached);

        SetText(runtimeValueText, FormatRuntime(summary.runtimeSeconds));
        SetText(floorsClearedValueText, floorsCleared.ToString());
        SetText(stagesClearedValueText, stagesCleared.ToString());
        SetText(enemiesKilledValueText, summary.enemiesDefeated.ToString());
        SetText(killEssenceValueText, summary.rawKillEssence.ToString());
        SetText(baseClearValueText, summary.rawBaseEssence.ToString());
        SetText(floorDepthValueText, summary.rawFloorEssence.ToString());
        SetText(rawSubtotalValueText, summary.rawEssenceBeforeMultipliers.ToString());
        SetText(runMultiplierValueText, $"x{summary.effectiveCombinedMultiplier:0.00}");
        SetText(rewardTotalValueText, summary.stageRewardEssence.ToString());
        SetText(composureBonusValueText, summary.composureBonusEssence.ToString());
        SetText(otherBonusValueText, otherEssence.ToString());
        SetText(soulEssenceEarnedValueText, summary.totalEssenceEarned.ToString());
    }

    private RunRewardSummary BuildFallbackSummary()
    {
        RunRewardSummary fallback = new RunRewardSummary
        {
            runtimeSeconds = 0f,
            floorReached = 0,
            stageReached = 0,
            stagesCleared = 0,
            enemiesDefeated = 0,
            essencePerKill = 0,
            totalEssenceEarned = 0,
            stageRewardEssence = 0,
            composureBonusEssence = 0,
            rawBaseEssence = 0,
            rawKillEssence = 0,
            rawFloorEssence = 0,
            rawEssenceBeforeMultipliers = 0,
            effectiveCombinedMultiplier = 1f
        };

        if (LevelRunManager.HasInstance)
        {
            LevelClearContext context = LevelRunManager.Instance.LastClearContext;
            if (context.floorDepth > 0)
            {
                int stagesPerFloor = Mathf.Max(1, LevelRunManager.Instance.StagesPerFloor);
                fallback.floorReached = ((context.floorDepth - 1) / stagesPerFloor) + 1;
                fallback.stageReached = ((context.floorDepth - 1) % stagesPerFloor) + 1;
            }
        }

        return fallback;
    }

    private string BuildSummaryDetailsText(RunRewardSummary summary)
    {
        int otherEssence = Mathf.Max(0, summary.totalEssenceEarned - summary.stageRewardEssence - summary.composureBonusEssence);
        int stagesCleared = Mathf.Max(summary.stagesCleared, summary.stageReached);
        int floorsCleared = Mathf.Max(0, summary.floorReached);

        return
            $"{FormatTwoColumn($"Run time :{FormatRuntime(summary.runtimeSeconds)}", $"Stages Cleared :{stagesCleared}")}\n" +
            $"{FormatTwoColumn($"Floors Cleared :{floorsCleared}", $"Enemies Killed: {summary.enemiesDefeated}")}\n\n" +
            "<align=center>Soul Essence Calculation</align>\n\n" +
            $"{FormatTwoColumn($"Kill Essence :{summary.rawKillEssence}", $"Run Multiplier :x{summary.effectiveCombinedMultiplier:0.00}")}\n" +
            $"{FormatTwoColumn($"Base Clear :{summary.rawBaseEssence}", $"Reward Total:{summary.stageRewardEssence}")}\n" +
            $"{FormatTwoColumn($"Floor Depth:{summary.rawFloorEssence}", $"Composure Bonus :{summary.composureBonusEssence}")}\n" +
            $"{FormatTwoColumn($"Raw Subtotal:{summary.rawEssenceBeforeMultipliers}", $"Other Bonus :{otherEssence}")}\n\n" +
            $"<align=center>Soul Essence Earned :{summary.totalEssenceEarned}</align>";
    }

    private string FormatRuntime(float runtimeSeconds)
    {
        TimeSpan runTime = TimeSpan.FromSeconds(Mathf.Max(0f, runtimeSeconds));
        return runTime.TotalHours >= 1
            ? runTime.ToString(@"hh\:mm\:ss")
            : runTime.ToString(@"mm\:ss");
    }

    private string FormatTwoColumn(string left, string right)
    {
        return $"{left,-34}{right}";
    }

    private void BindReturnToLobbyButton(Button button)
    {
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyPressed);
        }

        returnToLobbyButton = button;
        if (returnToLobbyButton == null)
        {
            return;
        }

        returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyPressed);
        returnToLobbyButton.onClick.AddListener(HandleReturnToLobbyPressed);
    }

    private void HandleReturnToLobbyPressed()
    {
        Time.timeScale = 1f;
        isGameOverShowing = false;
        SetGameOverCanvasVisible(false);
        SetCanvasGroupVisible(inGameUICanvasGroup, true);
        SetPauseCanvasVisible(false);

        PlayerManager player = observedPlayer != null ? observedPlayer : FindFirstObjectByType<PlayerManager>();
        if (player != null)
        {
            player.RespawnForRunStart();
        }

        if (generateFreshRunOnReturn && LevelRunManager.HasInstance)
        {
            LevelRunManager.Instance.GenerateNewRun();
        }

        string targetScene = string.IsNullOrWhiteSpace(lobbySceneName) ? DefaultLobbySceneName : lobbySceneName;
        if (!TemporaryLoadingUI.LoadSceneWithOverlay(targetScene, LoadSceneMode.Single))
        {
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }
    }

    private void CacheGameOverCanvasVisibility(RectTransform root, bool useScaleForVisibility)
    {
        gameOverCanvasRoot = root;
        useScaleVisibilityForGameOverCanvas = useScaleForVisibility && root != null;
        gameOverCanvasVisibleScale = Vector3.one;

        if (gameOverCanvasRoot == null)
        {
            return;
        }

        Vector3 currentScale = gameOverCanvasRoot.localScale;
        gameOverCanvasVisibleScale = currentScale.sqrMagnitude > 0.0001f ? currentScale : Vector3.one;
    }

    private void SetGameOverCanvasVisible(bool visible)
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = visible ? 1f : 0f;
            gameOverCanvasGroup.interactable = visible;
            gameOverCanvasGroup.blocksRaycasts = visible;
        }

        if (useScaleVisibilityForGameOverCanvas && gameOverCanvasRoot != null)
        {
            gameOverCanvasRoot.localScale = visible ? gameOverCanvasVisibleScale : Vector3.zero;
        }
    }

    private static TextMeshProUGUI FindTextByPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        Transform found = root.Find(path);
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Button FindButtonByName(Transform root, string objectName)
    {
        Transform found = FindDescendantByName(root, objectName);
        Button button = found != null ? found.GetComponent<Button>() : null;
        if (button != null)
        {
            return button;
        }

        Button[] buttons = root != null ? root.GetComponentsInChildren<Button>(true) : Array.Empty<Button>();
        string normalizedName = NormalizeName(objectName);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button candidate = buttons[i];
            if (candidate != null && NormalizeName(candidate.gameObject.name) == normalizedName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform root, string objectName)
    {
        return FindDescendantByName(root, objectName, 0);
    }

    private static Transform FindDescendantByName(Transform root, string objectName, int occurrenceIndex)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        int matchesSkipped = 0;
        return FindDescendantByName(root, NormalizeName(objectName), Mathf.Max(0, occurrenceIndex), ref matchesSkipped);
    }

    private static Transform FindDescendantByName(Transform root, string normalizedName, int occurrenceIndex, ref int matchesSkipped)
    {
        if (root == null)
        {
            return null;
        }

        if (NormalizeName(root.name) == normalizedName)
        {
            if (matchesSkipped >= occurrenceIndex)
            {
                return root;
            }

            matchesSkipped++;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), normalizedName, occurrenceIndex, ref matchesSkipped);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static TextMeshProUGUI FindValueTextUnderLabel(Transform root, string labelObjectName, int occurrenceIndex = 0)
    {
        Transform labelRoot = FindDescendantByName(root, labelObjectName, occurrenceIndex);
        if (labelRoot == null)
        {
            return null;
        }

        TextMeshProUGUI[] texts = labelRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI candidate = texts[i];
            if (candidate != null && NormalizeName(candidate.gameObject.name) == "Text Out")
            {
                return candidate;
            }
        }

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI candidate = texts[i];
            if (candidate != null && candidate.transform != labelRoot && NormalizeName(candidate.gameObject.name) != "Text Bold")
            {
                return candidate;
            }
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static bool SceneNameEquals(string sceneName, string expectedSceneName)
    {
        return string.Equals(NormalizeName(sceneName), NormalizeName(expectedSceneName), StringComparison.OrdinalIgnoreCase);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetCanvasGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void SetPauseCanvasVisible(bool visible)
    {
        SetCanvasGroupVisible(PauseUICanvasGroup, visible);
        if (PauseUICanvasGroup != null)
        {
            PauseUICanvasGroup.transform.localScale = visible ? Vector3.one : Vector3.zero;
        }
    }
}
