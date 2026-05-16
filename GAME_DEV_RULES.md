```
# GAME_DEV_RULES.md — Game Development Rules

## Always Do First
- Before writing or changing any game code, inspect the existing project
structure.
- Read all available project instruction files first.

Common instruction files may include:

```txt
AGENTS.md
AGENTS.MD
PROJECT_RULES.md
DEV_NOTES.md
README.md
GAME_DESIGN.md
TECHNICAL_DESIGN.md
Understand the current engine, framework, folder structure, coding style, and
gameplay architecture before making changes.
Do not rewrite systems from scratch unless the user specifically requests it.
Project Understanding

Before implementing anything, identify:

Game engine or framework being used
Main gameplay loop
Core player mechanics
Existing scene structure
Asset structure
Input system
Save/load system, if any
UI/HUD system
Audio system
Current build or run workflow

Do not assume the project is blank unless it truly is.

Reference Images, Videos, or Game Examples
If a reference image, video, or game example is provided, match the intended
feel as closely as possible.
Study and replicate relevant details such as:
Camera angle
Player movement feel
UI layout
Art direction
Lighting
Color palette
Animation timing
Combat rhythm
Level composition
Feedback effects
Audio mood
Do not “improve” or reinterpret the reference unless requested.
The goal is to match the reference direction first, then polish.

If placeholder content is needed, use clearly temporary assets and mark them as
placeholders.

Gameplay First Rule
Prioritize gameplay feel over visual complexity.
A feature is not done just because it works technically.
It must feel good, readable, responsive, and understandable to the player.
Always consider:
Responsiveness

```

```
Timing
Feedback
Player control
Difficulty clarity
Visual readability
Audio feedback
Game loop impact
Core Gameplay Loop
Every feature should support the core gameplay loop.
Do not add mechanics that do not serve the current loop.
Before adding a new system, identify how it affects:
Player goal
Challenge
Reward
Progression
Replayability
Balance
Player retention
Local Build and Run Workflow
Always run and test the game locally after changes.
Use the project’s existing run/build command when available.
Do not assume code is correct without launching or testing the game.

Common examples:

npm run dev
npm run build
npm run start

For Unity:

Open the project in Unity Editor and enter Play Mode.

For Unreal:

Open the project in Unreal Editor and use Play-In-Editor.

For Godot:

Open the project in Godot and run the main scene.
If a local server is required, use the project’s existing server command.
Do not start duplicate server instances if one is already running.
Screenshot and Capture Workflow
Take screenshots or screen recordings after meaningful visual or gameplay
changes.
Compare the result against the reference, if one exists.
Do at least 2 review passes for visual, UI, or feel-based work.

When reviewing screenshots or gameplay captures, check:

Camera position
Player scale
Environment scale
UI placement
Spacing
Font size
Lighting
Color accuracy
Character visibility
Enemy visibility
Hit effects
Animation poses
Particle effects
Scene composition

```

```
Performance issues
Visual clutter

When comparing, be specific:

Player character is too large compared to the environment.
Camera is too close and reduces situational awareness.
HUD spacing is tighter than the reference.
Hit flash is too subtle and needs stronger feedback.
Jump arc feels floaty compared to the intended feel.
Output Defaults
Follow the existing project structure.
Do not force everything into a single file unless the user specifically asks.
Keep systems modular and easy to maintain.
Prefer readable, simple code over overly clever code.
Use descriptive file, class, variable, and function names.
Keep placeholder assets clearly separated from production assets.
Asset Rules
Always check the project’s asset folders before creating or using placeholders.

Common asset folders may include:

assets/
Assets/
public/
sprites/
textures/
models/
audio/
ui/
fonts/
brand_assets/
If real assets exist, use them.
Do not replace real assets with placeholders.
Do not invent new brand colors, logos, character designs, or UI themes if
official assets already exist.
If a style guide or palette exists, follow it exactly.
Game Art Direction
Do not create generic-looking game visuals.
Every scene, UI screen, character, and effect should support a clear art
direction.
Avoid default engine visuals unless they are intentionally part of the style.
Avoid generic placeholder gray boxes in final-facing work.
Use consistent:
Color palette
Lighting style
Shape language
UI style
Icon style
Animation style
Visual effects style
Anti-Generic Game Design Guardrails
Visual Style
Do not rely on default engine materials, default colors, or generic UI styling.
Choose a clear visual identity.
Keep all screens and gameplay elements visually consistent.
Make important gameplay objects easy to recognize.
Lighting
Lighting should support mood, readability, and gameplay.
Avoid flat lighting unless intentionally stylized.
Use contrast to guide the player’s eye.
Important objects, enemies, exits, and interactables should be readable.
Camera
Camera behavior must support gameplay.

```

```
Avoid camera movement that fights player control.
Consider:
Follow speed
Dead zones
Zoom level
Screen shake
Collision handling
Field of view
Player visibility
Enemy visibility
Camera changes must be tested during gameplay, not just viewed in editor.
Controls
Controls must feel responsive.
Avoid unnecessary input delay.
Always test:
Movement
Jumping
Attacking
Dodging
Interacting
Menu navigation
Controller support, if applicable
Input should feel predictable and consistent.
Movement
Movement should be tuned, not just functional.
Check:
Acceleration
Deceleration
Jump height
Gravity
Air control
Friction
Dash distance
Turn speed
Sprint speed
Player movement should match the intended genre and game feel.
Combat
Combat must have clear feedback.
Attacks should communicate:
Startup
Active frames
Recovery
Hit confirmation
Damage taken
Knockback
Cooldowns
Use effects carefully:
Hit stop
Screen shake
Flash
Sound
Particles
Damage numbers, if appropriate
Do not make combat feel silent, weightless, or unclear.
UI and HUD
UI must be readable during gameplay.
Do not overload the HUD.
Important information should be visible at a glance.
Menus must support:
Hover state
Focus state
Active/pressed state
Keyboard/controller navigation when applicable
UI should match the game’s art direction.

```

```
Audio
Audio is part of game feel.
Add or preserve sound feedback for:
Button clicks
Movement actions
Attacks
Hits
Damage
Rewards
Errors
Transitions
Avoid silent interactions.
Use consistent volume levels.
Do not let music overpower important feedback sounds.
Animation
Animations should communicate gameplay state clearly.
Every important action should have readable timing.
Check:
Idle
Walk/run
Jump
Fall
Land
Attack
Hit reaction
Death
Interact
UI transitions
Avoid snapping unless intentionally stylized.
Use animation events carefully and document them.
Effects
Visual effects should support player understanding.
Do not add excessive particles that hide gameplay.
Effects should communicate:
Damage
Healing
Pickup
Level up
Danger
Interactions
Ability activation
Keep effects consistent with the art style.
Level Design
Levels should guide the player naturally.
Avoid random object placement.
Check:
Player path
Landmarks
Enemy placement
Reward placement
Difficulty curve
Safe zones
Checkpoints
Camera visibility
Make sure players understand where to go without excessive explanation.
Game Balance
Do not add rewards, damage values, cooldowns, or prices randomly.
Balance values should be intentional and documented.
When changing balance, consider:
Early game
Mid game
Late game
New players
Skilled players

```

```
Economy impact
Progression speed
Avoid overpowered or useless mechanics.
Performance
Game changes must consider performance.
Watch for:
Too many update loops
Unnecessary physics checks
Too many particles
Large uncompressed textures
Excessive draw calls
Memory leaks
Expensive UI updates
Unbatched objects
Do not introduce performance-heavy systems without a reason.
Save Data
Be careful when changing progression, inventory, player stats, wallet, currency,
or unlock systems.
Do not break existing save files.
If save data structure changes, document migration needs.
Avoid deleting or renaming save keys without handling backward compatibility.
Code Quality Rules
Keep gameplay systems modular.
Avoid giant manager files unless already part of the project architecture.
Avoid hardcoding values when they should be tunable.
Use config objects, ScriptableObjects, data tables, JSON, or constants where
appropriate.
Keep magic numbers documented.
Separate:
Gameplay logic
UI logic
Input logic
Audio logic
Save/load logic
Visual effects logic
Do not duplicate systems that already exist.
Extend existing systems when appropriate.
Testing Rules

After making gameplay changes, test the actual gameplay path.

Always check:

Game launches without errors
Main scene loads
Player can control the character
Feature works in normal flow
Feature works after restart, if save-related
UI does not overlap or break
No console errors
No major frame drops
Existing features still work

For bug fixes, verify:

The bug is reproduced before fixing, when possible
The fix addresses the root cause
The fix does not create a new issue
Edge cases are tested
Documentation Notes
After every meaningful change, update or create documentation notes.
This applies to:
New gameplay features
Bug fixes

```

```
Design updates
Balance changes
Asset changes
File creation
Refactors
Scene changes
UI/HUD changes
Input changes
Save/load changes
Notes Folder
Keep all documentation notes inside a project notes folder.

Recommended folder name:

Project_Notes/

Recommended primary files:

Project_Notes/change-documentation.md
Project_Notes/current-project-status.md
Change Documentation

Use change-documentation.md as the main running changelog.

Each entry should include:

Date
Summary of the change
Files affected
Scenes affected
Systems affected
Gameplay behavior changed
Design decisions made
Balance values changed
Known limitations
Follow-up tasks

Example format:

## 2026-05-16 — Player Dash Tuning

### Summary
Adjusted player dash behavior to feel faster and more responsive.

### Files Affected
- PlayerController.cs
- PlayerMovementConfig.asset

### Gameplay Changes
- Dash distance increased from 4.0 to 5.5 units.
- Dash cooldown reduced from 1.2s to 0.8s.
- Added brief input lock during dash recovery.

### Design Notes
- Dash now feels closer to the intended fast-action movement style.
- Recovery prevents dash spam from becoming uncontrollable.

### Known Limitations
- Needs controller testing.
- Dash VFX still uses placeholder particle effect.
Current Project Status

Use current-project-status.md as the active development tracker.

```

```
Keep it updated with:

Current game stage
Current scope
Completed work
Active priorities
Remaining tasks
Known bugs
Known blockers
Systems in progress
Testing status
Per-System or Per-Feature Notes

Each major system, mechanic, level, or feature should have its own note file.

Examples:

Project_Notes/player-controller.md
Project_Notes/combat-system.md
Project_Notes/inventory-system.md
Project_Notes/save-load-system.md
Project_Notes/main-menu.md
Project_Notes/level-01.md
Project_Notes/enemy-ai.md
Project_Notes/audio-system.md
If a change affects an existing system, update the existing note.
Do not create duplicate notes for the same system.
If a brand-new system or level is created, create a new note file for it.
Documentation Format
Keep notes detailed but scannable.
Use:
Headings
Bullet points
Tables
Short summaries
Clear file references
Before/after values when tuning gameplay
Non-Negotiable Documentation Rule
No development session should end without documentation being updated.
Notes must reflect the current state of the project.
Hard Rules
Do not add gameplay systems, mechanics, UI screens, enemies, items, or content
that were not requested.
Do not redesign the game direction unless requested.
Do not overwrite existing systems without understanding them first.
Do not ignore existing assets, scenes, prefabs, or project architecture.
Do not replace real assets with placeholders.
Do not make gameplay changes without testing them in-game.
Do not treat technical completion as gameplay completion.
Do not ship silent interactions when feedback is needed.
Do not add unbalanced values without documenting them.
Do not break existing save data.
Do not create duplicate managers or duplicate systems.
Do not leave console errors unresolved.
Do not ignore performance.
Do not finish a session without updating documentation notes.

Good neutral filenames for this:

```txt
GAME_DEV_RULES.md
GAME_PROJECT_RULES.md
GAMEPLAY_IMPLEMENTATION_RULES.md
GAME_AGENT_GUIDE.md

```

```
PROJECT_RULES.md

Best general-purpose name: GAME_DEV_RULES.md.

```

