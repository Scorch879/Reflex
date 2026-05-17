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
- Stage order is now randomized per floor (with boss pinned as the final stage when available).
- Added a floor-specific debug HUD with auto-docking against the existing emotion debug HUD.
- Repaired broken default generation profile asset serialization (resolved merge-marker corruption).
- Door auto-binding now supports directional fallback naming used by non-door scene layouts.
- Single-route nodes now bind all discovered exits to the same destination for clearer traversal.
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
- Upgrade station is now scene-authored in `Lobby` (not runtime-spawned), with interaction collider and visible mesh.
- HP bar startup sync now initializes both green/red fill correctly at lobby start when HP is full.
- Buff cards now support run-persistent stacking with per-card stage duration (`buffDurationStages`).
- Special buff cards now support one-pick-per-run locking and contradiction blocking (`blockedCards`), including Fleet foot vs Windrunner exclusivity.
- Added a temporary runtime game-over summary overlay with run metrics and soul-essence calculation breakdown on player death.

## Active Priorities
- Playtest full path: Lobby (start only) -> Floor 1 stage chain -> Floor 2 stage chain (no lobby return).
- Validate that each new floor generates a different stage order and still ends with boss.
- Validate debug HUD readability and placement on multiple editor game-view resolutions.
- Validate that directional fallback exits in Lobby remain readable and reachable.
- Validate gameplay feel in Unity Play Mode across multiple rooms.
- Validate stage-duration expiration behavior for short-duration cards (for example Berserker Tempo).
- Validate special-card contradiction filtering and one-time pick constraints across long runs.
- Tune floor scaling fields:
  - `enemyHealthPerFloorStep`
  - `enemyDamagePerFloorStep`
  - `spawnCountPerFloorStep`
  - `respawnDelayReductionPerFloorStep`
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

## Remaining Tasks
- Decide whether to keep `Final Boss Level` as stage 5 each floor or introduce a separate standard stage 5 scene.
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
