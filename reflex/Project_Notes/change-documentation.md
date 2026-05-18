## 2026-05-18 - Player Immortality Debug Toggle

### Summary
Added a keyboard debug toggle in `PlayerManager` for switching the existing `isImmortal` state on and off during Play Mode.

### Files Affected
- Assets/Scripts/Player/PlayerManager.cs
- Project_Notes/change-documentation.md
- Project_Notes/current-project-status.md
- Project_Notes/player-manager.md

### Scenes Affected
- Any scene using the player `PlayerManager`

### Systems Affected
- Player damage handling
- Debug/test controls

### Gameplay/Debug Changes
- Pressing `=` toggles `PlayerManager.isImmortal`.
- The key is stored as serialized `immortalToggleKey` with default `Key.Equals`.
- Toggling logs whether player immortality is enabled or disabled.
- Existing damage behavior is unchanged: `TakeDamage()` still early-returns while `isImmortal` is true.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo --no-incremental` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode should be run to confirm pressing `=` toggles immortality and blocks incoming damage as expected.

## 2026-05-18 - Player Weapon Hitbox Visual Indicator Hidden

### Summary
Stopped the player weapon hitbox debug cube/gizmo from showing during weapon activation while preserving the existing hit detection flow.

### Files Affected
- Assets/Scripts/Combat/WeaponManager.cs
- Project_Notes/change-documentation.md
- Project_Notes/current-project-status.md

### Scenes Affected
- Any scene using the player `WeaponManager`

### Systems Affected
- Player combat
- Weapon hitbox debug visuals

### Gameplay/UI Changes
- `HitboxVisual` is still activated as the hitbox reference object for attack overlap calculations.
- `HitboxVisual` renderers are now disabled by default so the hitbox indicator is not visible in normal gameplay.
- Added a hidden-by-default `showHitboxVisualIndicator` debug toggle for future weapon range tuning.
- `OnDrawGizmos()` now respects the same debug toggle, so the editor wire cube stays hidden unless intentionally enabled.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode should be run to visually confirm attacks no longer reveal the hitbox cube while still damaging enemies.

## 2026-05-18 - Pause Menu Return To Main Menu Button

### Summary
Made the pause-menu `Return to Menu` button functional so it exits the paused run and loads the authored Main Menu scene.

### Files Affected
- Assets/Scripts/Visuals/UI/InGameUIManager.cs
- Assets/Scripts/Game/PauseManager.cs
- Project_Notes/change-documentation.md
- Project_Notes/current-project-status.md
- Project_Notes/main-menu.md

### Scenes Affected
- Any scene using the persistent `UI Manager` pause menu
- Assets/Scenes/Main Menu.unity

### Systems Affected
- Pause menu UI
- Scene transition flow
- Persistent pause manager state
- Main Menu return flow

### Gameplay/UI Changes
- Pause-menu `Return to Menu` now loads `Main Menu` through `TemporaryLoadingUI.LoadSceneWithOverlay()` when available.
- Return flow force-clears pause UI, music settings UI, game-over UI state, and `Time.timeScale` before changing scenes.
- `PauseManager` now resets pause state on scene load and safely rebinds the player `Pause` input when a player exists.
- Persistent in-game HUD and pause canvas are hidden when the Main Menu scene loads, preventing UI from carrying over on top of the menu.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode should be run to confirm pause -> Return to Menu -> Main Menu, then Play -> Lobby, with pause input still working after re-entry.

## 2026-05-18 - Main Menu Startup Flow Hookup

### Summary
Hooked the authored Main Menu scene into the playable flow so the built game starts on the main menu and its buttons drive Start, Settings, and Quit behavior.

### Files Affected
- Assets/Scenes/Main Menu.unity
- Assets/Scripts/Game/MainMenuManager.cs
- Assets/Scripts/Game/MainMenuManager.cs.meta
- Project_Notes/change-documentation.md
- Project_Notes/current-project-status.md
- Project_Notes/main-menu.md

### Scenes Affected
- Assets/Scenes/Main Menu.unity

### Systems Affected
- Main menu UI
- Scene startup flow
- Background music settings

### Gameplay/UI Changes
- Confirmed `Assets/Scenes/Main Menu.unity` is first in Build Settings.
- Added `MainMenuManager` to the Main Menu canvas.
- Main Menu canvas now starts visible instead of being serialized at zero scale.
- `Play btn` loads `Lobby`, using `TemporaryLoadingUI.LoadSceneWithOverlay()` when available.
- `SETTINGS` opens a runtime music settings panel with:
  - Mute Music toggle
  - Music Volume slider
  - percentage readout
  - close button
- `Quit` calls `Application.Quit()` in builds and stops Play Mode in the Unity Editor.
- Main Menu music uses the existing persistent `BackgroundMusic` system and the same `REflex.mp3` clip/preferences as in-game pause settings.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode should be run from `Main Menu` to confirm button clicks, settings overlay placement, music mute/volume, and Quit behavior.

## 2026-05-18 - Pause Music Settings Overlay Placement Fix

### Summary
Moved the pause-menu music settings panel out of the pause button layout stack so it opens as a frontmost overlay, and added an in-panel close button.

### Files Affected
- Assets/Scripts/Visuals/UI/InGameUIManager.cs
- Project_Notes/change-documentation.md
- Project_Notes/current-project-status.md

### Scenes Affected
- Any scene using the `UI Manager` prefab/runtime pause UI

### Systems Affected
- Pause menu UI
- Background music settings UI

### Gameplay/UI Changes
- Runtime music settings panel is now parented directly to the pause canvas instead of the `Main` vertical layout group.
- Panel is centered above the pause menu and brought to the top sibling layer when opened.
- Added a top-right `X` close button inside the settings panel.
- Settings button lookup now falls back to searching child buttons by GameObject name.
- Existing mute toggle and volume slider bindings continue to drive `BackgroundMusic.SetMuted()` and `BackgroundMusic.SetVolume()`.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode still needs to be run to visually confirm the panel position and click/drag behavior in the authored pause menu.

## 2026-05-18 - Pause Menu Music Settings

### Summary
Made the pause-menu Settings button functional by adding a runtime music settings panel for muting and changing background music volume.

### Files Affected
- Assets/Scripts/Game/BackgroundMusic.cs
- Assets/Scripts/Game/PauseManager.cs
- Assets/Scripts/Visuals/UI/InGameUIManager.cs
- Assets/Prefabs/UI/UI Manager.prefab
- Project_Notes/change-documentation.md
- Project_Notes/current-project-status.md

### Scenes Affected
- Lobby
- Any scene using the `UI Manager` prefab

### Systems Affected
- Pause menu UI
- Background music playback
- Audio preferences

### Gameplay/UI Changes
- Existing pause-menu `Settings` button now opens a music settings panel.
- Settings panel contains only:
  - Mute Music toggle
  - Music Volume slider with percentage readout
- Music mute and volume values persist through `PlayerPrefs`.
- `BackgroundMusic` now creates/maintains a persistent 2D `AudioSource`, uses `Assets/Audio/REflex.mp3` through the `UI Manager` prefab, and applies saved settings across scenes.
- Pause input now correctly calls `PauseGame()` when unpaused and `ResumeGame()` when paused.
- Pause UI visibility now restores the pause canvas scale when showing, matching the prefab's scale-based hidden state.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode should be run to visually confirm pause menu layout, Settings button interaction, mute behavior, and volume adjustment in the authored Lobby UI.

## 2026-05-18 - UI Manager Game Over Binding Fallback Fix

### Summary
Fixed the authored game-over screen refusing to show when one structured stat field did not match the exact hierarchy path expected by `InGameUIManager`.

### Files Affected
- Assets/Scripts/Visuals/UI/InGameUIManager.cs
- Project_Notes/change-documentation.md

### Systems Affected
- Game-over canvas binding
- Game-over summary stat population
- Return-to-lobby button binding

### Gameplay/UI Changes
- Game-over binding now requires only the authored `Game Over Canvas` and a return button to show the screen.
- Summary value fields are now discovered recursively by labelled parent object names instead of exact all-or-nothing paths.
- Binding now tolerates whitespace in object names like `Reward ` and still populates available values.
- Return to Lobby button lookup now searches descendants instead of requiring the button to be a direct child.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode should be rerun to confirm the death screen appears and every displayed value updates on the authored canvas.

## 2026-05-18 - Game Over Flow Moved Into UI Manager

### Summary
Removed the temporary game-over runtime UI path and moved death-screen ownership into `InGameUIManager`, using the authored `UI Manager` `Game Over Canvas` as the only game-over screen.

### Files Affected
- Assets/Scripts/Visuals/UI/InGameUIManager.cs
- Assets/Scripts/Player/PlayerManager.cs
- Assets/Scripts/Visuals/UI/TemporaryLoadingUI.cs
- Assets/Scripts/Visuals/UI/TemporaryLoadingUI.cs.meta
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs.meta
- Assembly-CSharp.csproj
- Project_Notes/current-project-status.md
- Project_Notes/change-documentation.md

### Systems Affected
- In-game UI manager
- Player death/game-over flow
- Game-over summary stat binding
- Return-to-lobby flow
- Loading overlay script organization

### Gameplay/UI Changes
- `InGameUIManager` now observes `PlayerManager.PlayerDied`, pauses gameplay on death, populates the authored game-over stat fields, and shows the UI Manager child `Game Over Canvas`.
- `PlayerManager.Die()` now delegates game-over presentation to `InGameUIManager` so death handling remains reliable even if event binding timing changes.
- Return to Lobby now stays in the UI manager: it hides the authored game-over screen, resets player run state, optionally generates a fresh run, and loads `Lobby` through the loading overlay.
- Removed the temporary runtime game-over canvas builder and canvas-view binding class.
- Split the loading overlay into `TemporaryLoadingUI.cs` so scene loading behavior remains available without keeping the deleted game-over script file.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode validation is still required for the death -> game-over -> Return to Lobby loop and final authored-screen visual spacing.

## 2026-05-18 - Enemy Tank Hitbox Auto-Binding + NullReference Fix

### Summary
Fixed a runtime `NullReferenceException` in enemy melee hitbox damage flow and added automatic hitbox/hurtbox discovery for spawned enemies (including tank naming variants).

### Files Affected
- Assets/Scripts/AI/EnemyHitbox.cs
- Assets/Scripts/AI/EnemyController.cs
- Assets/Scripts/AI/States/SpawnControl/EnemySpawner.cs

### Systems Affected
- Enemy attack hitbox damage trigger flow
- Enemy spawn-time hitbox/hurtbox auto-setup
- Tank enemy prefab compatibility at runtime

### Gameplay Changes
- `EnemyHitbox` now auto-resolves its parent `EnemyController` on spawn/enable before applying damage.
- Added null-guard behavior so missing controller links no longer throw exceptions during `OnTriggerStay`.
- Spawned enemies now run automatic hitbox setup:
  - resolves attack hitbox from `EnemyHitbox` component or fallback names (`Hit Box`, `Hitbox`)
  - resolves hurtbox from `EnemyHurtbox` component or fallback names (`Hurt Box`, `Hurtbox`)
  - applies `Enemy` tag to resolved hurtbox
  - ensures resolved attack hitbox starts inactive
- This specifically covers spawned tank enemies that do not use the exact `Hurt Box` naming previously expected.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Editor Play Mode validation is still required to confirm in-game hit timing and collision behavior across all enemy types.

## 2026-05-18 - Authored Game Over Canvas Binding + Typography/Format Fix

### Summary
Rewired game-over presentation to use the authored `UI Manager` `Game Over Canvas` stats layout directly, fixing the tiny fallback text formatting and restoring the intended readable font sizing/style from the designed canvas.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Game-over canvas binding and visibility flow
- Game-over summary formatting/rendering
- Return-to-lobby interaction wiring for authored canvas

### Gameplay/UI Changes
- Added authored-canvas auto-binding fallback by canvas name (`Game Over Canvas`) so setup works even without manually adding `TemporaryGameOverCanvasView`.
- Added structured field mapping for the authored stats panel:
  - Runtime, floors/stages cleared, enemies killed
  - Kill/base/floor/subtotal essence values
  - Run multiplier, reward total, composure bonus, other bonus
  - Final soul essence earned
- Game-over summary now fills each authored value field directly instead of rendering one dense monolithic TMP block.
- Added canvas root scale visibility handling so authored canvases hidden at `0,0,0` are correctly shown on death and hidden again on return.
- Updated `TemporaryGameOverCanvasView.TryGetBindings` to allow structured-layout usage without requiring a fallback `detailsText` assignment.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final visual tuning still needs in-editor Play Mode confirmation on your target resolutions after this binding change.

## 2026-05-18 - Game Over Runtime Layout Match Pass (Reference Image Alignment)

### Summary
Adjusted the runtime game-over canvas layout and text styling to match the target reference screen: compact white title text, readable in-panel stats, and a text-first Return to Lobby button under the panel.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Game-over runtime UI composition
- Run-summary text rendering/readability
- TMP runtime text material behavior

### Gameplay/UI Changes
- Removed runtime use of the red `Game Over Header` sprite for the title block (this sprite rendered as oversized red header text).
- Added a dedicated runtime title text (`GAME OVER`) in white to match target composition.
- Reworked summary body formatting into two-column lines that mirror the reference layout:
  - Runtime / Stages Cleared
  - Floors Cleared / Enemies Killed
  - Soul Essence Calculation block
  - Centered final `Soul Essence Earned`
- Reduced runtime body font size and adjusted panel content bounds so all lines stay inside the stats frame.
- Updated Return to Lobby to a text-forward style with transparent button background, positioned below the panel.
- Forced TMP `faceColor` per runtime text instance to resolve black text caused by the project default TMP material face color being black.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final pixel-perfect tuning still requires in-editor Play Mode review on your target aspect ratios.

## 2026-05-18 - Game Over UI Asset Integration (Background/Header/Stats)

### Summary
Replaced the plain temporary runtime game-over styling with the new authored Game Over art assets from `Assets/Sprites/UI`, while preserving existing game-over logic and button flow.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs
- Assets/Sprites/UI/GameOver Background.png.meta
- Assets/Sprites/UI/Game Over Header.png.meta
- Assets/Sprites/UI/Game Over Statistics Rect.png.meta

### Systems Affected
- Game-over runtime overlay presentation
- UI sprite import pipeline for new game-over assets

### Gameplay/UI Changes
- `TemporaryGameOverUI` runtime canvas now uses:
  - `GameOver Background`
  - `Game Over Header`
  - `Game Over Statistics Rect`
- Added optional sprite override fields in `TemporaryGameOverUI` for quick inspector customization.
- Added safe sprite resolution flow:
  - uses assigned inspector sprite when present
  - falls back to `Resources.Load(...)` lookup
  - in Editor, falls back to `AssetDatabase.LoadAssetAtPath(...)` for direct project asset loading
- Updated temporary runtime layout to include:
  - full-screen background art layer
  - dim layer for readability
  - centered statistics panel art
  - header art block
  - existing Return to Lobby button and run-summary text content

### Import Settings Changes
- Converted new Game Over textures to UI sprites:
  - `textureType: Sprite (2D and UI)` (`textureType: 8`)
  - `spriteMode: Single` (`spriteMode: 1`)
  - `alphaIsTransparency: true`
  - mipmaps disabled for cleaner UI rendering

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Runtime visual validation in Unity Play Mode is still required to finalize scale/spacing against target resolution variants.

## 2026-05-18 - Editor-Safe Shader Warmup Guard (Loading Overlay)

### Summary
Fixed shader warmup assertion spam in Editor by gating full shader warmup calls behind environment-aware settings and a safe warmup wrapper.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Temporary loading overlay startup flow
- Scene transition loading warmup flow
- Shader warmup safety behavior in Editor vs Player builds

### Gameplay/UI Changes
- `TemporaryLoadingUI` now has dedicated warmup toggles:
  - `warmupShadersInPlayerBuilds` (default: `true`)
  - `warmupShadersInEditor` (default: `false`)
- Startup warmup routine now skips `Shader.WarmupAllShaders()` in Editor by default.
- Scene-load warmup path now conditionally runs shader warmup only when enabled.
- Added `TryWarmupAllShaders()` wrapper and optional logging toggle (`logShaderWarmupFailures`) for safer diagnostics.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Full warmup is intentionally disabled in Editor by default to avoid keyword-state mismatch assertions during Play Mode.
- If desired for profiling, Editor warmup can be re-enabled via `warmupShadersInEditor` in the inspector.

## 2026-05-18 - Weighted Enemy Type Randomization + Tank Exclusive Scaling

### Summary
Updated enemy spawners to pick enemy types per wave from a weighted pool (Ant, Drone, Tank), with tank configured as less likely and spawned as an exclusive wave whose count scales up by floor.

### Files Affected
- Assets/Scripts/AI/States/SpawnControl/EnemySpawner.cs
- Assets/Prefabs/Enemy Prefabs/Spawn Ant.prefab
- Assets/Prefabs/Enemy Prefabs/Spawn Drone.prefab

### Systems Affected
- Enemy wave composition
- Spawner prefab configuration shared across level scenes
- Floor-based wave scaling for elite-style enemy waves

### Gameplay Changes
- Added weighted wave selection in `EnemySpawner` through `enemySpawnOptions`.
- Added per-wave exclusive mode (`exclusiveWave`) for heavy enemy types.
- Tank is now configured as:
  - lower spawn chance (`weight: 0.35` vs `1.0` for Ant/Drone)
  - exclusive wave type (no mixed enemy wave when selected)
  - floor-scaling count:
    - base: 1
    - +1 every 3 floors
    - cap: 4
- Applied identical spawn-option configuration to both shared spawner prefabs used across level scenes.

### Build/Test
- `dotnet build Assembly-CSharp.csproj -nologo` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode validation is still needed to confirm encounter feel and tank frequency pacing across full floor progression.

## 2026-05-18 - Aggression Anti-Spike Pass (Sustained Bruteforce Qualification)

### Summary
Adjusted emotion scoring so aggression no longer spikes from one or two hits. Aggressive classification now requires sustained combat commitment (multiple enemies + repeated attacks over time), matching intended bruteforce behavior.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs

### Systems Affected
- Emotion aggression score pacing
- Early-room anti-spike behavior
- Combat-only playstyle qualification (no puzzle dependency)

### Gameplay Changes
- Added sample-confidence blending for rate-based metrics:
  - Early low-duration samples blend toward neutral instead of instantly scoring as high aggression.
- Added aggression qualification gate:
  - Requires progress across attacks, enemies encountered, enemies engaged, and combat window duration before aggression evidence is fully trusted.
  - Qualification now uses an additional strict multiplicative gate so partial progress in one signal cannot overcompensate for weak commitment in others.
- Added low-commitment burst cap:
  - One/two-hit interactions with low engagement commitment are capped from reaching high aggression score (including multi-enemy scenes where most enemies are only encountered, not engaged).
- Updated no-encounter fallbacks for initiative/engagement to neutral instead of aggressive.

### Threshold Tightening
- `aggressiveThreshold`: `0.61 -> 0.67`
- `calmThreshold`: `0.41 -> 0.40`
- `minimumEvidenceForChange`: `0.30 -> 0.42`

### New Tuning Fields
- `neutralRateScore`
- `minimumAggressiveAttacks`
- `minimumAggressiveEnemiesEncountered`
- `minimumAggressiveEnemiesEngaged`
- `minimumAggressiveWindowSeconds`
- `earlyBurstAggressionCap`
- `lowQualificationCalmBonus`

### Default Qualification Retune
- `minimumAggressiveAttacks`: `4 -> 7`
- `minimumAggressiveEnemiesEncountered`: `2 -> 3`
- `minimumAggressiveEnemiesEngaged`: `2 -> 3`
- `minimumAggressiveWindowSeconds`: `10 -> 16`

### Design Notes
- Aggressive now maps to sustained pressure behavior (staying near groups and repeatedly committing attacks).
- Calm/passive behavior (hit-and-run, waiting for openings, disengaging) remains valid without puzzle telemetry.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final weights still need in-editor playtest tuning against different room densities and weapon cadences.

## 2026-05-18 - Emotion Playstyle Validity Refactor (Combat Commitment vs Avoidance)

### Summary
Refactored emotion scoring to better match intended playstyle semantics: aggressive as high combat commitment/bruteforce behavior, and calm as slower, lower-engagement, avoidance-leaning behavior.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs

### Systems Affected
- Emotion state classification and scoring
- Aggression confidence/evidence gating
- Debug factor interpretation for profile tuning

### Gameplay Changes
- Reworked score inputs from mostly raw totals to rate-normalized evidence (per-time pressure) to reduce saturation drift over long runs.
- Lifetime rate scoring now uses accumulated tracked activity time (running + idle) instead of a single-room clear duration.
- Added explicit combat commitment signal:
  - Attack volume over time
  - Attacks-per-encounter initiative
  - Hit conversion quality
- Added encounter-vs-engagement telemetry:
  - `enemiesEncountered` tracks enemies that detect the player.
  - `enemiesEngaged` tracks enemies the player actually commits attacks against.
  - Calm/avoidance scoring now uses this gap directly (seen but not engaged).
- Added explicit calm avoidance signal:
  - Lower engagement per encounter
  - Higher idle share
  - Slower room-clear tempo
  - Lower incoming pressure
- Reframed room tempo contribution:
  - Faster clears now bias aggressive score.
  - Slower clears now bias calm score.
- Updated confidence calculation to use a decision-window style evidence model (targeting ~45s trend quality) to avoid flips from isolated actions.
- Added tunable fields:
  - `expectedAttacksPerEncounter`
  - `expectedDecisionWindowSeconds`
  - `minimumRateSampleWindow`

### Design Notes
- This model aligns more directly with the project behavior goals:
  - Aggressive: commit to frequent combat and force through encounters.
  - Calm: engage less aggressively and clear more deliberately.
- Rate-based scoring keeps state interpretation more stable across progression and long sessions.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- The current tracker is intentionally combat-and-movement-only for current scope (no puzzle subsystem), so classification quality depends on encounter, engagement, movement, and damage signals.
- Final threshold/weight tuning still requires in-editor playtests across different weapon cadences and room archetypes.

## 2026-05-18 - Temporary Loading Screen (Asset Load + Shader Warmup)

### Summary
Added a temporary loading overlay system that appears during scene asset loading and shader warmup, using the same authored-canvas-or-runtime-canvas workflow as the temporary game-over UI.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Scene transition loading flow
- Shader warmup presentation
- Runtime UI overlay bootstrapping

### Gameplay/UI Changes
- Added `TemporaryLoadingUI` runtime manager (bootstrapped before scene load).
- Added `TemporaryLoadingCanvasView` authored-canvas binding component for easy in-editor customization.
- Scene loading now uses async flow with visible loading states:
  - `Loading assets...`
  - `Compiling shaders...`
  - `Finalizing...`
- Added startup shader warmup overlay (`showStartupShaderWarmup`) to cover first-load shader compilation.
- Updated all direct scene transitions in code to route through the loading overlay with safe fallback:
  - `TemporaryGameOverUI` return-to-lobby flow
  - `LevelRunManager.TravelTo(...)`
  - `LevelRunManager.AdvanceToNextFloor()`

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode validation is still required to tune perceived timing and visual polish of the loading overlay across all scene transitions.

## 2026-05-18 - Boss Floor Cadence + Direct Boss Scene Routing Fix

### Summary
Fixed boss-floor progression so boss stages start on floor 3 and repeat every 3 floors, and repaired boss-scene start behavior that could route to unintended rooms.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelRunManager.cs
- Assets/Scripts/AI/Boss/BossController.cs

### Systems Affected
- Floor stage-scene generation
- Scene-to-node synchronization when loading scenes directly
- Boss room clear/progression flow

### Gameplay Changes
- Added configurable boss-floor cadence in `LevelRunManager`:
  - `firstBossFloor` (default `3`)
  - `bossFloorInterval` (default `3`)
- Boss stage placement now follows floor cadence:
  - Boss floors: include boss stage (kept at end when `keepBossStageAtEnd` is enabled).
  - Non-boss floors: exclude boss scenes from random stage order.
- Direct scene starts in a boss scene now align to the next scheduled boss floor run and bind to the generated boss node instead of drifting to another room route.
- Boss scenes no longer auto-clear from the "no active spawners" fallback path.
- Boss defeat now explicitly clears the current stage via:
  - `LevelRunManager.Instance.MarkCurrentLevelClearedFromScene("Boss defeated")`

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode verification is still required to confirm full in-scene boss encounter flow, defeat timing, and no-door auto-advance behavior.

## 2026-05-18 - Dash Cancel NullReference Fix (WeaponManager)

### Summary
Fixed a NullReferenceException when dashing while no weapon data is currently equipped or restored.

### Files Affected
- Assets/Scripts/Combat/WeaponManager.cs

### Systems Affected
- Combat combo timer reset flow
- Dash cancel attack cleanup path

### Gameplay Changes
- `WeaponManager.HitboxOff()` now safely handles missing references before toggling attack state.
- `WeaponManager.StartResetTime()` now guards against missing `playerManager` and missing `weaponData`.
- When no weapon is equipped, combo state now safely resets instead of throwing a console error.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode validation is still required to confirm full in-game behavior during early-load/no-weapon edge cases.

## 2026-05-17 - Floor-Scaled Enemy Wave Sequencing + Clear Gating

### Summary
Added optional multi-wave combat in enemy rooms with floor-scaled extra-wave chance, and blocked room clear/reward progression while upcoming waves are queued.

### Files Affected
- Assets/Scripts/AI/States/SpawnControl/EnemySpawner.cs
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Enemy wave sequencing
- Room clear evaluation flow
- Buff-card reward timing

### Gameplay Changes
- Enemy spawners now roll for another wave after each wave is defeated.
- Extra-wave chance starts low on floor 1 and increases with floor depth up to a cap.
- `EnemySpawner` now tracks upcoming-wave state through `HasUpcomingWave`.
- `LevelRunManager` now defers `RoomEvaluated` clear processing when any current-scene spawner has an upcoming wave queued.
- Result: rooms only enter cleared state and trigger buff cards after the final wave is finished.

### Tuning Fields
- `enableAdditionalWaves`
- `maxWavesPerRoom`
- `additionalWaveChanceFloorOne`
- `additionalWaveChancePerFloor`
- `maxAdditionalWaveChance`
- `respawnDelay` (existing spacing between waves)

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode verification is still needed for multi-wave pacing and feel across full floor progression.

## 2026-05-17 - Door Randomization Fix (Double-Door Bias Removal)

### Summary
Fixed door candidate discovery and random grouping so linked double doors count as one logical choice without dominating random exit selection.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelDoorAutoBinder.cs
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Scene door candidate auto-discovery
- Single-random-door grouping and selection

### Gameplay Changes
- Door auto-binder now combines directional door candidates with `Doors*` group candidates instead of preferring only one category.
- Added shorthand directional support in name matching:
  - `Walls N`, `Walls S`, `Walls E`, `Walls W`
  - plus existing full-name directional aliases.
- Added candidate filtering to avoid selecting broad parent containers (for example generic `Doors`) when child door candidates exist.
- Updated random grouping so sibling pair objects named `Door/Doors S` and `Door/Doors W` under the same parent are treated as one logical door selection.
- Result: double-door back-to-back setups still open together, but they no longer suppress other valid door options in random selection.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final perceived randomness and route readability still require in-editor Play Mode validation per scene layout.

## 2026-05-17 - Lobby RewardManager Priority Fix (Use Inspector Card Pool)

### Summary
Fixed RewardManager ownership resolution so a scene-authored manager (for example the Lobby instance with your 15 configured buff cards) always takes precedence over the runtime bootstrap fallback manager.

### Files Affected
- Assets/Scripts/Interactables/RewardManager.cs

### Systems Affected
- Reward manager singleton lifecycle
- Card-pool source selection across scene boot and transition

### Gameplay/UI Changes
- Added bootstrap-origin tracking (`_spawnedByBootstrap`) on runtime-created RewardManager instances.
- Updated singleton conflict resolution in `Awake()`:
  - If existing instance is bootstrap and current is scene-authored, the scene-authored manager replaces it.
  - If existing instance is scene-authored and current is bootstrap, bootstrap instance is destroyed.
  - Score-based arbitration is still used when both instances are the same origin type.
- This ensures manually assigned Lobby buff cards are preserved and used instead of fallback runtime card definitions.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode verification is still required to confirm inspector card presentation end-to-end in your current scenes.

## 2026-05-17 - Linked Back-to-Back Door Groups (Door W / Door S)

### Summary
Updated single-random-door selection so back-to-back doors under the same `Door W` / `Door S` parent are treated as one logical door choice and open together.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Random door-route assignment
- Linked door-group selection behavior

### Gameplay Changes
- Single-door random selection now groups doors by parent for linked back-to-back setups:
  - `Door S`
  - `Door W`
  - `Doors S`
  - `Doors W`
- If any door in one of these parents is selected, all doors in that same parent receive the same generated route and open together.
- Entry-side exclusion still applies at group level:
  - Groups containing blocked entry doors are excluded when possible.
  - Fallback keeps progression possible if all groups are blocked candidates.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Single Random Exit Door + Buff-Choice Door Gate

### Summary
Updated progression door flow so only one exit door is active per combat stage, chosen randomly each room entry, while avoiding the entry-side door when possible. Doors now remain locked after clear until a buff card is chosen.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelRunManager.cs
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Scripts/LevelGeneration/LevelDoor.cs

### Systems Affected
- Door route binding and entry-door exclusion behavior
- Stage clear reward gating and door unlock timing

### Gameplay Changes
- Added single-door activation mode for generated door routes:
  - Only one door receives a route per scene entry.
  - The active door is selected randomly from available doors.
  - The destination route is selected randomly from current generated node connections.
- Entry-side exclusion:
  - Entry-side door group is still identified from player spawn proximity.
  - Random active door selection excludes that blocked entry-side group when possible.
  - Safe fallback keeps progression possible if no non-entry candidate exists.
- Buff choice gate for door unlocking:
  - Added `RewardManager.IsAwaitingBuffChoiceForDoorUnlock`.
  - `LevelRunManager.AreDoorsUnlocked` now requires pending buff choice to be completed (non-lobby nodes).
  - Reward manager now tracks pending buff choice after clear and clears it on card selection / safety-close paths.
- Door prompt now reports buff gate explicitly (`Choose a buff first`) while waiting for card selection.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- If reward UI/card pool cannot open after clear, the pending-buff lock is not applied to avoid hard-locking progression.

## 2026-05-17 - Per-Door Slide Direction Component

### Summary
Added a dedicated door slide settings component so each door can explicitly define how `door_left` and `door_right` panels slide.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelDoor.cs
- Assets/Scripts/LevelGeneration/LevelDoorSlideSettings.cs
- Assets/Scripts/LevelGeneration/LevelDoorSlideSettings.cs.meta

### Systems Affected
- Door animation direction configuration
- Door leaf open-position calculation

### Gameplay Changes
- Added `LevelDoorSlideSettings` component (Inspector-editable) with:
  - `doorLeftSlideDirection` (local-space vector)
  - `doorRightSlideDirection` (local-space vector)
  - `normalizeDirections`
- `LevelDoor` now reads slide directions from `LevelDoorSlideSettings` when present.
- `LevelDoor` now auto-attaches `LevelDoorSlideSettings` at runtime when missing.
- Default slide directions remain `left` for `door_left` and `right` for `door_right`.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Deterministic Left/Right Sliding Direction

### Summary
Adjusted sliding-door animation to use explicit name-based direction: `door_left` always slides left and `door_right` always slides right in local door space.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelDoor.cs

### Systems Affected
- Door panel open-direction logic

### Gameplay Changes
- Removed pair-axis inference and fallback axis guessing for split-door motion.
- Door leaves now open with deterministic local-axis behavior:
  - `door_left`: `Vector3.left * openOffset`
  - `door_right`: `Vector3.right * openOffset`
- This ensures a consistent split-slide animation for the prefab behavior you requested.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Sliding Door Split Fix (Left/Right Separation)

### Summary
Fixed door panel animation so sliding doors split correctly: `door_left` and `door_right` now separate away from each other as a pair instead of using per-panel axis guessing.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelDoor.cs

### Systems Affected
- Door leaf animation targeting and open direction resolution

### Gameplay Changes
- Door leaves are now paired (`door_left` with nearest `door_right`) and opened in opposite directions along the pair axis.
- Pair axis is derived from left-right local position delta (x/z dominant axis), which prevents incorrect forward/backward panel drift on certain prefab orientations.
- Added fallback behavior for unmatched leaves to keep single-panel edge cases functional.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final per-scene feel still requires in-editor Play Mode verification for offset/speed tuning.

## 2026-05-17 - Contextual Door Opening + Entry Door Lockout

### Summary
Implemented contextual door animation and traversal rules so doors now open based on scene context and the entry-side door is blocked after entering a combat level.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelDoor.cs
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Door interaction prompts and traversal gating
- Door leaf animation (FBX child parts `door_left` / `door_right`)
- Scene-entry progression guardrails

### Gameplay Changes
- Added animated sliding behavior for door leaf meshes:
  - Automatically finds all `door_left` and `door_right` children under each bound `LevelDoor`.
  - Captures closed local positions and animates to open positions using configurable offset (`1.21` default).
  - Auto-selects slide axis per leaf (`x` or `z`) based on local layout to support rotated door placements.
  - Supports multi-door/double-door setups by animating all matching leaves under each door root.
- Lobby behavior:
  - Doors now open when the player gets near (proximity-driven open state).
- Combat level behavior:
  - Doors stay closed until normal unlock conditions are met (room clear / unlocked state).
- Entry door lockout:
  - On non-lobby room entry, the nearest routed door to the spawn/entry position is marked blocked for re-entry.
  - Nearby sibling doors within a small grouping radius are also blocked to handle double-door entry sides.
  - Safeguard prevents blocking every routed door in a room.
  - Blocked doors show `Cannot go back through this door` and ignore interaction.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final animation feel and entry-door grouping radius still need in-editor Play Mode tuning per scene layout.

## 2026-05-18 - HitboxOn Combo Index Guard (IndexOutOfRange Fix)

### Summary
Fixed `IndexOutOfRangeException` in `WeaponManager.HitboxOn()` caused by animation event timing firing with an invalid combo index after state resets (for example after death/respawn transitions).

### Files Affected
- Assets/Scripts/Combat/WeaponManager.cs

### Systems Affected
- Combat hitbox animation-event execution safety
- Post-reset combat stability

### Gameplay Changes
- Added robust guards in `HitboxOn()` for:
  - missing `playerManager`
  - dash state (null-safe)
  - missing/empty weapon combo data
  - invalid combo step index (`currentComboIndex - 1` out of bounds)
  - missing hitbox visual object
- On invalid combo-state events, hitbox flow now exits safely via `HitboxOff()` instead of crashing.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- In-editor playtesting is still needed to validate animation-event timing during edge transitions (death, dash cancel, immediate re-entry).

## 2026-05-18 - Return-to-Lobby Respawn Reset (Death Recovery)

### Summary
Updated return-to-lobby flow after death to perform a full player respawn reset: clear in-run card buffs, restore health, and clear dead/combat lock state before loading Lobby.

### Files Affected
- Assets/Scripts/Player/PlayerManager.cs
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Player runtime state reset
- Game-over return-to-lobby behavior

### Gameplay Changes
- Added `PlayerManager.RespawnForRunStart()` to centralize true respawn reset behavior:
  - clears card buffs
  - restores HP to max
  - resets dead/combat/state flags (`isDead`, `canAttack`, combo state, vulnerability state)
  - refreshes health UI immediately
- `ResetTemporaryRunState()` now calls `RespawnForRunStart()` to keep existing run-start reset paths consistent.
- Pressing `Return to Lobby` on game-over now explicitly calls player respawn reset before fresh-run generation and lobby load.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- In-editor Play Mode validation is still needed to confirm feel/timing around death-to-lobby transition and input recovery.

## 2026-05-18 - Game Over Return-to-Lobby Button

### Summary
Added a Return to Lobby button to the game-over screen for both authored-canvas UI and runtime fallback UI paths.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Game-over navigation flow
- Scene-authored game-over canvas bindings
- Runtime fallback game-over UI

### Gameplay/UI Changes
- Added `Return to Lobby` button support to `TemporaryGameOverCanvasView` bindings.
- Added runtime fallback `Return To Lobby` button creation when no authored canvas is available.
- Added button click behavior:
  - resumes time (`Time.timeScale = 1`)
  - hides game-over overlay
  - optionally regenerates a fresh run before returning
  - loads lobby scene (default: `Lobby`)
- Added script controls:
  - `lobbySceneName` (default `Lobby`)
  - `generateFreshRunOnReturn` (enabled by default)

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Authoring path requires assigning the optional return button reference in `TemporaryGameOverCanvasView` to override runtime fallback button usage cleanly.

## 2026-05-18 - Game Over UI Authoring Canvas Hook

### Summary
Converted the temporary game-over flow to support a scene-authored Canvas so UI/UX can be designed directly in-editor without relying on hardcoded runtime layout.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Player-death game-over presentation
- Runtime UI binding and fallback behavior

### Gameplay/UI Changes
- Added `TemporaryGameOverCanvasView` component (in the same script file) for editor-authored bindings:
  - `CanvasGroup`
  - `TextMeshProUGUI` details text
- `TemporaryGameOverUI` now:
  - searches for an authored `TemporaryGameOverCanvasView` in loaded scenes (including inactive objects)
  - uses authored canvas bindings when available
  - falls back to runtime-generated UI only when no authored view is found
- Authoring flow is now designer-friendly:
  - create/modify your own Canvas hierarchy
  - assign references in `TemporaryGameOverCanvasView`
  - iterate on layout/visuals without code edits

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Current display content is still composed into one details text block; visual hierarchy is now editable, but field-level text splitting is a future enhancement.

## 2026-05-17 - Equipped Weapon Persistence Without Manual Weapon Lists

### Summary
Removed the dependency on manually filling `SaveManager.availableWeapons` to restore the equipped weapon from save data.

### Files Affected
- Assets/Scripts/Data/SaveManager.cs
- Assets/Scripts/Combat/WeaponManager.cs

### Systems Affected
- Save/load equipped weapon restoration
- Runtime weapon discovery

### Gameplay Changes
- Save restore now auto-discovers weapon references from:
  - optional manual references (if present)
  - current player weapon
  - scene `WeaponPickup` references
  - currently loaded `WeaponData` assets
- Added pending-restore retry on scene load when a saved weapon is not yet available in memory.
- Added `WeaponManager.EquipWeaponFromSave(...)` so save restoration can equip visuals/state without re-writing save data each apply.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- If a saved weapon asset is not included/referenced in runtime content at all, restore will continue to log that the weapon cannot be resolved.

## 2026-05-17 - Temporary Game Over Compile Fix (CS0170)

### Summary
Fixed a struct definite-assignment compile error in temporary game-over summary rendering.

### Files Affected
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs

### Systems Affected
- Temporary game-over UI
- Run summary fallback/selection flow

### Gameplay/UI Changes
- `RunRewardSummary` now initializes from fallback first, then gets replaced by runtime summary when available.
- Removes `CS0170` (`effectiveCombinedMultiplier` possibly unassigned) from game-over text rendering.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing unrelated warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Run-Persistent Buff Cards + Special Card Locks + Temporary Game Over Summary

### Summary
Changed buff-card behavior from single-stage replacement to run-persistent stacking, added per-card duration and special-card exclusivity rules, and implemented a temporary runtime game-over screen with run-summary calculations.

### Files Affected
- Assets/Scripts/Data/Buffs Data/BuffCardData.cs
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Scripts/Player/PlayerManager.cs
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs
- Assets/Scripts/Visuals/UI/TemporaryGameOverUI.cs.meta

### Systems Affected
- Buff-card data schema
- Card-offer filtering and card-lifecycle runtime behavior
- Run reward telemetry and essence breakdown tracking
- Player death signaling
- Game-over UI fallback flow

### Gameplay/UI Changes
- Buff cards now persist for the run by default and no longer clear automatically when a new card is selected.
- Added `buffDurationStages` to card data:
  - `0` means whole-run duration
  - `1+` means expires after that many stage clears
- Added special-card fields:
  - `isSpecialCard`
  - `blockedCards`
- Special cards can only be picked once per run.
- Picked special cards can permanently block contradictory cards from appearing again in that run.
- Added built-in rule enforcement for `Fleet foot` <-> `Windrunner` mutual exclusivity and default one-stage duration for `Berserker Tempo` when missing.
- Reward manager now tracks run-level essence breakdown data and exposes a summary snapshot.
- Added `PlayerManager.PlayerDied` event for death-driven UI flow.
- Added a temporary runtime game-over overlay that shows:
  - Game Over title
  - Runtime
  - Floor/stage cleared
  - Enemies killed
  - Total Soul Essence earned
  - Calculation breakdown (`kills x essencePerKill`, raw subtotal, effective multiplier, stage total, composure bonus, other sources)

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Temporary game-over UI is runtime-generated placeholder UI and intended to be replaced by final authored UI.
- Full in-editor Play Mode validation is still required for multi-floor duration edge cases and final pacing feel.

## 2026-05-17 - One-Time Lobby Entry (No Lobby Between Floors)

### Summary
Updated progression flow so Lobby is only used at game start. After a floor is completed, the run now advances directly into the next floor's stage 1 without returning to Lobby.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Floor transition flow
- Generated run graph destination labeling

### Gameplay Changes
- Final stage transition no longer loads Lobby.
- Floor completion now increments floor immediately and loads next floor stage 1 scene directly.
- Node `0` transitions from final stages are now interpreted as a `Next Floor` transition marker.
- Door label for node `0` destination now displays `Next Floor` instead of `Lobby`.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Floor Debug HUD + Auto Docking

### Summary
Added a dedicated floor debug HUD that displays the current floor and stage, and auto-docks to avoid overlap when another debug HUD is visible.

### Files Affected
- Assets/Scripts/AI/FloorDebugHUD.cs
- Assets/Scripts/AI/FloorDebugHUD.cs.meta
- Assets/Scripts/AI/EmotionDebugHUD.cs

### Systems Affected
- Runtime debug UI overlays
- Floor progression visibility and QA observability

### Gameplay/UI Changes
- Added `FloorDebugHUD` (toggle key: `F5`) with:
  - Current floor
  - Current stage / stages per floor
  - Floor difficulty multipliers (HP, damage, spawn, respawn)
- Added auto-docking behavior:
  - If `EmotionDebugHUD` is visible, floor HUD docks to the right when possible, otherwise below.
  - Final position is clamped to screen bounds.
- Added screen-size-aware scaling for readability across resolutions.
- Exposed visible-area information from `EmotionDebugHUD` so other debug overlays can position relative to it.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Randomized Stage Order Per Floor

### Summary
Updated floor progression so stage order randomizes every floor instead of always following a fixed scene sequence.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelRunManager.cs

### Systems Affected
- Floor run generation and per-floor scene ordering

### Gameplay Changes
- Enabled per-floor stage-order randomization.
- Each new floor now gets a fresh shuffled stage sequence.
- Boss scenes are still pinned to the final stage of each floor when available, preserving a strong floor climax.
- Added profile-aware scene pooling for randomized generation:
  - Non-boss and boss scene candidates are derived from `LevelGenerationProfile.RoomScenes`.
  - Falls back to `roomSceneNames` if profile candidates are unavailable.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Floor Loop Progression (Stage 1-5 per Floor)

### Summary
Implemented floor-based progression where each floor runs through stages 1-5, then returns to Lobby and advances to the next floor. Added floor-scaled enemy difficulty so higher floors are harder.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelRunManager.cs
- Assets/Scripts/AI/States/SpawnControl/EnemySpawner.cs
- Assets/Scripts/AI/EnemyController.cs
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Resources/LevelGeneration/Default Level Generation Profile.asset

### Scenes Affected
- Assets/Scenes/Lobby.unity
- Assets/Scenes/Level_1_Scene.unity
- Assets/Scenes/Level_2_Scene.unity
- Assets/Scenes/Level_3_Scene.unity
- Assets/Scenes/Level_4_Scene.unity
- Assets/Scenes/Final Boss Level.unity

### Systems Affected
- Run generation and scene progression
- Floor/stage labeling and reward context mapping
- Enemy spawn pressure scaling
- Enemy stat scaling

### Gameplay Changes
- Run path now executes as stage ladder per floor:
  - Stage 1: `Level_1_Scene`
  - Stage 2: `Level_2_Scene`
  - Stage 3: `Level_3_Scene`
  - Stage 4: `Level_4_Scene`
  - Stage 5: `Final Boss Level`
- After clearing stage 5 and returning to Lobby, floor increments (`Floor 1 -> Floor 2 -> Floor 3 ...`) and a new floor run is generated.
- Difficulty increases with floor:
  - Enemy health scales up per floor.
  - Enemy damage scales up per floor.
  - Spawn counts scale up per floor.
  - Respawn delay scales down per floor (with minimum clamp).
- Door destination labels now display `Floor X - Stage Y`.
- Fallback scene order is now deterministic (sequential) if profile scene candidates are unavailable.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Runtime playtesting is still required for balance tuning of higher-floor scaling values.
- Floor progression currently resets when a fresh session starts (no cross-session persistence yet).

## 2026-05-17 - Room_2 Removal and Flow Repair

### Summary
Removed `Room_2` from the active level flow and fixed a broken default generation profile asset that had unresolved Git merge markers, which was causing fallback behavior and incorrect lobby returns.

### Files Affected
- Assets/Resources/LevelGeneration/Default Level Generation Profile.asset
- Assets/Scripts/LevelGeneration/LevelRunManager.cs
- ProjectSettings/EditorBuildSettings.asset

### Scenes Affected
- Assets/Scenes/Lobby.unity
- Assets/Scenes/Level_1_Scene.unity
- Assets/Scenes/Level_2_Scene.unity
- Assets/Scenes/Level_3_Scene.unity
- Assets/Scenes/Level_4_Scene.unity
- Assets/Scenes/Final Boss Level.unity
- Assets/Scenes/SampleScene.unity

### Systems Affected
- Level generation profile loading
- Deterministic progression pathing
- Build scene inclusion list

### Gameplay Changes
- Rebuilt `Default Level Generation Profile.asset` as valid Unity YAML (removed unresolved merge conflict markers).
- Updated deterministic run path to remove `Room_2` while preserving the 6-step flow:
  - Lobby -> Level 1 -> Level 2 -> Level 3 -> Level 4 -> Level 5 (`Level_4_Scene` reuse) -> Final Boss -> Lobby.
- Removed `Room_2` from build scenes.
- Corrected fallback room scene name typo in `LevelRunManager`:
  - `Level_4_scene` -> `Level_4_Scene`.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- A dedicated `Level_5` scene is still pending; `Level_4_Scene` is currently reused for floor 5.
- Runtime playtesting in Unity Editor is still required to validate full traversal feel.

## 2026-05-16 - PatrolState NavMesh Guard Fix

### Summary
Fixed a runtime AI crash caused by reading `NavMeshAgent.remainingDistance` when the agent is not currently on a NavMesh.

### Files Affected
- Assets/Scripts/AI/States/PatrolState.cs

### Systems Affected
- Enemy AI state machine
- Patrol navigation safety checks

### Gameplay Changes
- `PatrolState` now verifies the agent is non-null, active, and on a NavMesh before:
  - setting patrol destinations in `OnEnter()`
  - evaluating arrival via `remainingDistance` in `Tick()`
- Enemies that are temporarily off NavMesh no longer throw exceptions from patrol logic.

### Build/Test
- Pending in-editor Play Mode validation for spawn placements and patrol transitions.

### Known Limitations
- This change prevents the crash, but enemies spawned off-bake may still idle until placed back on a valid NavMesh area.

## 2026-05-16 - Lobby Entry Flow + Linear Run to Boss

### Summary
Wired the game to use `Lobby` as the true entry scene with a deterministic level path and a reachable boss endpoint, while keeping `SampleScene` available for testing.

### Files Affected
- Assets/Scripts/LevelGeneration/LevelDoorAutoBinder.cs
- Assets/Scripts/LevelGeneration/LevelRunManager.cs
- Assets/Scripts/LevelGeneration/LevelGenerationProfile.cs
- Assets/Resources/LevelGeneration/Default Level Generation Profile.asset
- ProjectSettings/EditorBuildSettings.asset

### Scenes Affected
- Assets/Scenes/Lobby.unity
- Assets/Scenes/Level_1_Scene.unity
- Assets/Scenes/Level_2_Scene.unity
- Assets/Scenes/Level_3_Scene.unity
- Assets/Scenes/Room_2.unity
- Assets/Scenes/Final Boss Level.unity
- Assets/Scenes/SampleScene.unity

### Systems Affected
- Build scene bootstrap order
- Generated run graph and door routing
- Scene transition fallback behavior

### Gameplay Changes
- Build scene list now includes `Final Boss Level` and retains `SampleScene` as a test scene.
- Default run profile is now linear and deterministic:
  - Lobby -> Level 1 -> Level 2 -> Level 3 -> Level 4 (`Level_4_Scene`) -> Level 5 (`Room_2`) -> Final Boss -> Lobby.
- Door binder now supports directional fallback names (for example `North`, `Walls_North`) when explicit `Door/Doors` objects are not present.
- When a node has exactly one route, all detected door candidates map to that same route for clearer progression.
- Added safe auto-advance fallback for scenes with no door candidates:
  - Lobby can enter the run even without explicit door objects.
  - Non-lobby nodes only auto-advance after the room is cleared.
- Run door binding/auto-advance now only applies when the active scene matches the current generated node, so direct test-scene launches (for example `SampleScene`) are not force-routed into the run.
- Corrected a scene-name/build-settings mismatch that caused `Room_2` load failures by aligning profile depth mapping with the available scene set and adding `Room_2` explicitly to build scenes.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- A dedicated `Level_5` scene is not yet present; current flow uses `Room_2` as level 5.
- In-editor Unity Play Mode validation is still required for final interaction feel and exit readability.

## 2026-05-16 - Emotion Loop Refinement

### Summary
Improved the emotion adaptation loop so game responses update continuously from aggression score and confidence, not only when calm/aggressive state flips.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs
- Assets/Scripts/AI/EmotionDirector.cs
- Assets/Scripts/AI/EmotionDebugHUD.cs
- Assets/Scripts/AI/States/SpawnControl/EnemySpawner.cs

### Systems Affected
- Emotion analysis and signaling
- Emotion director adaptation logic
- Spawn count and respawn timing adaptation
- Debug observability

### Gameplay Changes
- Added `EmotionProfileUpdated` event emitted during each evaluation pass.
- Director now computes a continuous blend between calm and aggressive tuning based on aggression score and confidence.
- Director updates are now bounded in logging to reduce spam while still reporting meaningful adaptation shifts.
- Respawn delay scaling can now use continuous score/confidence blending.
- Debug HUD now displays director blend/confidence to make adaptation behavior easier to tune.

### Design Notes
- Maintains existing calm/aggressive strategy framing while making response intensity smoother.
- Confidence dampens overreaction early in a room when evidence is weak.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- One existing warning remains unrelated to this change:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Runtime playtesting in Unity Editor is still needed for final feel tuning.
- `GAME_DEV_RULES.md` appears to contain encoding artifacts and should be normalized to UTF-8.

## 2026-05-16 - Emotion Distinction Pass (Behavior + Visual)

### Summary
Made calm vs aggressive profiles more visibly and behaviorally distinct by wiring existing director fields into live chase movement and scene tinting.

### Files Affected
- Assets/Scripts/AI/States/ChaseState.cs
- Assets/Scripts/AI/EmotionDirector.cs

### Gameplay Changes
- `ChaseState` now uses `GetDirectorChaseDestination(...)` as a tactical destination source.
- In `AggressionContainment`, enemies now honor standoff/retreat behavior while still applying local separation.
- Calm chase flow keeps ring-style pressure but now routes through director tactical destination.

### Visual Changes
- Director `worldTint` is now applied to:
  - `RenderSettings.ambientLight` (tint strength controlled)
  - Active camera background colors (tint strength controlled)
- Visual baselines are cached per scene and restored when tinting is disabled or director is disabled.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final tuning still requires in-editor playtesting for perceived intensity and readability.

## 2026-05-17 - Calm Motivation Pass (Relief + Composure Rewards)

### Summary
Implemented a calm-play motivation loop without recovery buffs by adding tactical relief charges and composure essence rewards.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs
- Assets/Scripts/AI/EmotionDirector.cs
- Assets/Scripts/AI/EmotionDebugHUD.cs
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Scripts/Visuals/UI/InGameUIManager.cs

### Gameplay Changes
- Added `EmotionEngine.RoomStarted` event so systems can react exactly when a new combat room starts.
- Added Calm Relief charges in the emotion director:
  - Calm, low-damage, deathless rooms with enough combat actions earn charges.
  - Next room can consume a charge for reduced spawn pressure and slower enemy aggression.
- Added Composure Soul Essence bonus in reward manager:
  - Eligible calm rooms grant extra essence with quality scaling.
  - Optional on-screen status message communicates the reward immediately.

### Design Notes
- Keeps kill-all progression intact while making calm execution strategically valuable.
- Incentive is explicit (extra currency) and practical (easier next-room pressure), encouraging deliberate play instead of panic trading.

### Build/Test
- Pending compile + play-mode pass after this change set.

### Known Limitations
- Status message feedback requires UI references (`statusMessageText` and `statusMessageCanvasGroup`) to be assigned in the scene UI.
- Final threshold tuning still needed in-editor for pacing and fairness.

## 2026-05-17 - Aggression Spike Mitigation (Stacked Enemy Hits)

### Summary
Added burst-aware hit scoring so one punch hitting a stacked pack no longer spikes aggression as if all hits were fully independent actions.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs
- Assets/Scripts/AI/EmotionDebugHUD.cs

### Gameplay Changes
- Added effective hit tracking separate from raw hit count.
- Introduced diminishing returns for additional hits in the same burst window.
- Added per-attack effective-hit budget cap to prevent single-swing over-weighting.
- Emotion hit score and confidence action evidence now use effective hits.
- Debug HUD now shows effective hits to support live tuning.

### Design Notes
- Raw combat events are preserved for analytics/readability.
- Emotion adaptation now reacts more to sustained aggressive behavior over time than to one high-density overlap event.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final values for `multiHitBurstWindow`, `additionalHitFalloff`, and `maxEffectiveHitsPerAttack` need in-editor playtest tuning.

## 2026-05-17 - Aggression Tempo Tuning (Slower Build, Faster Cooldown)

### Summary
Adjusted emotion tempo so aggression climbs less abruptly from isolated attack events and decays faster when combat pressure cools down.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs

### Gameplay Changes
- Added directional smoothing:
  - `aggressionRiseSmoothing` for slower upward movement.
  - `aggressionFallSmoothing` for faster downward movement.
- Added passive calm decay after short inactivity:
  - `calmDecayDelay`
  - `calmDecayPerSecond`
- Reduced attack/hit pressure sensitivity in weighted scoring:
  - `attackIntentScale`
  - `hitIntentScale`
- Combat actions now refresh a shared combat-intent timestamp used by decay logic.

### Design Notes
- Keeps responsiveness while reducing one-attack overreaction.
- Encourages emotion state to reflect sustained behavior, not single spikes.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Final values still need in-editor feel validation against different weapons and room densities.

## 2026-05-17 - Forgiving Aggression Rebalance

### Summary
Rebalanced emotion thresholds and tempo to make aggression significantly more forgiving for passive/disengage play patterns.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs

### Gameplay Changes
- Raised aggressive entry threshold and widened hysteresis:
  - `aggressiveThreshold`: `0.58 -> 0.64`
  - `calmThreshold`: `0.42 -> 0.46`
- Reduced upward response intensity:
  - `aggressionRiseSmoothing`: `0.20 -> 0.14`
  - `attackIntentScale`: `0.75 -> 0.58`
  - `hitIntentScale`: `0.70 -> 0.52`
- Increased cooldown/recovery behavior:
  - `calmDecayDelay`: `0.90 -> 0.55`
  - `calmDecayPerSecond`: `0.07 -> 0.11`
  - `recentBehaviorWeight`: `0.60 -> 0.75`
- Added explicit passive forgiveness model:
  - `passiveRecoveryBoost`
  - `passiveForgivenessBias`
  - Recovery now prioritizes recent calm behavior when recent score drops below lifetime score.
- Raised transition evidence requirement:
  - `minimumEvidenceForChange`: `0.25 -> 0.38`

### Design Notes
- Intent is to prevent “one hit + disengage” loops from drifting aggressive unless pressure is sustained.
- Aggressive state should now represent sustained combat intent, not brief interactions.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Requires in-editor playtest to calibrate final feel around specific weapon cadence and room layouts.

## 2026-05-17 - Floor Transition Stability Fix (Emotion Engine)

### Summary
Added progression-stability safeguards so emotion telemetry does not saturate or stall across floors and continues updating reliably after floor transitions.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs

### Gameplay Changes
- Emotion engine now listens for floor entry (`LevelRunManager.LevelEntered`).
- On combat floor entry:
  - optionally clears stale active room/spawner state
  - optionally rebases telemetry with configurable carryover factor
  - forces an immediate emotion evaluation to keep HUD/director state fresh
- Added tunables:
  - `rebaseTelemetryOnLevelEntered`
  - `levelCarryoverFactor`
  - `clearRoomStateOnLevelEntered`

### Design Notes
- Prevents lifetime metric saturation from making aggression appear frozen at higher floors.
- Keeps room state consistent when moving between scenes/floors.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Needs in-editor multi-floor verification to confirm behavior under all room/spawner combinations.

## 2026-05-18 - Combat-Only Emotion Updates + Less-Forgiving Rebalance

### Summary
Reduced calm-overcorrection and restricted passive emotion decay/telemetry updates to active combat rooms only.

### Files Affected
- Assets/Scripts/AI/EmotionEngine.cs

### Gameplay Changes
- Rebalanced aggressiveness toward less-forgiving defaults:
  - `aggressiveThreshold`: `0.64 -> 0.61`
  - `calmThreshold`: `0.46 -> 0.41`
  - `aggressionRiseSmoothing`: `0.14 -> 0.18`
  - `aggressionFallSmoothing`: `0.55 -> 0.42`
  - `calmDecayDelay`: `0.55 -> 1.20`
  - `calmDecayPerSecond`: `0.11 -> 0.04`
  - `attackIntentScale`: `0.58 -> 0.68`
  - `hitIntentScale`: `0.52 -> 0.62`
  - `passiveRecoveryBoost`: `0.50 -> 0.22`
  - `passiveForgivenessBias`: `0.09 -> 0.015`
  - `recentBehaviorWeight`: `0.75 -> 0.62`
  - `minimumEvidenceForChange`: `0.38 -> 0.30`
- Telemetry event writes now early-return when there is no active room:
  - `RecordDamageTaken`
  - `RecordDeath`
  - `RecordEnemyEncounter`
  - `RecordAttackStarted`
  - `RecordEnemyHit`
  - `RecordMovement`
- Periodic evaluation in `Update()` now runs only while `IsRoomActive`.
- Passive calm decay and passive forgiveness bias are now disabled outside active combat.
- Floor-enter handler no longer forces an aggression re-score; it only pushes a snapshot update after optional rebase.

### Design Notes
- Addresses the issue where passive/out-of-combat behavior kept decreasing aggression.
- Keeps calm recovery possible, but slower and more tied to actual combat behavior.

### Build/Test
- Full solution build is currently blocked by unrelated missing types:
  - `BossManager`, `BossState`, `LevelDoorSlideSettings`
- EmotionEngine edits themselves were applied cleanly, but in-editor validation is required until the unrelated compile blockers are resolved.

## 2026-05-17 - Composure Reward UI Wiring (Status Message)

### Summary
Finished wiring the composure reward status message UI so `+Soul Essence (Composure)` can render through the persistent `UI Manager` prefab at runtime.

### Files Affected
- Assets/Prefabs/UI/UI Manager.prefab

### Scenes Affected
- Assets/Scenes/SampleScene.unity (inspected prefab instance overrides)
- Assets/Scenes/Lobby.unity (inspected prefab instance overrides)
- Assets/Scenes/Level_4_Scene.unity (inspected prefab instance overrides)

### Systems Affected
- In-game HUD status messaging presentation
- Composure reward player feedback loop

### Gameplay/UI Changes
- Wired `InGameUIManager` serialized references in the prefab:
  - `inGameUICanvasGroup`
  - `PauseUICanvasGroup`
  - `statusMessageText`
  - `statusMessageCanvasGroup`
- Added a dedicated `Status Message Text` object under `In-Game UI Canvas` with:
  - `TextMeshProUGUI` component for runtime message content
  - `CanvasGroup` initialized hidden (`alpha = 0`) for fade-in/fade-out playback
- Serialized status fade timings in prefab to match script defaults.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Direct in-editor launches of scenes that do not include/instantiate the persistent `UI Manager` prefab will not show composure status text until that prefab is present.

## 2026-05-17 - Stage-Clear Temporary Buff Rewards + Weighted Card Pool

### Summary
Implemented stage-end reward cards as temporary buffs (one active stage card at a time), formalized what counts as a valid stage clear for card UI triggering, and expanded the buff card pool with weighted obtain rates.

### Files Affected
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Scripts/LevelGeneration/LevelRunManager.cs
- Assets/Scripts/Player/PlayerManager.cs
- Assets/Scripts/Data/Buffs Data/BuffCardData.cs
- Assets/Scenes/SampleScene.unity
- Assets/Buff Cards/Brute Force I.asset
- Assets/Buff Cards/Precision I.asset
- Assets/Buff Cards/Fleet foot.asset
- Assets/Buff Cards/Glass Cannon.asset
- Assets/Buff Cards/Momentum Rhythm.asset
- Assets/Buff Cards/Soul Siphon I.asset
- Assets/Buff Cards/Essence Surge I.asset
- Assets/Buff Cards/Kinetic Focus.asset
- Assets/Buff Cards/Windrunner.asset
- Assets/Buff Cards/Berserker Tempo.asset

### Systems Affected
- Stage clear detection and clear-reason signaling
- Reward screen trigger policy
- Temporary stage card buff lifecycle
- Card pool selection/randomization

### Gameplay/UI Changes
- Stage clear now exposes a typed clear context (`LevelClearContext`) from `LevelRunManager`, including:
  - clear reason (`RoomEvaluated`, `NoActiveSpawners`, `AlwaysUnlocked`, `SceneRequested`)
  - optional room report payload for combat clears
- RewardManager now uses detailed clear context instead of generic clear event to decide card reward UI trigger.
- Added configurable stage reward trigger mode:
  - `AnyStageClear`
  - `CombatRoomClear` (default)
  - `CalmCombatRoomClear`
- Buff cards are now explicitly temporary per stage selection:
  - previous card buffs are cleared before applying the newly selected stage card.
- Card offering changed from uniform random to weighted random without replacement.
- Added obtain-rate fields to card data:
  - `obtainWeight`
  - `calmStateBonusWeight` (extra weight on calm clears)
- Expanded available stage cards in `SampleScene` from 4 to 10.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- RewardManager card-pool expansion was serialized in `SampleScene`; if other scenes use separate RewardManager instances, mirror the card list there.
- In-editor Play Mode validation is still required for final balance feel and UI pacing across consecutive stage clears.

## 2026-05-17 - Reward Popup Reliability Fix (Cross-Scene Stage Flow)

### Summary
Fixed missing stage reward card popup in normal stage progression by making `RewardManager` available across scenes and adding a runtime fallback reward UI when scene-wired card UI is absent.

### Files Affected
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Scenes/SampleScene.unity

### Systems Affected
- Cross-scene reward manager lifecycle
- Stage-clear card reward popup reliability
- Runtime fallback UI and card-pool provisioning

### Gameplay/UI Changes
- Added RewardManager auto-bootstrap before scene load.
- RewardManager now persists with singleton behavior and prefers richer scene configuration when available.
- Added runtime fallback reward overlay (3 card choices) for scenes without prewired reward UI.
- Added runtime fallback card pool matching current temporary stage cards when no serialized card pool is present.
- Updated default stage-card trigger policy to `AnyStageClear` to avoid suppressed popups when clear reason is not room-evaluation.
- Updated `SampleScene` trigger mode to `AnyStageClear` for consistency.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Runtime fallback UI is functional and readable, but art polish still depends on using a fully scene-authored reward canvas.
- In-editor Play Mode validation is still required to tune exact timing/flow feel after clear events.

## 2026-05-17 - Reward Card Freeze Fix (Open/Select Stability)

### Summary
Fixed stage reward card flow freezes that could leave gameplay stuck at `Time.timeScale = 0` when the reward popup opened or when a card was selected.

### Files Affected
- Assets/Scripts/Interactables/RewardManager.cs
- Assets/Scripts/Interactables/BuffCardUI.cs
- Assets/Scripts/Player/PlayerManager.cs

### Systems Affected
- Reward popup transition lifecycle (fade in/out and pause state)
- Card click routing to active reward manager instance
- UI safety around missing or reloaded persistent HUD references

### Gameplay/UI Changes
- Added fade coroutine coordination in `RewardManager`:
  - Fade-in and fade-out now cancel each other cleanly to avoid `Time.timeScale` race conditions.
  - Reward popup now force-closes safely on scene load/disable/destroy and always restores `Time.timeScale` to `1`.
- Added runtime `EventSystem` bootstrap in `RewardManager` when missing, so reward cards remain clickable in combat scenes that lack scene-authored UI event input modules.
- Added guard to skip opening reward UI when zero valid card choices exist, preventing soft-locks on empty card pools.
- Added manager fallback lookup in `BuffCardUI` so card clicks still resolve if the serialized manager reference is stale.
- Added null-guarded HP UI updates in `PlayerManager` to avoid runtime null-reference spam when UI manager instances reload.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Full Play Mode validation is still needed for end-to-end feel verification during rapid card selection and immediate scene transitions.

## 2026-05-17 - Reward Popup Regression Fix (Post-Transition Suppression)

### Summary
Fixed a regression where reward cards could fail to appear after entering later stages because transition safety cleanup could hide a newly-triggered reward popup.

### Files Affected
- Assets/Scripts/Interactables/RewardManager.cs

### Systems Affected
- Scene transition reward popup lifecycle
- Stage-clear reward visibility consistency

### Gameplay/UI Changes
- Changed reward transition cleanup hook from `SceneManager.sceneLoaded` to `SceneManager.sceneUnloaded`.
- This preserves the safety cleanup for stale popups between scenes while preventing same-transition suppression of rewards that trigger during scene-load progression checks.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

### Known Limitations
- Unity Play Mode verification is still required to confirm expected popup cadence across full multi-stage and multi-floor traversal.

## 2026-05-18 - Glass Cannon Behavior Update (Double Deal / Double Receive)

### Summary
Replaced Glass Cannon behavior so it no longer reduces player max HP. The card now applies double outgoing damage and double incoming damage.

### Files Affected
- Assets/Scripts/Player/PlayerManager.cs

### Systems Affected
- Temporary card buff application
- Player damage dealt calculation
- Player incoming damage handling

### Gameplay Changes
- `MaxHealth` no longer depends on any Glass Cannon HP modifier.
- Glass Cannon now sets:
  - incoming damage multiplier = `2.0`
  - outgoing damage multiplier = `2.0`
- Incoming damage logging/emotion telemetry now records the final multiplied damage value.
- Clearing temporary card buffs resets Glass Cannon multipliers back to `1.0`.

### Build/Test
- `dotnet build reflex.sln` succeeded.
- Existing warning remains unrelated:
  - `Assets/Scripts/Movement/PlayerMovementManagement.cs(30,18) CS0649 isSprinting is never assigned`.

## 2026-05-17 - Weapon Equipped State Persistence

### Summary
Added persistence for the player's equipped weapon so it can be saved and reloaded across sessions.

### Files Affected
- Assets/Scripts/Combat/WeaponManager.cs

### Systems Affected
- Weapon Management
- Save/Data Persistence (`SaveManager`)

### Gameplay/UI Changes
- When a new weapon is equipped via `WeaponManager.EquipWeapon()`, it automatically passes the `weaponName` to `SaveManager.Instance.SetEquippedWeapon()`.
- Ensures that any manual weapon change correctly updates the active player save file during gameplay.

### Build/Test
- Tested via code inspection and `git diff`.

### Known Limitations
- Relies on `SaveManager` being instanced and properly implemented to handle file I/O operations.

## 2026-05-17 - Runtime Upgrade Station + Temporary Upgrade UI Bootstrap

### Summary
Implemented a fully runtime-bootstrapped upgrade flow so permanent upgrades can be accessed in Lobby without manual scene wiring, and ensured upgrades persist through `SaveManager`.

### Files Affected
- Assets/Scripts/Data/SaveManager.cs
- Assets/Scripts/Game/UpgradeManager.cs
- Assets/Scripts/Interactables/UpgradeStation.cs
- Assets/Scripts/Visuals/UpgradeUIManager.cs

### Systems Affected
- Save/load bootstrap lifecycle
- Permanent upgrade application and stat sync
- Lobby interactables
- Upgrade UI fallback flow

### Gameplay/UI Changes
- `SaveManager` now self-bootstraps at runtime and safely recreates a default save if existing JSON is unreadable.
- `UpgradeManager` now self-bootstraps at runtime and supports fallback upgrade tuning values when no `UpgradeSettings` asset is assigned.
- Upgrade purchases now:
  - spend essence from `SaveManager.currentSave`
  - save immediately to disk
  - reapply permanent bonuses to player stats
  - sync player runtime essence value for HUD correctness
- `UpgradeUIManager` now self-bootstraps and supports a temporary runtime UI fallback (`OnGUI`) when no scene-authored upgrade panel is assigned.
- `UpgradeStation` now:
  - ensures a trigger interaction collider is present
  - toggles upgrade UI through `UpgradeUIManager.IsOpen`
  - auto-spawns a runtime station in `Lobby` (with temporary placeholder visuals) when none exists.

### Build/Test
- Attempted `dotnet build reflex.sln` (elevated) but build validation is currently blocked by project file script-inclusion drift:
  - `SaveManager` references in `PlayerManager`/`WeaponManager` were unresolved by the generated `.csproj` include list.
- Unity Play Mode validation is still required for interaction feel and final placement tuning.

### Known Limitations
- Runtime station currently auto-spawns only in `Lobby`.
- Temporary upgrade UI uses IMGUI fallback styling and is intended as a placeholder until a scene-authored UI panel is wired.

## 2026-05-17 - Lobby-Only Upgrade Station + HP Bar Start Fill Fix

### Summary
Converted the upgrade station from runtime auto-spawn behavior to a scene-authored Lobby object, and fixed the HP bar initialization bug where the green bar could start at zero despite full HP text.

### Files Affected
- Assets/Scripts/Interactables/UpgradeStation.cs
- Assets/Scenes/Lobby.unity
- Assets/Scripts/Visuals/UI/InGameUIManager.cs
- Assets/Scripts/Player/PlayerManager.cs

### Scenes Affected
- Assets/Scenes/Lobby.unity

### Systems Affected
- Lobby interactable placement/authoring
- Upgrade station interaction flow
- In-game health UI initialization

### Gameplay/UI Changes
- Removed runtime station auto-spawn logic from `UpgradeStation`.
- Added a real `Upgrade Station` GameObject to the Lobby scene under `Interactables` with:
  - `UpgradeStation` component
  - trigger `BoxCollider` for interaction
  - visible mesh renderer/filter for clear in-scene presence
- `UpgradeStation` now focuses on interaction behavior and collider validation only.
- Added immediate HP fill sync API in `InGameUIManager`:
  - `SetHealthImmediate(currentHp, maxHp)` sets both red/green fill instantly and updates text.
- `PlayerManager` now initializes HP bars once UI becomes available at start:
  - avoids the start-of-lobby case where text shows `100/100` but green fill remains at `0`.
- `PlayerManager.Heal()` now also updates HP bar visuals immediately.

### Build/Test
- `dotnet build reflex.sln` succeeded (1 existing unrelated warning).
- Unity Editor Play Mode validation remains required for final in-game verification.

### Known Limitations
- Interaction prompt rendering still depends on `PlayerInteraction.uiElement` scene wiring (existing Lobby player instance currently has it unassigned).

