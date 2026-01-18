# Quick Test Guide - Hitbox & Sword Fix

## What Changed

1. **Hitbox Y Position**: Changed from 1.0 to **-1.0** ✅
2. **Debug Logging**: Added extensive collision detection logs ✅

---

## Test Now

### Step 1: Enter Play Mode
```
Unity → Play button
```

### Step 2: Complete Round 1
```
Defeat the static enemy
```

### Step 3: Swing Sword at Round 2 Enemy
```
Pick up sword
Swing at rigged enemy
Watch Console
```

---

## What to Look For in Console

### ✅ SUCCESS - Console shows:
```
[CombatSystem] Collision detected with [BuildingBlock] Sword, tag: Sword
[CombatSystem] This enemy: Player, tag: Enemy
[CombatSystem] Is melee weapon? True, meleeWeaponsEnabled: True
[CombatSystem] Sword (kinematic) collision velocity: X.XX m/s
→ Enemy should die if velocity >= 1.0
```

### ❌ PROBLEM 1 - No console messages at all:
**Cause:** Sword and enemy not colliding (physics issue)

**Fix:**
1. Edit → Project Settings → Physics
2. Check Layer Collision Matrix
3. Ensure sword's layer can collide with enemy's layer

### ❌ PROBLEM 2 - Shows "Is melee weapon? False":
**Cause:** Sword not being detected as weapon

**Fix:**
- Verify sword has tag "Sword" (case-sensitive)
- Check sword name contains "sword" or "katana"

### ❌ PROBLEM 3 - Shows velocity but no kill:
**Cause:** Velocity too low

**Fix:**
- Swing faster
- OR: Select enemy → CombatSystem → Change "Sword Min Velocity" to 0.1

---

## Quick Fixes

### Lower Velocity Threshold (During Play Mode):
```
1. Select enemy in Hierarchy
2. Find CombatSystem component
3. Set "Sword Min Velocity" = 0.1
4. Try hitting enemy
```

### Verify Sword Setup:
```
1. Select sword in Hierarchy
2. Check:
   - Tag = "Sword" ✓
   - Has Collider (not trigger) ✓
   - Has Rigidbody ✓
   - Collision Detection = Continuous ✓
```

---

## Hitbox Visual Reference

```
Enemy root (hips) at Y=1.0

OLD hitbox:
  center Y=1.0
  extends from Y=0.0 to Y=2.0
  [covers torso and head]

NEW hitbox:
  center Y=-1.0 (relative to root)
  world position when root at Y=1.0:
  extends from Y=-1.0 to Y=1.0
  [covers legs and lower torso]
```

---

## If Still Not Working

Report these details:
1. **Do console messages appear?** (Yes/No)
2. **What do the messages say?** (copy/paste)
3. **Is velocity shown?** (what value?)
4. **Does enemy have CombatSystem component?** (check Inspector)

The debug logs will tell us exactly what's wrong! 🔍
