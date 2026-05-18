using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private const string DefaultLobbySceneName = "Lobby";
    private const string PlayButtonName = "Play btn";
    private const string SettingsButtonName = "SETTINGS";
    private const string QuitButtonName = "Quit";
    private const string SettingsPanelName = "Main Menu Music Settings Panel";
    private const string SettingsCloseButtonName = "Main Menu Settings Close Button";

    [SerializeField] private string lobbySceneName = DefaultLobbySceneName;
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private RectTransform settingsPanel;
    [SerializeField] private CanvasGroup settingsCanvasGroup;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private Button closeSettingsButton;

    private BackgroundMusic backgroundMusic;
    private bool isUpdatingSettingsUi;

    private void Awake()
    {
        Time.timeScale = 1f;
        RestoreMenuCanvasScale();
        backgroundMusic = BackgroundMusic.EnsureInstance(backgroundMusicClip);
        ResolveBindings();
        SetSettingsVisible(false);
    }

    private void OnDestroy()
    {
        UnbindButton(startButton, HandleStartPressed);
        UnbindButton(settingsButton, HandleSettingsPressed);
        UnbindButton(quitButton, HandleQuitPressed);
        UnbindButton(closeSettingsButton, HandleCloseSettingsPressed);

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveListener(HandleMuteChanged);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }
    }

    private void ResolveBindings()
    {
        Transform root = transform;
        startButton = startButton != null ? startButton : FindButtonByName(root, PlayButtonName);
        settingsButton = settingsButton != null ? settingsButton : FindButtonByName(root, SettingsButtonName);
        quitButton = quitButton != null ? quitButton : FindButtonByName(root, QuitButtonName);

        if (settingsPanel == null)
        {
            Transform discoveredPanel = FindDescendantByName(root, SettingsPanelName);
            settingsPanel = discoveredPanel as RectTransform;
        }

        if (settingsPanel == null)
        {
            settingsPanel = BuildSettingsPanel(root);
        }

        if (settingsPanel != null && settingsCanvasGroup == null)
        {
            settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null)
            {
                settingsCanvasGroup = settingsPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        BindButton(startButton, HandleStartPressed);
        BindButton(settingsButton, HandleSettingsPressed);
        BindButton(quitButton, HandleQuitPressed);
        BindButton(closeSettingsButton, HandleCloseSettingsPressed);

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveListener(HandleMuteChanged);
            muteToggle.onValueChanged.AddListener(HandleMuteChanged);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
            volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        RefreshSettingsUi();
    }

    private RectTransform BuildSettingsPanel(Transform parent)
    {
        GameObject panelObject = CreateUIObject(SettingsPanelName, parent, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(255f, -20f);
        panelRect.sizeDelta = new Vector2(520f, 320f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.SetAsLastSibling();

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.015f, 0.015f, 0.02f, 0.94f);

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

        settingsCanvasGroup = panelObject.GetComponent<CanvasGroup>();

        CreateSettingsLabel(panelRect, "Music Settings Title", "MUSIC SETTINGS", 34f, TextAlignmentOptions.Center, 48f);
        closeSettingsButton = CreateCloseButton(panelRect);

        RectTransform muteRow = CreateSettingsRow(panelRect, "Mute Music Row", 54f);
        TextMeshProUGUI muteLabel = CreateSettingsLabel(muteRow, "Mute Music Label", "Mute Music", 28f, TextAlignmentOptions.Left, 48f);
        SetFlexibleWidth(muteLabel.gameObject, 1f);
        muteToggle = CreateMusicToggle(muteRow);

        RectTransform volumeRow = CreateSettingsRow(panelRect, "Music Volume Header Row", 42f);
        TextMeshProUGUI volumeLabel = CreateSettingsLabel(volumeRow, "Music Volume Label", "Music Volume", 24f, TextAlignmentOptions.Left, 38f);
        SetFlexibleWidth(volumeLabel.gameObject, 1f);
        volumeValueText = CreateSettingsLabel(volumeRow, "Music Volume Value", "70%", 24f, TextAlignmentOptions.Right, 38f);
        SetPreferredWidth(volumeValueText.gameObject, 96f);

        volumeSlider = CreateMusicVolumeSlider(panelRect);
        return panelRect;
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
        ApplyMenuTextStyle(label);

        LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        return label;
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUIObject(SettingsCloseButtonName, parent, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
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
        StretchToParent(closeLabel.rectTransform);
        return button;
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

    private void HandleStartPressed()
    {
        Time.timeScale = 1f;
        SetSettingsVisible(false);

        string targetScene = string.IsNullOrWhiteSpace(lobbySceneName) ? DefaultLobbySceneName : lobbySceneName;
        if (!TemporaryLoadingUI.LoadSceneWithOverlay(targetScene, LoadSceneMode.Single))
        {
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }
    }

    private void HandleSettingsPressed()
    {
        ResolveBindings();
        SetSettingsVisible(!IsSettingsVisible());
    }

    private void HandleQuitPressed()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleCloseSettingsPressed()
    {
        SetSettingsVisible(false);
    }

    private void HandleMuteChanged(bool muted)
    {
        if (isUpdatingSettingsUi)
        {
            return;
        }

        BackgroundMusic music = GetBackgroundMusic();
        if (music == null)
        {
            return;
        }

        music.SetMuted(muted);
        RefreshSettingsUi();
    }

    private void HandleVolumeChanged(float volume)
    {
        if (isUpdatingSettingsUi)
        {
            return;
        }

        BackgroundMusic music = GetBackgroundMusic();
        if (music == null)
        {
            return;
        }

        music.SetVolume(volume);
        UpdateVolumeText(volume);
    }

    private BackgroundMusic GetBackgroundMusic()
    {
        if (backgroundMusic == null)
        {
            backgroundMusic = BackgroundMusic.EnsureInstance(backgroundMusicClip);
        }

        return backgroundMusic;
    }

    private void RefreshSettingsUi()
    {
        BackgroundMusic music = GetBackgroundMusic();
        if (music == null)
        {
            return;
        }

        isUpdatingSettingsUi = true;
        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(music.IsMuted);
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(music.Volume);
        }

        UpdateVolumeText(music.Volume);
        isUpdatingSettingsUi = false;
    }

    private void UpdateVolumeText(float volume)
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
        }
    }

    private bool IsSettingsVisible()
    {
        return settingsCanvasGroup != null && settingsCanvasGroup.alpha > 0.5f;
    }

    private void SetSettingsVisible(bool visible)
    {
        if (settingsCanvasGroup == null)
        {
            return;
        }

        settingsCanvasGroup.alpha = visible ? 1f : 0f;
        settingsCanvasGroup.interactable = visible;
        settingsCanvasGroup.blocksRaycasts = visible;

        if (settingsPanel != null)
        {
            settingsPanel.localScale = visible ? Vector3.one : Vector3.zero;
            if (visible)
            {
                settingsPanel.SetAsLastSibling();
            }
        }
    }

    private void RestoreMenuCanvasScale()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && transform.localScale.sqrMagnitude < 0.001f)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void ApplyMenuTextStyle(TextMeshProUGUI target)
    {
        Button styleSourceButton = settingsButton != null ? settingsButton : startButton;
        TextMeshProUGUI source = styleSourceButton != null ? styleSourceButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (target == null || source == null)
        {
            return;
        }

        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
        target.fontStyle = source.fontStyle;
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

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
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
        for (int i = 0; i < buttons.Length; i++)
        {
            Button candidate = buttons[i];
            if (candidate != null && NamesMatch(candidate.gameObject.name, objectName))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        if (NamesMatch(root.name, objectName))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool NamesMatch(string left, string right)
    {
        return string.Equals(NormalizeName(left), NormalizeName(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
}
