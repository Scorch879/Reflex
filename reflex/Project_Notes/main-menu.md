## Main Menu

### Current State
- `Assets/Scenes/Main Menu.unity` is the first enabled scene in Build Settings.
- The authored Main Menu canvas is visible by default and has `MainMenuManager` attached.
- `Play btn` loads `Lobby`.
- `SETTINGS` opens a runtime music settings overlay.
- `Quit` exits the player build and stops Play Mode in the Unity Editor.
- The pause-menu `Return to Menu` button loads this scene and clears pause state before the transition.
- Startup no longer runs the temporary loading overlay's global shader warmup by default, preventing the build from exiting while `Compiling shaders...` is visible over the menu.

### Audio Settings
- Main Menu uses the existing persistent `BackgroundMusic` system.
- Main Menu references `Assets/Audio/REflex.mp3`.
- Mute and volume settings are saved through the same `PlayerPrefs` keys used by in-game pause settings.

### Files
- `Assets/Scripts/Game/MainMenuManager.cs`
- `Assets/Scripts/Game/BackgroundMusic.cs`
- `Assets/Scripts/Game/PauseManager.cs`
- `Assets/Scripts/Visuals/UI/InGameUIManager.cs`
- `Assets/Scripts/Visuals/UI/TemporaryLoadingUI.cs`
- `Assets/Scenes/Main Menu.unity`

### Testing Status
- C# build passes with `dotnet build Assembly-CSharp.csproj -nologo`.
- Unity player rebuild/run validation is still required to confirm the Main Menu no longer crashes at startup.
- Unity Play Mode validation is still required for the full Main Menu -> Lobby path, final overlay placement, and pause -> Return to Menu flow.
