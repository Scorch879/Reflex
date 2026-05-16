using System;
using UnityEngine;

public enum LevelRoomKind
{
    Combat,
    Reward,
    Rest,
    Boss,
    Utility
}

public enum LevelRoomClearRule
{
    UseGlobalDefaults,
    AlwaysUnlocked,
    EnemySpawners,
    Manual
}

[Serializable]
public class LevelSceneCandidate
{
    [SerializeField] private string sceneName;
    [SerializeField, Min(0)] private int weight = 1;
    [SerializeField, Min(1)] private int minDepth = 1;
    [SerializeField, Min(0)] private int maxDepth;
    [SerializeField] private bool canRepeatConsecutively;
    [SerializeField] private LevelRoomKind roomKind = LevelRoomKind.Combat;

    public string SceneName => sceneName;
    public int Weight => Mathf.Max(0, weight);
    public int MinDepth => Mathf.Max(1, minDepth);
    public int MaxDepth => Mathf.Max(0, maxDepth);
    public bool CanRepeatConsecutively => canRepeatConsecutively;
    public LevelRoomKind RoomKind => roomKind;

    public bool IsValidForDepth(int depth)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || Weight <= 0 || depth < MinDepth)
        {
            return false;
        }

        return MaxDepth <= 0 || depth <= MaxDepth;
    }
}

[Serializable]
public struct LevelGenerationRuntimeOverrides
{
    public bool overrideGeneratedRoomCount;
    [Min(1)] public int generatedRoomCount;

    public bool overrideDoorChoices;
    [Min(1)] public int minDoorChoices;
    [Min(1)] public int maxDoorChoices;

    public bool overrideMaxForwardRoomSkip;
    [Min(1)] public int maxForwardRoomSkip;

    public bool overrideFixedSeed;
    public int fixedSeed;
}

[CreateAssetMenu(fileName = "Level Generation Profile", menuName = "Reflex/Level Generation Profile")]
public class LevelGenerationProfile : ScriptableObject
{
    [Header("Scene Pool")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private LevelSceneCandidate[] roomScenes =
    {
        new LevelSceneCandidate()
    };

    [Header("Generated Run")]
    [SerializeField, Min(1)] private int generatedRoomCount = 8;
    [SerializeField, Min(1)] private int minDoorChoices = 1;
    [SerializeField, Min(1)] private int maxDoorChoices = 3;
    [SerializeField, Min(1)] private int maxForwardRoomSkip = 3;
    [SerializeField] private int fixedSeed;
    [SerializeField] private bool regenerateWhenReturningToLobby = true;

    [Header("Door Rules")]
    [SerializeField] private bool lockDoorsWhileRoomActive = true;
    [SerializeField] private bool autoBindSceneDoors = true;

    [Header("Progression")]
    [SerializeField] private bool unlockCurrentLevelAfterClear = true;
    [SerializeField] private bool unlockLevelsWithoutSpawners = true;
    [SerializeField] private bool disableSpawnersAfterLevelClear = true;

    [Header("Debug")]
    [SerializeField] private bool logGeneratedGraph = true;
    [SerializeField] private bool logDoorBinding = true;
    [SerializeField] private bool logProgression = true;

    public string LobbySceneName => string.IsNullOrWhiteSpace(lobbySceneName) ? "Lobby" : lobbySceneName;
    public LevelSceneCandidate[] RoomScenes => roomScenes;
    public int GeneratedRoomCount => Mathf.Max(1, generatedRoomCount);
    public int MinDoorChoices => Mathf.Max(1, minDoorChoices);
    public int MaxDoorChoices => Mathf.Max(MinDoorChoices, maxDoorChoices);
    public int MaxForwardRoomSkip => Mathf.Max(1, maxForwardRoomSkip);
    public int FixedSeed => fixedSeed;
    public bool RegenerateWhenReturningToLobby => regenerateWhenReturningToLobby;
    public bool LockDoorsWhileRoomActive => lockDoorsWhileRoomActive;
    public bool AutoBindSceneDoors => autoBindSceneDoors;
    public bool UnlockCurrentLevelAfterClear => unlockCurrentLevelAfterClear;
    public bool UnlockLevelsWithoutSpawners => unlockLevelsWithoutSpawners;
    public bool DisableSpawnersAfterLevelClear => disableSpawnersAfterLevelClear;
    public bool LogGeneratedGraph => logGeneratedGraph;
    public bool LogDoorBinding => logDoorBinding;
    public bool LogProgression => logProgression;
}
