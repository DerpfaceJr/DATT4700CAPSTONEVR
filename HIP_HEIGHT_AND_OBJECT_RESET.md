# Hip Height Adjustment & Object Reset System ✅

## Features Implemented

### 1. Adjustable Hip Height for Rigged Enemies
- ✅ Hip Y position now configurable in Inspector
- ✅ Default value: 1.0 (standard standing height)
- ✅ Easily adjustable for calibration

### 2. Object Position Reset Between Rounds
- ✅ Automatically resets throwables and weapons at round start
- ✅ Tracks initial positions and restores them
- ✅ Resets physics velocities and rotations
- ✅ Works for swords, cubes, balls, and all throwable objects

---

## Part 1: Hip Height Adjustment

### Implementation (`EnemyIKReplay.cs`)

**New Field (Line ~15):**
```csharp
[Header("Character Positioning")]
[Tooltip("Y position for character hips/root (adjustable for height calibration)")]
public float hipHeight = 1.0f;
```

**Modified Positioning (Line ~210, ~267):**
```csharp
// In ApplyFrame() and ApplyFrameInterpolated()
targetPos.y = hipHeight; // Use adjustable hip height instead of recorded Y
```

### How It Works

**Before:**
```
Enemy uses recorded player Y position (varies by player)
→ Can cause inconsistent enemy heights
→ Difficult to adjust
```

**After:**
```
Enemy uses hipHeight field (default 1.0)
→ Consistent enemy height every round
→ Easily adjustable in Inspector
```

### Adjusting Hip Height

1. **Select rigged enemy** in Hierarchy (during Play Mode)
   - Or select enemy prefab in Project

2. **Find EnemyIKReplay component** in Inspector

3. **Adjust "Hip Height" slider**
   - **0.5**: Very short/crouched
   - **1.0**: Standard standing (default) ← recommended
   - **1.5**: Tall character
   - **2.0**: Very tall

4. **Effects are immediate** (during Play Mode)

### Why This Helps

**Problem**: Different players record at different heights
- Tall player: Recorded Y = 1.2
- Short player: Recorded Y = 0.8
- Result: Inconsistent enemy heights

**Solution**: Override with fixed hipHeight
- All enemies: hipHeight = 1.0
- Consistent height regardless of player
- Easy calibration via Inspector

---

## Part 2: Object Position Reset System

### New Script: `ObjectPositionResetter.cs`

**Automatically tracks:**
- All objects with tags: "Throwable", "Sword", "Weapon", "Melee"
- All objects with `BuildingBlockProjectile` component
- All objects with `SwordPhysicsFix` component
- Manually assigned objects

**Resets on round start:**
- Position (back to spawn location)
- Rotation (back to original orientation)
- Physics velocities (stop all movement)
- Kinematic state (restore original)

### How It Works

```
Round Start
    ↓
ObjectPositionResetter.FindAndTrackObjects()
    ↓
Stores initial position/rotation for each object
    ↓
Round progresses (objects thrown, moved, etc.)
    ↓
Round End → New Round Start
    ↓
ObjectPositionResetter.ResetObjects()
    ↓
All objects return to starting positions
    ↓
Player has fresh arena!
```

### Configuration

**In Inspector (RoundManager > ObjectPositionResetter):**

| Setting | Default | Description |
|---------|---------|-------------|
| **Auto Find Objects** | ✓ | Automatically find and track objects on Start |
| **Reset On Round Start** | ✓ | Enable/disable reset system |
| **Reset Velocities** | ✓ | Stop all physics movement |
| **Reset Rotations** | ✓ | Restore original orientation |
| **Trackable Tags** | ["Throwable", "Sword", "Weapon", "Melee"] | Tags to search for |
| **Manual Objects** | [] | Manually assign specific objects |

### Integration with RoundManager

**Modified `RoundManager.cs` (Line ~26, ~118):**

```csharp
// Reference added
public ObjectPositionResetter objectResetter;

// Called at round start
if (objectResetter != null)
{
    objectResetter.ResetObjects();
}
```

---

## Testing

### Test 1: Hip Height Adjustment

1. **Enter Play Mode**
2. **Complete Round 1**
3. **Round 2 enemy spawns**
4. **Select enemy in Hierarchy**
5. **Find EnemyIKReplay component**
6. **Change "Hip Height" value** (try 0.5, 1.0, 1.5, 2.0)
7. **Observe enemy height change** in real-time

**Expected:**
- ✅ Enemy height adjusts immediately
- ✅ Character stays standing (not sitting)
- ✅ Feet remain on ground
- ✅ Height consistent regardless of recorded data

### Test 2: Object Reset System

1. **Enter Play Mode**
2. **Note starting positions** of sword and throwables
3. **Throw objects around the arena**
4. **Move sword to far location**
5. **Complete Round 1**
6. **Round 2 starts**
7. **Check Console and object positions**

**Expected Console:**
```
ObjectPositionResetter: Found RoundManager, ready to reset X objects
ObjectPositionResetter: Tracking Sword at position (x, y, z)
ObjectPositionResetter: Tracking Cube at position (x, y, z)
Round 2: Reset all throwable objects and weapons
ObjectPositionResetter: Reset X objects to starting positions
```

**Expected Result:**
- ✅ All objects back at starting positions
- ✅ All rotations reset to original
- ✅ All physics velocities zeroed
- ✅ Objects ready for new round

### Test 3: Manual Object Tracking

1. **Exit Play Mode**
2. **Select RoundManager**
3. **Find ObjectPositionResetter component**
4. **Expand "Manual Objects" array**
5. **Add specific objects** (drag from Hierarchy)
6. **Enter Play Mode**
7. **Verify those objects are tracked**

**Expected:**
- ✅ Manually added objects tracked
- ✅ Console shows tracking messages
- ✅ Objects reset on round start

---

## Configuration Options

### Hip Height Presets:

| Height | Use Case |
|--------|----------|
| 0.5 | Crouched/low enemy |
| 0.8 | Short character |
| 1.0 | Standard adult (default) |
| 1.2 | Tall character |
| 1.5 | Very tall |
| 2.0 | Giant |

### Object Reset Options:

**To disable reset for specific rounds:**
```csharp
// In RoundManager.cs, before objectResetter.ResetObjects():
if (currentRound > 1) // Only reset after Round 1
{
    objectResetter.ResetObjects();
}
```

**To reset at different times:**
```csharp
// Call manually from any script:
ObjectPositionResetter resetter = FindObjectOfType<ObjectPositionResetter>();
resetter.ResetObjects();
```

**To update stored positions mid-game:**
```csharp
// If you move objects and want to set new "home" positions:
resetter.UpdateStoredPositions();
```

---

## Advanced Features

### ObjectPositionResetter Public Methods:

```csharp
// Find and track all objects (called automatically on Start)
resetter.FindAndTrackObjects();

// Track a specific object
resetter.TrackObject(myGameObject);

// Reset all tracked objects (called automatically on round start)
resetter.ResetObjects();

// Update stored positions (new "home" for all objects)
resetter.UpdateStoredPositions();

// Get count of tracked objects
int count = resetter.GetTrackedObjectCount();
```

### Custom Integration Example:

```csharp
// In your custom script:
void OnCustomEvent()
{
    ObjectPositionResetter resetter = FindObjectOfType<ObjectPositionResetter>();
    
    // Reset only if certain condition
    if (someCondition)
    {
        resetter.ResetObjects();
    }
    
    // Or update positions after player rearranges objects
    if (playerRearrangedObjects)
    {
        resetter.UpdateStoredPositions();
    }
}
```

---

## Troubleshooting

### Problem: Hip height doesn't change

**Check:**
1. Is field set in Inspector? (Look for "Hip Height")
2. Is character using EnemyIKReplay component?
3. Is character actually a rigged enemy (not static)?

**Solution:**
- Only rigged enemies (Round 2+) use EnemyIKReplay
- Static enemy (Round 1) doesn't have adjustable height
- Check console for "EnemyIKReplay" messages

### Problem: Objects don't reset

**Check:**
1. ObjectPositionResetter on RoundManager? ✓
2. "Reset On Round Start" enabled? (should be ✓)
3. Objects being tracked? (check console)
4. RoundManager calling ResetObjects()?

**Expected Console:**
```
ObjectPositionResetter: Now tracking X total objects
Round 2: Reset all throwable objects and weapons
ObjectPositionResetter: Reset X objects to starting positions
```

**If no messages:**
- ObjectPositionResetter not attached
- Or autoFindObjects = false
- Or no objects match trackable tags

### Problem: Some objects not resetting

**Check:**
1. Do they have trackable tags? ("Sword", "Throwable", etc.)
2. Do they have BuildingBlockProjectile or SwordPhysicsFix?
3. Are they manually added to Manual Objects array?

**Solution:**
Add missing objects manually:
1. Select RoundManager
2. Find ObjectPositionResetter
3. Expand "Manual Objects"
4. Drag object from Hierarchy to array

### Problem: Objects reset to wrong position

**Cause:** Initial positions stored incorrectly

**Solution:**
```csharp
// Reposition objects where you want them
// Then call:
resetter.UpdateStoredPositions();
// New positions now stored as "home"
```

---

## Files Modified

1. ✅ `Assets/Scripts/EnemyIKReplay.cs`
   - Added `hipHeight` field (line ~15)
   - Modified `ApplyFrame()` to use hipHeight (line ~213)
   - Modified `ApplyFrameInterpolated()` to use hipHeight (line ~267)

2. ✅ `Assets/Scripts/RoundManager.cs`
   - Added `objectResetter` reference (line ~28)
   - Auto-find objectResetter in Start() (line ~66)
   - Call ResetObjects() on round start (line ~121)

3. ✅ `Assets/Scripts/ObjectPositionResetter.cs` (NEW)
   - Complete object tracking and reset system

4. ✅ Scene Updated
   - ObjectPositionResetter added to RoundManager GameObject

---

## Summary

### Hip Height:
✅ **Adjustable in Inspector** (default 1.0)
✅ **Consistent enemy height** across rounds
✅ **Real-time adjustment** during Play Mode
✅ **Easy calibration** for different scenarios

### Object Reset:
✅ **Automatic tracking** of throwables and weapons
✅ **Reset on round start** (positions, rotations, velocities)
✅ **Configurable** (enable/disable, manual objects, etc.)
✅ **Console feedback** for debugging

### Benefits:
- Consistent enemy positioning
- Clean arena between rounds
- Easy height calibration
- Better player experience
- No scattered objects

**Both systems are now active and ready for testing!** 🎮
