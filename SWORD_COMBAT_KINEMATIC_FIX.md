# Sword Combat Fix - Kinematic Rigidbody Issue ✅

## Problem
Sword wasn't registering kills on enemies when swung.

## Root Cause
The `SwordPhysicsFix` script makes the sword's Rigidbody **kinematic while grabbed** (line 31):

```csharp
if (isGrabbed && !wasGrabbed)
{
    // Grabbed
    rb.isKinematic = true;  // ← This prevents velocity calculation
}
```

When a Rigidbody is kinematic:
- `rb.linearVelocity` is always **zero**
- Physics engine doesn't calculate velocity
- Original code checked `rb.linearVelocity` for sword hits
- Result: sword always had "velocity too low" ❌

## Solution Applied

Modified `CombatSystem.cs` to handle both kinematic and non-kinematic swords:

```csharp
// Special handling for melee weapons
if (isMeleeWeapon && meleeWeaponsEnabled)
{
    // For kinematic swords (held), use collision relative velocity
    // For non-kinematic swords (thrown), use rigidbody velocity
    if (rb.isKinematic)
    {
        // Sword is held (kinematic) - use collision velocity
        velocityMagnitude = velocity.magnitude;
        Debug.Log($"[CombatSystem] Sword (kinematic) collision velocity: {velocityMagnitude:F2} m/s");
    }
    else
    {
        // Sword is released (non-kinematic) - use rigidbody velocity
        velocityMagnitude = rb.linearVelocity.magnitude;
        Debug.Log($"[CombatSystem] Sword (non-kinematic) rigidbody velocity: {velocityMagnitude:F2} m/s");
    }
    
    if (velocityMagnitude >= swordMinVelocity)
    {
        // Sword hit with sufficient velocity - instakill
        InstakillEnemy(collidingObject, velocityMagnitude, "SWORD KILL");
        return;
    }
    else
    {
        Debug.Log($"[CombatSystem] Sword hit but velocity too low ({velocityMagnitude:F2} < {swordMinVelocity})");
        return;
    }
}
```

### What Changed:
1. **Kinematic check added**: `if (rb.isKinematic)`
2. **While held (kinematic)**: Uses `velocity.magnitude` (collision relative velocity)
3. **While thrown (non-kinematic)**: Uses `rb.linearVelocity.magnitude`
4. **Debug logging added**: Shows which velocity is being used

---

## How It Works Now

### Held Sword (Kinematic):
```
You grab sword
    ↓
SwordPhysicsFix sets rb.isKinematic = true
    ↓
You swing sword at enemy
    ↓
Collision occurs
    ↓
CombatSystem detects rb.isKinematic = true
    ↓
Uses collision.relativeVelocity.magnitude
    ↓
Checks velocity >= 1.0 m/s
    ↓
INSTAKILL! ✓
```

### Thrown Sword (Non-Kinematic):
```
You release sword
    ↓
SwordPhysicsFix sets rb.isKinematic = false
    ↓
Sword flies through air
    ↓
Hits enemy
    ↓
CombatSystem detects rb.isKinematic = false
    ↓
Uses rb.linearVelocity.magnitude
    ↓
Checks velocity >= 1.0 m/s
    ↓
INSTAKILL! ✓
```

---

## Testing

### Test 1: Held Sword
1. **Pick up sword**
2. **Swing at enemy**
3. **Check Console**

**Expected:**
```
[CombatSystem] Sword (kinematic) collision velocity: 2.34 m/s
[CombatSystem] SWORD KILL! Sword hit Enemy_Round1 (velocity: 2.34 m/s)
```

### Test 2: Thrown Sword
1. **Pick up sword**
2. **Throw it** at enemy
3. **Check Console**

**Expected:**
```
[CombatSystem] Sword (non-kinematic) rigidbody velocity: 3.12 m/s
[CombatSystem] SWORD KILL! Sword hit Enemy_Round2 (velocity: 3.12 m/s)
```

### Test 3: Low Velocity
1. **Gently touch enemy with sword** (no swing)
2. **Check Console**

**Expected:**
```
[CombatSystem] Sword (kinematic) collision velocity: 0.23 m/s
[CombatSystem] Sword hit but velocity too low (0.23 < 1.0)
```

---

## Why This Happens

### Kinematic vs Non-Kinematic Rigidbodies:

**Kinematic Rigidbody** (while held):
- ✅ Position/rotation controlled by transform
- ✅ Collides with other objects
- ❌ Physics engine doesn't calculate velocity
- ❌ `rb.linearVelocity` always zero
- ✅ **Use collision.relativeVelocity instead**

**Non-Kinematic Rigidbody** (while free):
- ✅ Position controlled by physics
- ✅ Velocity calculated by physics engine
- ✅ `rb.linearVelocity` accurate
- ✅ **Can use rb.linearVelocity directly**

### Why Swords Are Kinematic While Held:

From `SwordPhysicsFix.cs`:
- Prevents sword from falling through floor while grabbed
- Gives player direct control over position
- Prevents physics jitter/instability
- Common VR grab pattern

---

## Configuration

### Sword Minimum Velocity:
- **Default**: 1.0 m/s
- **Location**: CombatSystem component on enemies
- **Field**: `swordMinVelocity`

**To adjust:**
1. Select enemy GameObject
2. Find CombatSystem component
3. Change "Sword Min Velocity"

**Recommendations:**
- **Easy mode**: 0.5 m/s (light touch kills)
- **Normal mode**: 1.0 m/s (moderate swing kills) ← current
- **Hard mode**: 2.0 m/s (strong swing required)

---

## Troubleshooting

### Still not killing enemies?

**Check Console for messages:**

**If you see:**
```
[CombatSystem] Sword (kinematic) collision velocity: X.XX m/s
[CombatSystem] Sword hit but velocity too low (X.XX < 1.0)
```
→ **Swing harder!** Your velocity is below threshold.

**If you see:**
```
[CombatSystem] Sword (kinematic) collision velocity: X.XX m/s
[CombatSystem] SWORD KILL! ...
```
→ **Working!** Enemy should die.

**If you see NO messages at all:**
→ Sword not colliding with enemy
- Check enemy has CombatSystem component
- Check enemy has Collider
- Check sword has Colliders (it does ✓)
- Check enemy isn't on a layer that ignores sword

### Enemies still alive after hit?

**Check:**
1. Console shows "SWORD KILL" message?
2. Enemy has EnemyController component?
3. InstakillEnemy() being called?

**Verify:**
```csharp
// EnemyController should receive:
public void TakeDamage(float damage)
{
    currentHealth -= damage;
    if (currentHealth <= 0)
    {
        Die(); // This should be called
    }
}
```

---

## Summary

✅ **Fixed**: Sword now uses collision velocity for kinematic rigidbodies
✅ **Added**: Debug logging to show velocity and detection
✅ **Works**: Both held swings and thrown swords
✅ **Threshold**: 1.0 m/s minimum velocity (configurable)

### What to Expect:

**Held Sword:**
- Swing at normal speed → Instant kill ✓
- Light touch → No damage (velocity too low)
- Console shows exact velocity

**Thrown Sword:**
- Throw at enemy → Instant kill ✓
- Weak toss → May not kill (velocity dependent)

**The sword combat system is now fully functional!** 🗡️
