## Current Game Stage
Lobby-first run flow is now wired with deterministic progression to boss, with active tuning still focused on adaptive difficulty/pressure through the emotion system.

## Current Scope
- Keep lobby as the build entry scene and first gameplay touchpoint.
- Maintain stable level progression from early floors through boss.
- Keep the player behavior analysis loop responsive and interpretable.
- Tune game adaptation to react without hard binary jumps.

## Completed Work
- Build scene order now boots through `Lobby` and includes boss/test scenes in Build Settings.
- Default run profile now uses a fixed stage ladder into final boss.
- Floor-loop logic is now active:
  - Floor 1 stage 1-5 -> Floor 2 stage 1-5 -> ...
- Lobby is now a one-time entry only (initial start), not a per-floor return hub.
- Stage order is now randomized per floor, with boss stages scheduled on floors 3, 6, 9... and pinned to the final stage when enabled.
- Added a floor-specific debug HUD with auto-docking against the existing emotion debug HUD.
- Repaired broken default generation profile asset serialization (resolved merge-marker corruption).
- Door auto-binding now supports directional fallback naming used by non-door scene layouts.
- Single-route nodes now bind all discovered exits to the same destination for clearer traversal.
- Door visuals now support animated leaf opening via `door_left` / `door_right` with per-instance axis-aware sliding.
- Entry-side doors are now blocked on non-lobby room entry to prevent immediate re-entry/backtracking through the same side.
- Door slide direction can now be overridden per door via `LevelDoorSlideSettings` component vectors for left/right leaf movement.
- Combat rooms now activate exactly one random exit door per scene entry (entry-side door excluded when possible).
- Post-clear doors now stay locked until buff selection is completed.
- Back-to-back door parents (`Door W` / `Door S`, including `Doors W` / `Doors S`) are now treated as linked groups and open together when selected.
- Door auto-discovery now includes directional aliases like `Walls N/E/S/W` alongside `Doors*` groups, with container filtering to prevent double-door candidate bias.
- Linked sibling pair objects (`Door/Doors S` + `Door/Doors W` under one parent) are now treated as one logical random-choice group.
- No-door auto-advance fallback now prevents dead-end progression in scenes without explicit door candidates.
- Floor difficulty scaling now applies to enemy health, enemy damage, spawn count, and respawn delay.
- Emotion engine records and scores player behavior using live and recent room signals.
- Room lifecycle integration across spawners is implemented.
- Emotion director applies adaptation directives to enemies and spawning.
- Continuous adaptation blend added from aggression score + confidence.
- Continuous respawn timing scaling added in spawner.
- Debug HUD now exposes adaptation blend/confidence.
- Chase behavior now consumes director standoff/retreat tactical destination.
- World tint is now applied in scene visuals (ambient + camera background).
- Calm relief charges now provide tactical pressure reduction in the next room after calm clears.
- Composure room clears now award bonus Soul Essence with quality scaling.
- Emotion hit scoring now mitigates stacked multi-hit aggression spikes via effective-hit diminishing returns.
- Emotion aggression tempo now uses slower rise / faster fall with passive calm decay after short combat inactivity.
- Aggression state now uses a more forgiving threshold/weight profile with passive-disengage bias to avoid passive play being misclassified as aggressive.
- Emotion telemetry now rebases on combat floor entry with stale-room cleanup to keep updates reliable across progression.
- Emotion telemetry/evaluation writes are now combat-only to prevent out-of-combat score drift.
- Forgiveness profile was tightened after over-calm feedback.
- Upgrade station is now scene-authored in `Lobby` (not runtime-spawned), with interaction collider and visible mesh.
- HP bar startup sync now initializes both green/red fill correctly at lobby start when HP is full.
- Buff cards now support run-persistent stacking with per-card stage duration (`buffDurationStages`).
- Special buff cards now support one-pick-per-run locking and contradiction blocking (`blockedCards`), including Fleet foot vs Windrunner exclusivity.
- RewardManager singleton ownership now prioritizes scene-authored instances over bootstrap fallback, so Lobby-configured inspector card pools are used reliably.
- Added a temporary runtime game-over summary overlay with run metrics and soul-essence calculation breakdown on player death.
- Equipped-weapon restore no longer requires manually maintaining `SaveManager.availableWeapons`; runtime weapon discovery now resolves saved weapon names automatically.
- Game-over screen now supports scene-authored Canvas binding through `TemporaryGameOverCanvasView`, enabling direct in-editor layout/design iteration.
- Game-over flow now includes a Return to Lobby button (authored-canvas and runtime fallback), with optional fresh-run regeneration on return.
- Return-to-lobby from game-over now performs a full respawn reset (clears in-run card buffs and revives player state) before loading Lobby.
- Added `WeaponManager.HitboxOn()` combo-index safety guards to prevent post-reset animation-event `IndexOutOfRangeException` crashes.
- Enemy spawners now support floor-scaled additional wave sequencing with queued-wave tracking via `HasUpcomingWave`.
- Room clear now defers while upcoming waves are queued, preventing premature stage clear and buff-card reward flow.

## Active Priorities
- Playtest full path: Lobby (start only) -> Floor 1 stage chain -> Floor 2 stage chain (no lobby return).
- Validate that each new floor generates a different stage order, with boss appearing only on floors 3/6/9... and at final stage on those floors.
- Validate debug HUD readability and placement on multiple editor game-view resolutions.
- Validate that directional fallback exits in Lobby remain readable and reachable.
- Validate entry-door lock selection per scene (including double-door sides) to ensure the intended incoming side is blocked.
- Validate single-random-door selection across full floor traversal to ensure route readability and no dead-end cases.
- Validate linked back-to-back parents open together and never split-route across the pair.
- Validate post-clear flow: clear room -> choose buff -> door unlock.
- Validate gameplay feel in Unity Play Mode across multiple rooms.
- Validate stage-duration expiration behavior for short-duration cards (for example Berserker Tempo).
- Validate special-card contradiction filtering and one-time pick constraints across long runs.
- Validate equipped-weapon persistence across full app restart without any manual weapon-list setup in inspector.
- Design and iterate the authored game-over canvas visual style/spacing/typography now that the runtime hook is in place.
- Wire and style the authored Return to Lobby button in the final UI prefab/canvas for production polish.
- Validate death -> Return to Lobby -> immediate re-entry loop for state correctness (movement/attack enabled, HP full, no lingering dead state).
- Validate attack animation events immediately after respawn/death transitions to confirm no invalid combo-step event fires.
- Tune floor scaling fields:
  - `enemyHealthPerFloorStep`
  - `enemyDamagePerFloorStep`
  - `spawnCountPerFloorStep`
  - `respawnDelayReductionPerFloorStep`
- Tune wave sequencing fields:
  - `enableAdditionalWaves`
  - `maxWavesPerRoom`
  - `additionalWaveChanceFloorOne`
  - `additionalWaveChancePerFloor`
  - `maxAdditionalWaveChance`
  - `respawnDelay`
- Tune blend fields:
  - `confidenceBlendFloor`
  - `profileUpdateLogBlendDelta`
  - `respawnRateConfidenceFloor`
- Tune calm motivation fields:
  - Calm relief eligibility thresholds and effect multipliers
  - Composure essence bonus thresholds and payout curve
- Tune anti-spike fields:
  - `multiHitBurstWindow`
  - `additionalHitFalloff`
  - `maxEffectiveHitsPerAttack`
- Tune tempo fields:
  - `aggressionRiseSmoothing`
  - `aggressionFallSmoothing`
  - `calmDecayDelay`
  - `calmDecayPerSecond`
  - `attackIntentScale`
  - `hitIntentScale`
  - `passiveRecoveryBoost`
  - `passiveForgivenessBias`
- Tune progression stability fields:
  - `rebaseTelemetryOnLevelEntered`
  - `levelCarryoverFactor`
  - `clearRoomStateOnLevelEntered`
- Tune strictness fields after combat-only gating:
  - `aggressiveThreshold` / `calmThreshold`
  - `attackIntentScale` / `hitIntentScale`
  - `calmDecayDelay` / `calmDecayPerSecond`

## Remaining Tasks
- Decide whether boss cadence should remain fixed at every 3 floors or be moved into profile/runtime tuning.
- Playtest calm-to-aggressive transitions and aggressive-to-calm recovery.
- Validate that room pacing remains readable at high spawn density.
- Confirm enemy containment behavior still feels intentional under blended values.
- Replace temporary runtime game-over overlay with final authored UI design once art/UI pass is ready.

## Known Bugs
- Existing warning persists: `PlayerMovementManagement.isSprinting` is never assigned.

## Known Blockers
- In-editor playtesting not executed in this session.

## Systems In Progress
- Level flow validation and scene progression polish.
- Emotion engine tuning and pacing balance.
- Buff-card balance tuning and contradiction-rule coverage.

## Testing Status
- Build test: pass (`dotnet build reflex.sln`).
- Runtime gameplay test: pending (Unity Editor Play Mode).
