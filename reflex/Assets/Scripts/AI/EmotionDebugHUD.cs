using UnityEngine;
using UnityEngine.InputSystem;

public class EmotionDebugHUD : MonoBehaviour
{
    private const float Width = 360f;
    private const float Padding = 12f;

    private static EmotionDebugHUD _instance;

    [SerializeField] private bool visible = true;
    [SerializeField] private Key toggleKey = Key.F3;

    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _mutedStyle;

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
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        EnsureStyles();

        EmotionEngine engine = EmotionEngine.Instance;
        EmotionProfileSnapshot snapshot = engine.CurrentSnapshot;
        EmotionRoomReport lastRoom = engine.LastRoomReport;

        Rect area = new Rect(Padding, Padding, Width, lastRoom.roomNumber > 0 ? 430f : 330f);
        GUILayout.BeginArea(area, GUIContent.none, _panelStyle);

        GUILayout.Label("EMOTION ENGINE", _titleStyle);
        DrawLine($"Profile: {snapshot.state}");
        DrawLine($"Aggression: {snapshot.aggressionScore:0.00}");
        DrawLine($"Room active: {(engine.IsRoomActive ? "yes" : "no")}");
        GUILayout.Space(6f);

        GUILayout.Label("Live totals", _mutedStyle);
        DrawLine($"Damage taken: {snapshot.damageTaken:0.0}");
        DrawLine($"Deaths: {snapshot.deathCount}");
        DrawLine($"Enemies seen: {snapshot.enemiesEncountered}");
        DrawLine($"Attacks / hits: {snapshot.attacksPerformed} / {snapshot.enemyHits}");
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
            DrawLine($"Spawns: {lastRoom.baseSpawnCount} -> {lastRoom.adjustedSpawnCount}");
            DrawLine($"Damage / deaths: {lastRoom.damageTaken:0.0} / {lastRoom.deathCount}");
            DrawLine($"Seen / attacks / hits: {lastRoom.enemiesEncountered} / {lastRoom.attacksPerformed} / {lastRoom.enemyHits}");
            DrawLine($"Move / idle: {lastRoom.timeRunning:0.0}s / {lastRoom.timeIdle:0.0}s");
        }

        GUILayout.Space(6f);
        GUILayout.Label($"Press {toggleKey} to hide/show", _mutedStyle);
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
            normal = { textColor = Color.white }
        };

        _mutedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.75f, 0.85f, 1f) }
        };
    }
}
