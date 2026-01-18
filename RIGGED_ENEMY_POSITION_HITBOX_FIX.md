# Rigged Enemy Position & Hitbox Fix ✅

## Issues Fixed
1. ❌ **Rigged enemies appearing sitting down** (Y position too low)
2. ❌ **Inaccurate hitbox** (collider not properly aligned with standing character)

## Root Cause

### Problem 1: Low Y Position
The `minYPosition` calculation was constraining enemies to the **bottom of the collider**:
```csharp
// OLD (WRONG):
minYPosition = capsule.center.y - (capsule.height / 2f);
// Result: 1.05 - 0.95 = 0.1 (way too low!)
```

This forced the character root to Y=0.1, but the player's actual recorded position is around Y=1.0, causing the character to appear crouched/sitting.

### Problem 2: Inaccurate Collider
The CapsuleCollider was:
- Center: Y=1.05
- Height: 1.9
- Bottom: 1.05 - 0.95 = **0.1** (should be 0)
- Top: 1.05 + 0.95 = **2.0** ✓

This misalignment caused hitbox issues.

---

## Solutions Applied

### Fix 1: Proper Collider Dimensions (`EnemySpawner.cs` line ~209)

**Changed from:**
```csharp
capsule.center = new Vector3(0f, 1.05f, 0f);
capsule.height = 1.9f;
// Bottom at 0.1, Top at 2.0
```

**Changed to:**
```csharp
capsule.center = new Vector3(0f, 1.0f, 0f);  // Center at waist
capsule.height = 2.0f;  // Full standing height
// Bottom at 0.0 (ground), Top at 2.0 (head)
```

### Fix 2: Proper Y Position Constraint (`EnemyIKReplay.cs` line ~54)

**Changed from:**
```csharp
// OLD: Constrain to bottom of collider
minYPosition = capsule.center.y - (capsule.height / 2f);
// Result: Forces character down to 0.1
```

**Changed to:**
```csharp
// NEW: Minimal constraint, just prevent floor clipping
minYPosition = 0.05f; // Very small offset from ground
// Result: Character uses recorded Y position (~1.0)
```

### Fix 3: Better Ground Detection (`EnemyIKReplay.cs` line ~183)

**Improved EstablishGroundLevel():**
```csharp
// Detects actual ground via raycast
float groundY = hit.point.y;
minYPosition = Mathf.Max(groundY + 0.05f, 0.05f);
// Sets minimum just above ground, not at collider bottom
```

---

## How It Works Now

### Character Positioning:
```
Player recorded Y position: ~1.0
    ↓
Enemy replays with frame.position.y = 1.0
    ↓
minYPosition constraint = 0.05
    ↓
constrainedY = Max(1.0, 0.05) = 1.0 ✓
    ↓
Character stands at proper height!
```

### Collider Alignment:
```
Character root at Y=1.0
    ↓
CapsuleCollider center at Y=1.0 (waist)
    ↓
Height = 2.0
    ↓
Bottom: 1.0 - 1.0 = 0.0 (feet at ground) ✓
Top: 1.0 + 1.0 = 2.0 (head) ✓
    ↓
Perfect standing hitbox!
```

---

## Before vs After

### Before (WRONG):
```
Root Y: 0.1 (too low)
Collider bottom: 0.1
Collider center: 1.05
Collider top: 2.0
Result: Character crouched, legs through floor ❌
```

### After (CORRECT):
```
Root Y: 1.0 (player height)
Collider bottom: 0.0 (at ground)
Collider center: 1.0 (at waist)
Collider top: 2.0 (at head)
Result: Character standing naturally ✓
```

---

## Testing

### Visual Test:
1. **Enter Play Mode**
2. **Complete Round 1**
3. **Observe Round 2 enemy**

**Expected:**
- ✅ Enemy stands upright (not sitting)
- ✅ Body aligned with ground (feet at Y=0)
- ✅ Natural standing posture
- ✅ Character height ~2 units

### Hitbox Test:
1. **Swing sword at enemy's torso**
2. **Check for hit registration**

**Expected:**
- ✅ Hits register on full body
- ✅ Head hits work (at Y=2.0)
- ✅ Chest hits work (at Y=1.0-1.5)
- ✅ Leg hits work (at Y=0.5)

### Console Messages:
```
EnemyIKReplay: Set minimum Y position = 0.05 (ground level + small offset)
EnemyIKReplay: Established ground level at Y=0.05 (detected ground at 0.0)
[CombatSystem] SWORD KILL! Sword hit Enemy_Round2 (velocity: X.XX m/s)
```

---

## Technical Details

### Collider Dimensions:
- **Center**: Y=1.0 (waist/center of mass)
- **Height**: 2.0 (from feet to head)
- **Radius**: 0.35 (narrow for precise hits)
- **Direction**: 1 (Y-axis vertical)

### Position Constraints:
- **minYPosition**: 0.05 (just above ground)
- **Recorded Y**: ~1.0 (player standing height)
- **Actual Y**: Max(recorded, 0.05) = 1.0 ✓

### Rigidbody Setup:
- **Mass**: 70kg (human weight)
- **Drag**: 2.0 (resistance)
- **Angular Drag**: 5.0 (rotation resistance)
- **Constraints**: Freeze X/Z rotation (prevent tipping)
- **Collision Detection**: Continuous (accurate)

---

## Why This Fix Works

### Problem: Collider-Based Constraint
The old code calculated `minY` from the collider's bottom:
- Collider bottom = center - height/2
- This created a **derived constraint** based on collider dimensions
- Wrong approach: collider should match character, not define it!

### Solution: Natural Height Constraint
The new code uses a **minimal ground-level constraint**:
- minY = 0.05 (just above floor)
- Character uses its **natural recorded height** (~1.0)
- Collider **matches** this natural height
- Right approach: character position drives collider placement!

---

## Configuration Options

### Adjust Character Height:
If players record at different heights, the system automatically adapts:
- Tall player (Y=1.2) → Enemy stands at Y=1.2 ✓
- Short player (Y=0.8) → Enemy stands at Y=0.8 ✓
- minY constraint only prevents floor clipping

### Adjust Collider Size:
In `EnemySpawner.cs` line ~212:
```csharp
capsule.center = new Vector3(0f, 1.0f, 0f);  // Waist height
capsule.radius = 0.35f;  // Thinner = more precise
capsule.height = 2.0f;  // Taller = larger hitbox
```

---

## Troubleshooting

### Enemy still sitting down?

**Check console for:**
```
EnemyIKReplay: Set minimum Y position = X.XX
```

**Should see:** `0.05` (not `0.1` or higher)

**If still wrong:**
1. Delete all enemies in scene
2. Exit Play Mode
3. Re-enter Play Mode (fresh spawn)

### Hitbox not working?

**Check:**
1. Enemy has CapsuleCollider? (should auto-add)
2. Collider center = (0, 1.0, 0)?
3. Collider height = 2.0?
4. Sword hitting collider? (visualize with gizmos)

**Verify in Inspector:**
- Select enemy
- Find CapsuleCollider component
- Check values match above

### Legs/feet clipping through floor?

**Check:**
1. Ground level at Y=0? (should be)
2. minYPosition = 0.05? (check console)
3. Recorded player Y position? (should be ~1.0)

**If clipping:**
- Increase `minYPosition` from 0.05 to 0.1
- Location: `EnemyIKReplay.cs` line ~60

---

## Summary

✅ **Fixed collider**: Now centered at Y=1.0, height=2.0 (proper standing)
✅ **Fixed Y position**: Uses recorded height (~1.0), not collider bottom (0.1)
✅ **Fixed hitbox**: Accurately covers standing character from feet to head
✅ **Fixed posture**: Character stands naturally, not sitting/crouching

### Expected Result:
- Character root at Y=1.0 (standing height)
- Collider from Y=0 to Y=2.0 (feet to head)
- Natural standing posture
- Accurate hit detection on full body
- No floor clipping

**Rigged enemies now stand properly and have accurate hitboxes!** 🎯
