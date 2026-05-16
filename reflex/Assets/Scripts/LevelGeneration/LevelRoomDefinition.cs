using UnityEngine;

[DisallowMultipleComponent]
public class LevelRoomDefinition : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private LevelRoomKind roomKind = LevelRoomKind.Combat;
    [SerializeField] private LevelRoomClearRule clearRule = LevelRoomClearRule.UseGlobalDefaults;
    [SerializeField] private bool overrideDisableSpawnersAfterClear;
    [SerializeField] private bool disableSpawnersAfterClear = true;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.scene.name : displayName;
    public LevelRoomKind RoomKind => roomKind;
    public LevelRoomClearRule ClearRule => clearRule;
    public bool HasSpawnerDisableOverride => overrideDisableSpawnersAfterClear;
    public bool DisableSpawnersAfterClear => disableSpawnersAfterClear;

    public void MarkCleared()
    {
        if (LevelRunManager.HasInstance)
        {
            LevelRunManager.Instance.MarkCurrentLevelClearedFromScene(DisplayName);
        }
    }
}
