## Player Manager

### Current State
- `PlayerManager` owns core player runtime state such as health, death, attack flags, temporary card buffs, soul essence, and immortality.
- `isImmortal` is a testing/debug flag that causes `TakeDamage()` to return before applying incoming damage.

### Debug Controls
- Press `=` during Play Mode to toggle `isImmortal`.
- The key is serialized as `immortalToggleKey` in `PlayerManager` and defaults to `Key.Equals`.
- Toggling writes a console log showing whether immortality is enabled or disabled.

### Files
- `Assets/Scripts/Player/PlayerManager.cs`

### Testing Status
- C# build passes with `dotnet build Assembly-CSharp.csproj -nologo --no-incremental`.
- Unity Play Mode validation is still required to confirm the key toggles immortality and damage is ignored while enabled.
