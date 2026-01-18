# Hitbox & Sword Combat Fix ✅

## Changes Applied

### 1. Hitbox Y Position Adjusted ✅

**File**: `Assets/Scripts/EnemySpawner.cs` (Line 216)

**Changed:**
```csharp
// OLD
capsule.center = new Vector3(0f, 1.0f, 0f);

// NEW  
capsule.center = new Vector3(0f, -1.0f, 0f);
```

**Result:**
- Rigged enemy hitbox center moved from Y=1.0 to Y=-1.0
- With height=2.0, collider now extends from Y=-2.0 to Y=0.0
- This positions the collider lower on the character body

### 2. Enhanced Debug Logging ✅

**File**: `Assets/Scripts/CombatSystem.cs`

**Added extensive logging to track collision detection:**

```csharp
// OnCollisionEnter() now logs:
- Colliding object name, tag, layer
- Enemy name, tag, layer  
- Collision velocity

// HandleCollision() now logs:
- Whether object has Rigidbody
- Whether object is tagged as Player
- Whether object was already used
- Whether object is a melee weapon
- Whether melee weapons are enabled
- Sword velocity (kinematic vs non-kinematic)
```

**Purpose:** Identify exactly where sword collision detection fails

---

## Why Sword Might Not Be Working

### Potential Issues to Check:

1. **Physics Layer Collision Matrix**
   - Sword layer might be ignoring Enemy layer
   - Check: Edit → Project Settings → Physics → Layer Collision Matrix
   - Verify sword's layer can collide with enemy's layer

2. **Sword Collider Type**
   - Is the sword using a Collider or Trigger?
   - Triggers don't call OnCollisionEnter (only OnTriggerEnter)
   - Check: Sword GameObject → Collider component → "Is Trigger" should be ☐ (unchecked)

3. **Enemy Rigidbody Type**
   - If enemy Rigidbody is Kinematic, collisions might not register properly
   - Should be: isKinematic = false (which it is in EnemySpawner.cs line 200)

4. **Collision Detection Mode**
   - Sword moving too fast might miss collisions
   - Check: Sword Rigidbody → Collision Detection → Should be "Continuous" or "Continuous Dynamic"

5. **Sword Not Tagged Properly**
   - Verified: Sword IS tagged as "Sword" ✓
   - Found in scene file: `m_TagString: Sword`

---

## Testing with Debug Logs

### What to Look For:

**When swinging sword at enemy, console should show:**

```
[CombatSystem] Collision detected with [BuildingBlock] Sword, tag: Sword, layer: Default
[CombatSystem] This enemy: Enemy_1, tag: Enemy, layer: Default
[CombatSystem] Collision velocity: X.XX m/s
[CombatSystem] Is melee weapon? True, meleeWeaponsEnabled: True
[CombatSystem] Sword (kinematic) collision velocity: X.XX m/s
```

**If you see NO console messages:**
- Sword and enemy are NOT colliding physically
- Check collision matrix and layers

**If you see messages but no kill:**
- Velocity is too low (< 1.0 m/s)
- Check: `swordMinVelocity` setting

---

## Quick Diagnostic Checklist

### Test 1: Check Collision Happens

1. **Enter Play Mode**
2. **Spawn enemy** (complete Round 1)
3. **Swing sword at enemy**
4. **Check Console**

**Expected:** Collision messages appear

**If NO messages:**
- Physics collision not happening
- Check layers, colliders, rigidbodies

### Test 2: Check Sword Is Detected

**If collision messages appear, look for:**
```
[CombatSystem] Is melee weapon? True
```

**If shows "False":**
- Sword not tagged correctly
- IsMeleeWeapon() logic failing

### Test 3: Check Velocity

**Look for:**
```
[CombatSystem] Sword (kinematic) collision velocity: X.XX m/s
```

**If velocity < 1.0:**
- Swing faster
- Or lower `swordMinVelocity` in Inspector

---

## Manual Fix Options

### Option 1: Lower Sword Velocity Threshold

1. **Find enemy in Hierarchy** (during Play Mode)
2. **Select enemy**
3. **Find CombatSystem component**
4. **Change "Sword Min Velocity"** from 1.0 to 0.1
5. **Try hitting enemy** with slower swings

### Option 2: Verify Sword Collider

1. **Select sword** in Hierarchy
2. **Check components:**
   - BoxCollider (or other Collider) present? ✓
   - "Is Trigger" unchecked? ✓
   - Rigidbody present? ✓
   - Collision Detection = "Continuous"? ✓

### Option 3: Check Physics Layers

1. **Go to:** Edit → Project Settings → Physics
2. **Scroll to:** Layer Collision Matrix
3. **Find row for sword's layer** (probably "Default")
4. **Find column for enemy's layer** (probably "Default")
5. **Verify checkmark present** (layers can collide)

### Option 4: Force Debug Collision

**Add to sword GameObject:**
```csharp
void OnCollisionEnter(Collision collision)
{
    Debug.Log($"[SWORD] Hit {collision.gameObject.name} with velocity {collision.relativeVelocity.magnitude}");
}
```

This confirms sword is detecting collisions at all.

---

## Expected Behavior After Fix

### Hitbox (Y=-1.0):

```
Character root at Y=1.0 (hip height)
Collider center at Y=-1.0 (relative to root)
Collider extends from Y=-2.0 to Y=0.0 (world space when root at Y=1.0)

Result: Collider positioned lower, covering legs and lower torso
```

### Sword Combat:

```
Player swings sword
    ↓
Sword collides with enemy
    ↓
OnCollisionEnter() called
    ↓
CombatSystem.HandleCollision()
    ↓
IsMeleeWeapon() returns true
    ↓
Check velocity (kinematic = use collision velocity)
    ↓
If velocity >= 1.0 m/s
    ↓
InstakillEnemy() called
    ↓
Enemy dies!
```

---

## Common Issues & Solutions

### Issue: "No collision messages at all"

**Cause:** Physics not detecting collision

**Solutions:**
1. Check Layer Collision Matrix
2. Verify both objects have Colliders (not Triggers)
3. Verify both objects have Rigidbodies
4. Check Collision Detection mode (use Continuous)

### Issue: "Is melee weapon? False"

**Cause:** Sword not recognized as melee weapon

**Solutions:**
1. Check sword tag = "Sword" (case-sensitive)
2. Or rename sword to include "sword", "blade", "katana"
3. Or add SwordPhysicsFix component

### Issue: "Velocity too low"

**Cause:** Not swinging fast enough

**Solutions:**
1. Swing faster
2. Lower swordMinVelocity to 0.5 or 0.1
3. Check if sword is actually moving (not stuck)

### Issue: "Sword tagged as Player"

**Cause:** Parent/hierarchy issue

**Solutions:**
1. Check sword tag directly (not parent's tag)
2. Sword should be tagged "Sword", not "Player"

---

## Verification Steps

### Step 1: Compile & Save ✅
- Code compiled without errors ✓
- Scene saved ✓

### Step 2: Test in Play Mode

1. **Enter Play Mode**
2. **Pick up sword**
3. **Complete Round 1**
4. **Round 2 enemy spawns**
5. **Observe enemy position:**
   - Standing height (not sitting) ✓
   - Hitbox visible if you enable Gizmos
   - Hitbox centered lower on body (Y=-1.0)

6. **Swing sword at enemy**
7. **Watch Console for debug messages**

### Step 3: Analyze Results

**If working:**
```
✓ Collision detected
✓ Is melee weapon? True
✓ Velocity sufficient
✓ Enemy dies
```

**If not working:**
- Read console messages
- Follow troubleshooting steps above
- Report specific error/issue

---

## Technical Details

### Hitbox Configuration:

```csharp
// In EnemySpawner.cs
CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
capsule.center = new Vector3(0f, -1.0f, 0f);  // Y=-1.0 (user requested)
capsule.radius = 0.35f;
capsule.height = 2.0f;
capsule.direction = 1; // Y-axis

// When character root is at Y=1.0:
// Collider center world position = Y=0.0 (1.0 + -1.0)
// Collider bottom = Y=-1.0 (center - height/2)
// Collider top = Y=1.0 (center + height/2)
```

### Sword Detection Logic:

```csharp
// In CombatSystem.cs IsMeleeWeapon()
bool isMeleeWeapon = 
    obj.CompareTag("Sword") ||       // Tag = "Sword"
    obj.CompareTag("Melee") ||       // Tag = "Melee"  
    obj.CompareTag("Weapon") ||      // Tag = "Weapon"
    obj.name.ToLower().Contains("sword") ||
    obj.name.ToLower().Contains("blade") ||
    obj.name.ToLower().Contains("katana") ||
    obj.GetComponent<SwordPhysicsFix>() != null;
```

### Velocity Detection:

```csharp
if (rb.isKinematic)
{
    // Sword held (kinematic) - use collision relative velocity
    velocityMagnitude = velocity.magnitude;
}
else
{
    // Sword released (non-kinematic) - use rigidbody velocity
    velocityMagnitude = rb.linearVelocity.magnitude;
}

if (velocityMagnitude >= swordMinVelocity)
{
    InstakillEnemy(); // Default: swordMinVelocity = 1.0
}
```

---

## Summary

### Changes Made:
1. ✅ Hitbox Y position changed to -1.0
2. ✅ Enhanced debug logging added
3. ✅ Code compiled successfully
4. ✅ Scene saved

### Next Steps:
1. Test in Play Mode
2. Watch console for collision messages
3. Follow troubleshooting steps if needed
4. Report specific issue if still not working

### Most Likely Issues:
1. **Physics layer collision disabled** (check collision matrix)
2. **Sword collider is trigger** (should be solid collider)
3. **Velocity too low** (swing faster or lower threshold)

**The debug logs will tell us exactly what's happening!** 🔍
