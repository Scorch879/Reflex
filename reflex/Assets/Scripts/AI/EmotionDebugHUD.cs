using UnityEngine;
using UnityEngine.InputSystem;

public class EmotionDebugHUD : MonoBehaviour
{
    private const float CompactWidth = 430f;
    private const float Padding = 12f;

    private static EmotionDebugHUD _instance;

    [SerializeField] private bool visible = true;
    [SerializeField] private Key toggleKey = Key.F3;
    [SerializeField] private Key toggleFullscreenKey = Key.F4;
    [SerializeField] private bool fullscreen;

    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _mutedStyle;
    private Vector2 _scrollPosition;

    public static void EnsureExists()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = FindFirstObjectByType<EmotionDebugHUD>();
        if (_instance != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("Emotion Debug HUD");
        _instance = hudObject.AddComponent<EmotionDebugHUD>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
        }

        if (Keyboard.current != null && Keyboard.current[toggleFullscreenKey].wasPressedThisFrame)
        {
            fullscreen = !fullscreen;
        }
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        EnsureStyles();

        EmotionEngine engine = EmotionEngine.Instance;
        EmotionDirectorDirective directive = EmotionDirector.Instance.CurrentDirective;
        EmotionProfileSnapshot snapshot = engine.CurrentSnapshot;
        EmotionRoomReport lastRoom = engine.LastRoomReport;

        float width = fullscreen ? Screen.width - (Padding * 2f) : Mathf.Min(CompactWidth, Screen.width - (Padding * 2f));
        float height = Screen.height - (Padding * 2f);
        Rect area = new Rect(Padding, Padding, width, height);
        GUILayout.BeginArea(area, GUIContent.none, _panelStyle);
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        GUILayout.Label("EMOTION ENGINE", _titleStyle);
        DrawLine($"Profile: {snapshot.state}");
        DrawLine($"Aggression: {snapshot.aggressionScore:0.00}");
        DrawLine($"Recent score: {snapshot.recentAggressionScore:0.00}");
        DrawLine($"Confidence: {snapshot.confidence:0.00}");
        DrawLine($"Room active: {(engine.IsRoomActive ? "yes" : "no")}");
        DrawLine($"Active spawners: {snapshot.activeSpawnerCount}");
        GUILayout.Space(6f);

        GUILayout.Label("Score Factors", _mutedStyle);
        DrawLine($"Damage pressure: {snapshot.damagePressureScore:0.00}");
        DrawLine($"Combat intent: {snapshot.combatIntentScore:0.00}");
        DrawLine($"Movement pressure: {snapshot.movementPressureScore:0.00}");
        DrawLine($"Time pressure: {snapshot.timePressureScore:0.00}");
        GUILayout.Space(6f);

        GUILayout.Label("Director", _mutedStyle);
        DrawLine($"Strategy: {directive.strategy}");
        DrawLine($"Blend / confidence: {directive.aggressionBlend:0.00} / {directive.confidence:0.00}");
        DrawLine($"Calm relief: {(EmotionDirector.Instance.IsCalmReliefActive ? "active" : "inactive")} (charges: {EmotionDirector.Instance.PendingCalmReliefCharges})");
        DrawLine($"Spawn x{directive.spawnMultiplier:0.00}");
        DrawLine($"Enemy speed x{directive.enemySpeedMultiplier:0.00}");
        DrawLine($"Enemy cooldown x{directive.enemyAttackCooldownMultiplier:0.00}");
        DrawLine($"Vision x{directive.enemyVisionMultiplier:0.00}");
        DrawLine($"Attack delay: {directive.attackOpeningDelay:0.00}s");
        DrawLine($"Standoff / retreat: {directive.chaseStandoffDistance:0.0} / {directive.retreatDistance:0.0}");
        GUILayout.Space(6f);

        GUILayout.Label("Live totals", _mutedStyle);
        DrawLine($"Damage taken: {snapshot.damageTaken:0.0}");
        DrawLine($"Deaths: {snapshot.deathCount}");
        DrawLine($"Enemies seen: {snapshot.enemiesEncountered}");
        DrawLine($"Attacks / hits: {snapshot.attacksPerformed} / {snapshot.enemyHits}");
        DrawLine($"Effective hits: {snapshot.effectiveEnemyHits:0.00}");
        DrawLine($"Running / idle: {snapshot.timeRunning:0.0}s / {snapshot.timeIdle:0.0}s");
        DrawLine($"Avg speed: {snapshot.averageMovementSpeed:0.00}");
        DrawLine($"Current room: {snapshot.currentRoomTime:0.0}s");
        GUILayout.Space(6f);

        GUILayout.Label("Latest room", _mutedStyle);
        if (lastRoom.roomNumber <= 0)
        {
            DrawLine("No completed room yet.");
        }
        else
        {
            DrawLine($"Room: {lastRoom.roomNumber}");
            DrawLine($"Profile: {lastRoom.emotionBefore} -> {lastRoom.emotionAfter}");
            DrawLine($"Score: {lastRoom.scoreBefore:0.00} -> {lastRoom.scoreAfter:0.00}");
            DrawLine($"Duration: {lastRoom.duration:0.0}s");
            DrawLine($"Spawners: {lastRoom.spawnerCount}");
            DrawLine($"Spawns: {lastRoom.baseSpawnCount} -> {lastRoom.adjustedSpawnCount}");
            DrawLine($"Damage / deaths: {lastRoom.damageTaken:0.0} / {lastRoom.deathCount}");
            DrawLine($"Seen / attacks / hits: {lastRoom.enemiesEncountered} / {lastRoom.attacksPerformed} / {lastRoom.enemyHits}");
            DrawLine($"Move / idle: {lastRoom.timeRunning:0.0}s / {lastRoom.timeIdle:0.0}s");
        }

        GUILayout.Space(6f);
        GUILayout.Label($"Press {toggleKey} to hide/show. Press {toggleFullscreenKey} for fullscreen.", _mutedStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawLine(string text)
    {
        GUILayout.Label(text, _labelStyle);
    }

    private void EnsureStyles()
    {
        if (_panelStyle != null)
        {
            return;
        }

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 10, 10),
            alignment = TextAnchor.UpperLeft
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        _mutedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.75f, 0.85f, 1f) }
        };
    }
}
