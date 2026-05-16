using System;
using System.Collections.Generic;
using System.Text;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class GeneratedLevelConnection
{
    public int doorIndex;
    public int destinationNodeId;
}

[Serializable]
public class GeneratedLevelNode
{
    public int id;
    public int depth;
    public string sceneName;
    public readonly List<GeneratedLevelConnection> connections = new List<GeneratedLevelConnection>();
}

public struct LevelDoorRoute
{
    public int DoorIndex;
    public int DestinationNodeId;
    public int DestinationDepth;
    public string DestinationSceneName;
    public string DestinationLabel;
}

[DefaultExecutionOrder(-1000)]
public class LevelRunManager : MonoBehaviour
{
    public static event Action<int, int, string> LevelEntered;
    public static event Action<int, int, string> LevelCleared;

    private static LevelRunManager _instance;

    [Header("Profile")]
    [SerializeField] private LevelGenerationProfile generationProfile;

    [Header("Scene Pool")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string[] roomSceneNames =
    {   
        "Level_1_Scene",
        "Level_2_Scene",
        "Level_3_Scene",
        "Level_4_scene",
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
    [SerializeField] private bool autoAdvanceWhenNoDoors = true;

    [Header("Progression")]
    [SerializeField] private bool unlockCurrentLevelAfterClear = true;
    [SerializeField] private bool unlockLevelsWithoutSpawners = true;
    [SerializeField] private bool disableSpawnersAfterLevelClear = true;

    [Header("Debug")]
    [SerializeField] private bool logGeneratedGraph = true;
    [SerializeField] private bool logDoorBinding = true;
    [SerializeField] private bool logProgression = true;

    private readonly Dictionary<int, GeneratedLevelNode> _nodesById = new Dictionary<int, GeneratedLevelNode>();
    private readonly HashSet<int> _clearedNodeIds = new HashSet<int>();
    private LevelGenerationRuntimeOverrides _runtimeOverrides;
    private int _currentNodeId;
    private int _pendingNodeId = -1;
    private int _deepestDepthReached;
    private int _activeSeed;
    private bool _regenerateOnNextLobbyLoad;
    private PlayerManager _persistentPlayer;

    public static bool HasInstance
    {
        get { return _instance != null; }
    }

    public static LevelRunManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<LevelRunManager>();

                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("Level Run Manager");
                    _instance = managerObject.AddComponent<LevelRunManager>();
                }
            }

            return _instance;
        }
    }

    public bool AreDoorsUnlocked
    {
        get
        {
            return !LockDoorsWhileRoomActive ||
                   IsCurrentNodeCleared ||
                   !EmotionEngine.HasInstance ||
                   !EmotionEngine.Instance.IsRoomActive;
        }
    }

    public int ActiveSeed
    {
        get { return _activeSeed; }
    }

    public int CurrentLevelDepth
    {
        get
        {
            return _nodesById.TryGetValue(_currentNodeId, out GeneratedLevelNode node) ? node.depth : 0;
        }
    }

    public int DeepestDepthReached
    {
        get { return _deepestDepthReached; }
    }

    public bool IsCurrentNodeCleared
    {
        get { return _clearedNodeIds.Contains(_currentNodeId); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _ = Instance;
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
        LoadDefaultProfileIfNeeded();
        GenerateNewRun();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnEnable()
    {
        EmotionEngine.RoomEvaluated += HandleRoomEvaluated;
    }

    private void OnDisable()
    {
        EmotionEngine.RoomEvaluated -= HandleRoomEvaluated;
    }

    private void Start()
    {
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }
    }

    public void GenerateNewRun()
    {
        _nodesById.Clear();
        _clearedNodeIds.Clear();
        _currentNodeId = 0;
        _pendingNodeId = -1;
        _deepestDepthReached = 0;
        _activeSeed = CreateRunSeed();

        System.Random random = new System.Random(_activeSeed);
        Dictionary<string, int> roomUseCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        GeneratedLevelNode lobbyNode = CreateNode(0, 0, LobbySceneName);
        _nodesById.Add(lobbyNode.id, lobbyNode);
        _clearedNodeIds.Add(lobbyNode.id);

        string previousScene = LobbySceneName;
        for (int depth = 1; depth <= GeneratedRoomCount; depth++)
        {
            string sceneName = PickRoomScene(random, previousScene, depth, roomUseCounts);
            GeneratedLevelNode node = CreateNode(depth, depth, sceneName);
            _nodesById.Add(node.id, node);
            IncrementRoomUseCount(roomUseCounts, sceneName);
            previousScene = sceneName;
        }

        BuildForwardConnections(random);
        ConnectLastRoomToLobby();

        if (LogGeneratedGraph)
        {
            Debug.Log(BuildGraphLog());
        }

        ResetPersistentPlayerRunState();
    }

    public void TravelTo(LevelDoorRoute route)
    {
        if (!AreDoorsUnlocked)
        {
            Debug.Log("Door is locked until the active room is clear.");
            return;
        }

        if (!_nodesById.TryGetValue(route.DestinationNodeId, out GeneratedLevelNode destination))
        {
            Debug.LogError("Generated door route points to a missing level node: " + route.DestinationNodeId);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(destination.sceneName))
        {
            Debug.LogError("Cannot load scene '" + destination.sceneName + "'. Add it to Build Settings.");
            return;
        }

        bool returningToLobby = destination.id == 0 && _currentNodeId != 0;
        _pendingNodeId = destination.id;
        _regenerateOnNextLobbyLoad = RegenerateWhenReturningToLobby && returningToLobby;

        SceneManager.LoadScene(destination.sceneName, LoadSceneMode.Single);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid())
        {
            return;
        }

        if (_pendingNodeId >= 0)
        {
            _currentNodeId = _pendingNodeId;
            _pendingNodeId = -1;
        }
        else
        {
            SyncCurrentNodeToLoadedScene(scene.name);
        }

        if (_regenerateOnNextLobbyLoad && SceneNameEquals(scene.name, LobbySceneName))
        {
            _regenerateOnNextLobbyLoad = false;
            GenerateNewRun();
        }

        EnsurePersistentPlayer(scene);
        RefreshCurrentLevelProgression(scene);
        AnnounceLevelEntered();

        if (AutoBindSceneDoors)
        {
            ConfigureDoorsForCurrentScene(scene.name);
        }
    }

    private void EnsurePersistentPlayer(Scene loadedScene)
    {
        PlayerManager[] players = FindObjectsByType<PlayerManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        PlayerManager scenePlayer = null;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].gameObject.scene == loadedScene)
            {
                scenePlayer = players[i];
                break;
            }
        }

        if (_persistentPlayer == null)
        {
            _persistentPlayer = scenePlayer != null ? scenePlayer : GetFirstPlayer(players);

            if (_persistentPlayer != null)
            {
                DontDestroyOnLoad(_persistentPlayer.gameObject);
                RefreshPlayerSceneReferences(_persistentPlayer);
            }

            return;
        }

        if (scenePlayer != null && scenePlayer != _persistentPlayer)
        {
            MovePersistentPlayerTo(scenePlayer.transform);

            Destroy(scenePlayer.gameObject);
        }
        else if (scenePlayer == null && TryGetSpawnPoint(loadedScene, out LevelPlayerSpawnPoint spawnPoint))
        {
            MovePersistentPlayerTo(spawnPoint.transform);
        }

        RefreshPlayerSceneReferences(_persistentPlayer);
    }

    private void MovePersistentPlayerTo(Transform target)
    {
        if (_persistentPlayer == null || target == null)
        {
            return;
        }

        CharacterController controller = _persistentPlayer.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
        {
            controller.enabled = false;
        }

        _persistentPlayer.transform.SetPositionAndRotation(target.position, target.rotation);

        if (controllerWasEnabled)
        {
            controller.enabled = true;
        }
    }

    private bool TryGetSpawnPoint(Scene scene, out LevelPlayerSpawnPoint spawnPoint)
    {
        LevelPlayerSpawnPoint[] spawnPoints = FindObjectsByType<LevelPlayerSpawnPoint>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        spawnPoint = null;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            LevelPlayerSpawnPoint candidate = spawnPoints[i];
            if (candidate == null || candidate.gameObject.scene != scene)
            {
                continue;
            }

            if (spawnPoint == null ||
                candidate.Priority > spawnPoint.Priority ||
                (candidate.Priority == spawnPoint.Priority && candidate.DefaultSpawn && !spawnPoint.DefaultSpawn))
            {
                spawnPoint = candidate;
            }
        }

        return spawnPoint != null;
    }

    private PlayerManager GetFirstPlayer(PlayerManager[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                return players[i];
            }
        }

        return null;
    }

    private void RefreshPlayerSceneReferences(PlayerManager player)
    {
        Camera sceneCamera = BindSceneCamera(player.transform);
        RefreshPerspectiveCameras(sceneCamera);

        PlayerMovementManagement movement = player.GetComponent<PlayerMovementManagement>();
        if (movement != null)
        {
            movement.RefreshSceneReferences();
        }

        PlayerOcclusionFader occlusionFader = player.GetComponent<PlayerOcclusionFader>();
        if (occlusionFader == null)
        {
            occlusionFader = player.gameObject.AddComponent<PlayerOcclusionFader>();
        }

        occlusionFader.RefreshForScene(sceneCamera);
    }

    private Camera BindSceneCamera(Transform playerTarget)
    {
        if (playerTarget == null)
        {
            return Camera.main;
        }

        Camera sceneCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        CinemachineCamera[] cinemachineCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        if (cinemachineCameras.Length > 0)
        {
            if (sceneCamera != null && sceneCamera.GetComponent<CinemachineBrain>() == null)
            {
                sceneCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            for (int i = 0; i < cinemachineCameras.Length; i++)
            {
                CinemachineCamera cinemachineCamera = cinemachineCameras[i];
                cinemachineCamera.Follow = playerTarget;
                cinemachineCamera.LookAt = null;
                cinemachineCamera.OnTargetObjectWarped(playerTarget, Vector3.zero);
            }

            if (sceneCamera != null && sceneCamera.TryGetComponent(out SceneCameraFollow fallbackFollow))
            {
                fallbackFollow.enabled = false;
            }
        }
        else if (sceneCamera != null)
        {
            SceneCameraFollow fallbackFollow = sceneCamera.GetComponent<SceneCameraFollow>();
            if (fallbackFollow == null)
            {
                fallbackFollow = sceneCamera.gameObject.AddComponent<SceneCameraFollow>();
            }

            fallbackFollow.enabled = true;
            fallbackFollow.Configure(playerTarget, true);
        }

        return sceneCamera;
    }

    private void RefreshPerspectiveCameras(Camera sceneCamera)
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        PerspectiveChanger[] perspectiveChangers = FindObjectsByType<PerspectiveChanger>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < perspectiveChangers.Length; i++)
        {
            perspectiveChangers[i].SetCamera(sceneCamera);
        }

        Billboarding[] billboards = FindObjectsByType<Billboarding>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < billboards.Length; i++)
        {
            billboards[i].SetCamera(sceneCamera);
        }
    }

    private void RefreshCurrentLevelProgression(Scene scene)
    {
        if (!_nodesById.TryGetValue(_currentNodeId, out GeneratedLevelNode node))
        {
            return;
        }

        _deepestDepthReached = Mathf.Max(_deepestDepthReached, node.depth);

        LevelRoomDefinition roomDefinition = GetRoomDefinition(scene);
        LevelRoomClearRule clearRule = roomDefinition != null
            ? roomDefinition.ClearRule
            : LevelRoomClearRule.UseGlobalDefaults;

        if (node.id == 0 || clearRule == LevelRoomClearRule.AlwaysUnlocked)
        {
            MarkCurrentNodeCleared("room configured as always unlocked");
            return;
        }

        if (clearRule == LevelRoomClearRule.Manual)
        {
            return;
        }

        bool shouldUnlockWithoutSpawners =
            clearRule == LevelRoomClearRule.EnemySpawners ||
            (clearRule == LevelRoomClearRule.UseGlobalDefaults && UnlockLevelsWithoutSpawners);

        if (shouldUnlockWithoutSpawners && !SceneHasActiveSpawner(scene))
        {
            MarkCurrentNodeCleared("no active spawners");
        }
    }

    private bool SceneHasActiveSpawner(Scene scene)
    {
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null && spawners[i].gameObject.scene == scene)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleRoomEvaluated(EmotionRoomReport report)
    {
        if (!UnlockCurrentLevelAfterClear)
        {
            return;
        }

        MarkCurrentNodeCleared("room cleared");
    }

    public void MarkCurrentLevelClearedFromScene(string source)
    {
        string reason = string.IsNullOrWhiteSpace(source) ? "scene script requested clear" : source + " requested clear";
        MarkCurrentNodeCleared(reason);
    }

    private void MarkCurrentNodeCleared(string reason)
    {
        if (!_nodesById.TryGetValue(_currentNodeId, out GeneratedLevelNode node) ||
            !_clearedNodeIds.Add(_currentNodeId))
        {
            return;
        }

        LevelCleared?.Invoke(node.id, node.depth, node.sceneName);

        if (ShouldDisableSpawnersAfterClear() && node.id != 0)
        {
            DisableCurrentSceneSpawners();
        }

        if (LogProgression)
        {
            Debug.Log("Level cleared: node " + node.id + " depth " + node.depth + " (" + node.sceneName + ") because " + reason + ".");
        }

        List<LevelDoor> doors = LevelDoorAutoBinder.FindOrCreateDoors();
        TryAutoAdvanceWithoutDoors(node, doors.Count, "level cleared");
    }

    private bool ShouldDisableSpawnersAfterClear()
    {
        LevelRoomDefinition roomDefinition = GetRoomDefinition(SceneManager.GetActiveScene());
        if (roomDefinition != null && roomDefinition.HasSpawnerDisableOverride)
        {
            return roomDefinition.DisableSpawnersAfterClear;
        }

        return DisableSpawnersAfterLevelClear;
    }

    private void DisableCurrentSceneSpawners()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < spawners.Length; i++)
        {
            EnemySpawner spawner = spawners[i];
            if (spawner != null && spawner.gameObject.scene == activeScene)
            {
                spawner.enabled = false;
            }
        }
    }

    private void AnnounceLevelEntered()
    {
        if (!_nodesById.TryGetValue(_currentNodeId, out GeneratedLevelNode node))
        {
            return;
        }

        LevelEntered?.Invoke(node.id, node.depth, node.sceneName);

        if (LogProgression)
        {
            Debug.Log("Level entered: node " + node.id + " depth " + node.depth + " (" + node.sceneName + "). Cleared: " + IsCurrentNodeCleared);
        }
    }

    private void ConfigureDoorsForCurrentScene(string sceneName)
    {
        List<LevelDoor> doors = LevelDoorAutoBinder.FindOrCreateDoors();

        if (!_nodesById.TryGetValue(_currentNodeId, out GeneratedLevelNode currentNode))
        {
            ClearDoors(doors);
            return;
        }

        if (!SceneNameEquals(currentNode.sceneName, sceneName))
        {
            ClearDoors(doors);
            return;
        }

        for (int i = 0; i < doors.Count; i++)
        {
            if (currentNode.connections.Count == 1)
            {
                doors[i].Configure(BuildDoorRoute(currentNode.connections[0]));
            }
            else if (i < currentNode.connections.Count)
            {
                doors[i].Configure(BuildDoorRoute(currentNode.connections[i]));
            }
            else
            {
                doors[i].ClearRoute(i);
            }
        }

        if (LogDoorBinding && doors.Count > 0)
        {
            Debug.Log("Bound " + Mathf.Min(doors.Count, currentNode.connections.Count) +
                      " generated door route(s) in " + sceneName + " from node " + currentNode.id + ".");
        }

        TryAutoAdvanceWithoutDoors(currentNode, doors.Count, "scene has no generated door candidates");
    }

    private void ClearDoors(List<LevelDoor> doors)
    {
        for (int i = 0; i < doors.Count; i++)
        {
            doors[i].ClearRoute(i);
        }
    }

    private LevelDoorRoute BuildDoorRoute(GeneratedLevelConnection connection)
    {
        GeneratedLevelNode destination = _nodesById[connection.destinationNodeId];
        return new LevelDoorRoute
        {
            DoorIndex = connection.doorIndex,
            DestinationNodeId = destination.id,
            DestinationDepth = destination.depth,
            DestinationSceneName = destination.sceneName,
            DestinationLabel = GetDestinationLabel(destination)
        };
    }

    private string GetDestinationLabel(GeneratedLevelNode destination)
    {
        if (destination.id == 0)
        {
            return "Lobby";
        }

        return "Room " + destination.depth + " - " + destination.sceneName;
    }

    private void SyncCurrentNodeToLoadedScene(string sceneName)
    {
        if (_nodesById.TryGetValue(_currentNodeId, out GeneratedLevelNode currentNode) &&
            SceneNameEquals(currentNode.sceneName, sceneName))
        {
            return;
        }

        if (SceneNameEquals(sceneName, LobbySceneName))
        {
            _currentNodeId = 0;
            return;
        }

        foreach (GeneratedLevelNode node in _nodesById.Values)
        {
            if (SceneNameEquals(node.sceneName, sceneName))
            {
                _currentNodeId = node.id;
                return;
            }
        }
    }

    private GeneratedLevelNode CreateNode(int id, int depth, string sceneName)
    {
        return new GeneratedLevelNode
        {
            id = id,
            depth = depth,
            sceneName = sceneName
        };
    }

    private void BuildForwardConnections(System.Random random)
    {
        for (int nodeId = 0; nodeId < GeneratedRoomCount; nodeId++)
        {
            GeneratedLevelNode node = _nodesById[nodeId];
            List<int> possibleDestinations = GetPossibleForwardDestinations(nodeId);

            if (possibleDestinations.Count == 0)
            {
                continue;
            }

            int cappedMin = Mathf.Clamp(MinDoorChoices, 1, possibleDestinations.Count);
            int cappedMax = Mathf.Clamp(MaxDoorChoices, cappedMin, possibleDestinations.Count);
            int exitCount = random.Next(cappedMin, cappedMax + 1);

            AddConnection(node, nodeId + 1);

            while (node.connections.Count < exitCount)
            {
                int destinationId = possibleDestinations[random.Next(possibleDestinations.Count)];

                if (!HasConnection(node, destinationId))
                {
                    AddConnection(node, destinationId);
                }
            }
        }
    }

    private List<int> GetPossibleForwardDestinations(int nodeId)
    {
        List<int> destinations = new List<int>();
        int maxDestination = Mathf.Min(GeneratedRoomCount, nodeId + MaxForwardRoomSkip);

        for (int destinationId = nodeId + 1; destinationId <= maxDestination; destinationId++)
        {
            destinations.Add(destinationId);
        }

        return destinations;
    }

    private void ConnectLastRoomToLobby()
    {
        GeneratedLevelNode finalRoom = _nodesById[GeneratedRoomCount];
        AddConnection(finalRoom, 0);
    }

    private void AddConnection(GeneratedLevelNode source, int destinationNodeId)
    {
        source.connections.Add(new GeneratedLevelConnection
        {
            doorIndex = source.connections.Count,
            destinationNodeId = destinationNodeId
        });
    }

    private bool HasConnection(GeneratedLevelNode source, int destinationNodeId)
    {
        for (int i = 0; i < source.connections.Count; i++)
        {
            if (source.connections[i].destinationNodeId == destinationNodeId)
            {
                return true;
            }
        }

        return false;
    }

    private int CreateRunSeed()
    {
        int configuredSeed = FixedSeed;
        if (configuredSeed != 0)
        {
            return configuredSeed;
        }

        unchecked
        {
            int seed = Guid.NewGuid().GetHashCode();
            seed = (seed * 397) ^ Environment.TickCount;
            seed = (seed * 397) ^ DateTime.UtcNow.Ticks.GetHashCode();
            return seed != 0 ? seed : 1;
        }
    }

    private string PickRoomScene(System.Random random, string previousScene, int depth, Dictionary<string, int> roomUseCounts)
    {
        if (TryPickProfileRoomScene(random, previousScene, depth, roomUseCounts, out string profileSceneName))
        {
            return profileSceneName;
        }

        List<string> validRooms = new List<string>();

        for (int i = 0; i < roomSceneNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(roomSceneNames[i]))
            {
                validRooms.Add(roomSceneNames[i]);
            }
        }

        if (validRooms.Count == 0)
        {
            return LobbySceneName;
        }

        if (validRooms.Count == 1)
        {
            return validRooms[0];
        }

        List<string> repeatSafeRooms = new List<string>();
        for (int i = 0; i < validRooms.Count; i++)
        {
            if (!SceneNameEquals(validRooms[i], previousScene))
            {
                repeatSafeRooms.Add(validRooms[i]);
            }
        }

        List<string> pickPool = repeatSafeRooms.Count > 0 ? repeatSafeRooms : validRooms;
        pickPool = KeepLeastUsedRoomNames(pickPool, roomUseCounts);

        for (int attempt = 0; attempt < 8; attempt++)
        {
            string candidate = pickPool[random.Next(pickPool.Count)];
            if (!SceneNameEquals(candidate, previousScene))
            {
                return candidate;
            }
        }

        return pickPool[random.Next(pickPool.Count)];
    }

    private bool TryPickProfileRoomScene(System.Random random, string previousScene, int depth, Dictionary<string, int> roomUseCounts, out string sceneName)
    {
        sceneName = null;

        if (generationProfile == null || generationProfile.RoomScenes == null)
        {
            return false;
        }

        List<LevelSceneCandidate> candidates = new List<LevelSceneCandidate>();
        List<LevelSceneCandidate> repeatSafeCandidates = new List<LevelSceneCandidate>();

        for (int i = 0; i < generationProfile.RoomScenes.Length; i++)
        {
            LevelSceneCandidate candidate = generationProfile.RoomScenes[i];
            if (candidate == null || !candidate.IsValidForDepth(depth))
            {
                continue;
            }

            candidates.Add(candidate);

            if (candidate.CanRepeatConsecutively || !SceneNameEquals(candidate.SceneName, previousScene))
            {
                repeatSafeCandidates.Add(candidate);
            }
        }

        List<LevelSceneCandidate> pickPool = repeatSafeCandidates.Count > 0 ? repeatSafeCandidates : candidates;
        pickPool = KeepLeastUsedCandidates(pickPool, roomUseCounts);
        if (pickPool.Count == 0)
        {
            return false;
        }

        int totalWeight = 0;
        for (int i = 0; i < pickPool.Count; i++)
        {
            totalWeight += pickPool[i].Weight;
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int roll = random.Next(0, totalWeight);
        for (int i = 0; i < pickPool.Count; i++)
        {
            roll -= pickPool[i].Weight;
            if (roll < 0)
            {
                sceneName = pickPool[i].SceneName;
                return true;
            }
        }

        sceneName = pickPool[pickPool.Count - 1].SceneName;
        return true;
    }

    private List<LevelSceneCandidate> KeepLeastUsedCandidates(List<LevelSceneCandidate> candidates, Dictionary<string, int> roomUseCounts)
    {
        if (candidates.Count <= 1)
        {
            return candidates;
        }

        int lowestUseCount = int.MaxValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            lowestUseCount = Mathf.Min(lowestUseCount, GetRoomUseCount(roomUseCounts, candidates[i].SceneName));
        }

        List<LevelSceneCandidate> leastUsedCandidates = new List<LevelSceneCandidate>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (GetRoomUseCount(roomUseCounts, candidates[i].SceneName) == lowestUseCount)
            {
                leastUsedCandidates.Add(candidates[i]);
            }
        }

        return leastUsedCandidates;
    }

    private List<string> KeepLeastUsedRoomNames(List<string> roomNames, Dictionary<string, int> roomUseCounts)
    {
        if (roomNames.Count <= 1)
        {
            return roomNames;
        }

        int lowestUseCount = int.MaxValue;
        for (int i = 0; i < roomNames.Count; i++)
        {
            lowestUseCount = Mathf.Min(lowestUseCount, GetRoomUseCount(roomUseCounts, roomNames[i]));
        }

        List<string> leastUsedRooms = new List<string>();
        for (int i = 0; i < roomNames.Count; i++)
        {
            if (GetRoomUseCount(roomUseCounts, roomNames[i]) == lowestUseCount)
            {
                leastUsedRooms.Add(roomNames[i]);
            }
        }

        return leastUsedRooms;
    }

    private int GetRoomUseCount(Dictionary<string, int> roomUseCounts, string sceneName)
    {
        if (roomUseCounts == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return 0;
        }

        return roomUseCounts.TryGetValue(sceneName, out int useCount) ? useCount : 0;
    }

    private void IncrementRoomUseCount(Dictionary<string, int> roomUseCounts, string sceneName)
    {
        if (roomUseCounts == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        roomUseCounts[sceneName] = GetRoomUseCount(roomUseCounts, sceneName) + 1;
    }

    private string BuildGraphLog()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Generated level run seed: " + _activeSeed);

        for (int i = 0; i <= GeneratedRoomCount; i++)
        {
            GeneratedLevelNode node = _nodesById[i];
            builder.Append("Node ");
            builder.Append(node.id);
            builder.Append(" [");
            builder.Append(node.sceneName);
            builder.Append("] -> ");

            if (node.connections.Count == 0)
            {
                builder.Append("none");
            }

            for (int connectionIndex = 0; connectionIndex < node.connections.Count; connectionIndex++)
            {
                GeneratedLevelConnection connection = node.connections[connectionIndex];
                GeneratedLevelNode destination = _nodesById[connection.destinationNodeId];

                if (connectionIndex > 0)
                {
                    builder.Append(", ");
                }

                builder.Append("Door ");
                builder.Append(connection.doorIndex);
                builder.Append(": ");
                builder.Append(destination.sceneName);
                builder.Append(" (node ");
                builder.Append(destination.id);
                builder.Append(")");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private bool SceneNameEquals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private void TryAutoAdvanceWithoutDoors(GeneratedLevelNode node, int availableDoorCount, string reason)
    {
        if (!AutoAdvanceWhenNoDoors || node == null || availableDoorCount > 0)
        {
            return;
        }

        if (!SceneNameEquals(SceneManager.GetActiveScene().name, node.sceneName))
        {
            return;
        }

        if (node.connections.Count != 1)
        {
            if (LogProgression && node.connections.Count > 1)
            {
                Debug.LogWarning("Auto-advance is disabled in scene '" + node.sceneName +
                                 "' because it has no generated doors and multiple routes.");
            }

            return;
        }

        // Non-lobby nodes should only auto-advance after the room is cleared.
        if (node.id != 0 && !IsCurrentNodeCleared)
        {
            return;
        }

        if (LogProgression)
        {
            Debug.Log("Auto-advancing from node " + node.id + " (" + node.sceneName +
                      ") because " + reason + ".");
        }

        TravelTo(BuildDoorRoute(node.connections[0]));
    }

    private void ResetPersistentPlayerRunState()
    {
        if (_persistentPlayer != null)
        {
            _persistentPlayer.ResetTemporaryRunState();
        }
    }

    private void LoadDefaultProfileIfNeeded()
    {
        if (generationProfile == null)
        {
            generationProfile = Resources.Load<LevelGenerationProfile>("LevelGeneration/Default Level Generation Profile");
        }
    }

    public void SetGenerationProfile(LevelGenerationProfile profile, bool regenerateRun)
    {
        generationProfile = profile;
        if (regenerateRun)
        {
            GenerateNewRun();
            ConfigureDoorsForCurrentScene(SceneManager.GetActiveScene().name);
        }
    }

    public void ApplyRuntimeOverrides(LevelGenerationRuntimeOverrides overrides, bool regenerateRun)
    {
        _runtimeOverrides = overrides;
        if (regenerateRun)
        {
            GenerateNewRun();
            ConfigureDoorsForCurrentScene(SceneManager.GetActiveScene().name);
        }
    }

    public void ClearRuntimeOverrides(bool regenerateRun)
    {
        _runtimeOverrides = default;
        if (regenerateRun)
        {
            GenerateNewRun();
            ConfigureDoorsForCurrentScene(SceneManager.GetActiveScene().name);
        }
    }

    private LevelRoomDefinition GetRoomDefinition(Scene scene)
    {
        LevelRoomDefinition[] definitions = FindObjectsByType<LevelRoomDefinition>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] != null && definitions[i].gameObject.scene == scene)
            {
                return definitions[i];
            }
        }

        return null;
    }

    private string LobbySceneName => generationProfile != null ? generationProfile.LobbySceneName : lobbySceneName;

    private int GeneratedRoomCount => _runtimeOverrides.overrideGeneratedRoomCount
        ? Mathf.Max(1, _runtimeOverrides.generatedRoomCount)
        : generationProfile != null ? generationProfile.GeneratedRoomCount : Mathf.Max(1, generatedRoomCount);

    private int MinDoorChoices => _runtimeOverrides.overrideDoorChoices
        ? Mathf.Max(1, _runtimeOverrides.minDoorChoices)
        : generationProfile != null ? generationProfile.MinDoorChoices : Mathf.Max(1, minDoorChoices);

    private int MaxDoorChoices => _runtimeOverrides.overrideDoorChoices
        ? Mathf.Max(MinDoorChoices, _runtimeOverrides.maxDoorChoices)
        : generationProfile != null ? generationProfile.MaxDoorChoices : Mathf.Max(MinDoorChoices, maxDoorChoices);

    private int MaxForwardRoomSkip => _runtimeOverrides.overrideMaxForwardRoomSkip
        ? Mathf.Max(1, _runtimeOverrides.maxForwardRoomSkip)
        : generationProfile != null ? generationProfile.MaxForwardRoomSkip : Mathf.Max(1, maxForwardRoomSkip);

    private int FixedSeed => _runtimeOverrides.overrideFixedSeed
        ? _runtimeOverrides.fixedSeed
        : generationProfile != null ? generationProfile.FixedSeed : fixedSeed;

    private bool RegenerateWhenReturningToLobby => generationProfile != null ? generationProfile.RegenerateWhenReturningToLobby : regenerateWhenReturningToLobby;
    private bool LockDoorsWhileRoomActive => generationProfile != null ? generationProfile.LockDoorsWhileRoomActive : lockDoorsWhileRoomActive;
    private bool AutoBindSceneDoors => generationProfile != null ? generationProfile.AutoBindSceneDoors : autoBindSceneDoors;
    private bool AutoAdvanceWhenNoDoors => generationProfile != null ? generationProfile.AutoAdvanceWhenNoDoors : autoAdvanceWhenNoDoors;
    private bool UnlockCurrentLevelAfterClear => generationProfile != null ? generationProfile.UnlockCurrentLevelAfterClear : unlockCurrentLevelAfterClear;
    private bool UnlockLevelsWithoutSpawners => generationProfile != null ? generationProfile.UnlockLevelsWithoutSpawners : unlockLevelsWithoutSpawners;
    private bool DisableSpawnersAfterLevelClear => generationProfile != null ? generationProfile.DisableSpawnersAfterLevelClear : disableSpawnersAfterLevelClear;
    private bool LogGeneratedGraph => generationProfile != null ? generationProfile.LogGeneratedGraph : logGeneratedGraph;
    private bool LogDoorBinding => generationProfile != null ? generationProfile.LogDoorBinding : logDoorBinding;
    private bool LogProgression => generationProfile != null ? generationProfile.LogProgression : logProgression;
}

public class SceneCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-14.5f, 14.3f, -14.5f);
    [SerializeField] private Vector3 eulerAngles = new Vector3(35f, 45f, 0f);
    [SerializeField, Min(0f)] private float followSharpness = 18f;

    public void Configure(Transform followTarget, bool snap)
    {
        target = followTarget;

        if (snap)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;
        float t = followSharpness <= 0f ? 1f : 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Euler(eulerAngles);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(eulerAngles);
    }
}
