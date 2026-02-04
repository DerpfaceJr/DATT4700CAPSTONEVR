# Game Mechanics and Systems — Full Reference

This document describes how every mechanic and system in the VR game works, based on the scripts in the project. It is a detailed reference, not a summary.

---

## 1. Round and Game Flow

### RoundManager

**Role:** Central game loop. Owns round number, spawns enemies, detects round completion and player death, drives reset.

**Start:**
- Finds or uses assigned: `EnemySpawner`, `PlayerRespawner`, `ObjectPositionResetter`, player GameObject (XR Origin or `[BuildingBlock] Camera Rig`), `YouLostPanel` (by name, including inactive). Subscribes to `PlayerHealth.onDeath` for HP-based game over.
- Starts round 1 via `StartRound(1)`.

**Update:**
- If player Y &lt; `deathYThreshold` (-10): calls `HandlePlayerDeath()`.
- If a round is active: calls `CheckRoundCompletion()`.

**StartRound(int roundNumber):**
- If game over: return. Sets `currentRound`, updates `roundNumText`, sets `isRoundActive = true`.
- Respawns player via `PlayerRespawner.RespawnPlayer()`.
- Resets throwables/weapons via `ObjectPositionResetter.ResetObjects()`.
- Removes nulls from `currentEnemies`.
- **Round 1:** Clears enemies via spawner, clears `currentEnemies` and `enemyRoundAssignments`, resets `enemiesSpawnedThisRound`.
- **Round 2+:** Syncs `currentEnemies` with `EnemySpawner.GetActiveEnemies()`, prunes dead entries from `enemyRoundAssignments`.
- **Enemy count:** Target count = `currentRound` (1 enemy in round 1, 2 in round 2, etc.). Builds set of “round slots” already filled by living enemies; for each missing slot 1..target:
  - Slot 1 (first): spawn position from `EnemySpawner.GetSpawnPositionForIndex(…)` (random in bounds or spawn points).
  - Slot 2+: spawn at `lastRoundEndPlayerPosition + offset`, clamped to map via `ClampSpawnToMapBounds` (uses WestWall, EastWall, NorthWall, SouthWall + padding).
- Spawns one enemy per missing slot with `EnemySpawner.SpawnSingleEnemyAt(…)`, subscribes to `EnemyController.OnEnemyDeath` → `OnEnemyDefeated`, adds to `currentEnemies` and `enemyRoundAssignments`.
- Fires `OnRoundStart(currentRound)`.

**CheckRoundCompletion:** Removes nulls from `currentEnemies`. If count is 0, calls `CompleteRound()`.

**CompleteRound():**
- Sets `isRoundActive = false`. Stores player position in `lastRoundEndPlayerPosition`. Resets objects again via `ObjectPositionResetter.ResetObjects()`. Fires `OnRoundComplete(currentRound)`.
- If a `RoundCompleteCutscene` exists: sets `waitingForCutscene = true` (cutscene will call `ContinueToNextRound()` when done).
- Else: immediately `StartRound(currentRound + 1)`.

**HandlePlayerDeath():** Sets game over, hides round activity, shows `youLostPanel`, fires `OnGameOver`, invokes `ResetGame()` after 2 seconds.

**ResetGame():** Clears game over, sets round to 1, hides You Lost panel, clears enemies via spawner and local lists, fires `OnGameReset`, then `StartRound(1)`.

**ContinueToNextRound():** Called by cutscene when finished; if `waitingForCutscene`, clears it and calls `StartRound(currentRound + 1)`.

**Map bounds:** `ClampSpawnToMapBounds` uses wall GameObjects (WestWall, EastWall, NorthWall, SouthWall) to get min/max X/Z and clamps spawn position with optional padding.

---

### RoundTextUpdater

**Role:** Keeps round UI in sync with RoundManager.

- Finds `RoundManager` and round TextMeshProUGUI (by name "Round Text" or on same GameObject). Subscribes to `OnRoundStart` and `OnGameReset`. On start and on those events, sets text to `string.Format(textFormat, roundNumber)` (default `"Round {0}"`). Unsubscribes in `OnDestroy`.

---

### RoundCompleteCutscene

**Role:** Short “round complete” state: disables movement and grab, shows UI, waits, then continues.

- Subscribes to `RoundManager.OnRoundComplete`. Finds `VRMovementController`, all `VRGrabThrow`, and RoundCompletePanel/Text (by name or in canvases).
- On round complete: runs coroutine `PlayCutscene(roundNumber)`:
  - Disables movement (`enableSmoothMovement = false`) and disables all `VRGrabThrow` (stores previous state).
  - Shows panel with `messageFormat` (e.g. "Round {0} Complete!").
  - Waits `cutsceneDuration` (default 3s).
  - Hides panel, re-enables movement and grab.
  - Calls `RoundManager.ContinueToNextRound()`.

---

### SetupRoundText

**Role:** One-time setup: finds "Round Text" GameObject and adds `RoundTextUpdater` if missing.

---

## 2. Player

### PlayerHealth

**Role:** HP, damage, death, optional respawn.

- `maxHealth` / `currentHealth` (default 1). `TakeDamage(float)` reduces health, clamps to 0, fires `onDamageTaken`, `onHealthChanged`; if health ≤ 0 calls `Die()`; else starts `invulnerabilityDuration` (no damage during that window). `Heal(float)` increases health up to max. `Die()` sets dead flag, fires `onDeath`; if `autoRespawn` then invokes `Respawn()` after `respawnDelay`. `Respawn()` clears dead, sets health to max, turns off invulnerability, calls `PlayerRespawner.RespawnPlayer()` if present, fires `onRespawn` and `onHealthChanged`. RoundManager subscribes to `onDeath` to trigger game over (You Lost panel + reset).

---

### PlayerRespawner

**Role:** Teleport player to a valid random spawn inside map bounds.

- Resolves XR Origin (or `[BuildingBlock] Camera Rig`). `CalculateSpawnBounds()` uses WestWall, EastWall, NorthWall, SouthWall (including inactive) to get min/max X/Z with `wallPadding`, and builds a horizontal bounds at `groundLevel`. `RespawnPlayer()` recalculates bounds, gets `GetRandomSpawnPosition()`, sets `xrOrigin.position` to it, and records the position for “avoid recent spawns” logic. `GetRandomSpawnPosition()`: up to `maxSpawnAttempts` tries a random X/Z in bounds at `groundLevel + playerHeightOffset`; position must pass `IsValidSpawnPosition` (raycast down for ground, no overlap with Wall colliders in 0.5f radius) and `IsFarEnoughFromPreviousSpawns` (2D distance ≥ `minDistanceFromPreviousSpawns`). If none found, retries without distance check; final fallback is center. `RecordSpawnLocation` keeps a rolling list (capped at `maxTrackedSpawnLocations`).

---

### PlayerLoseOnEnemyTouch

**Role:** Deal damage when the player touches an enemy (or enemy touches player).

- Needs `PlayerHealth` (on same object, parent, or FindObjectOfType). `EnsureBodyTrigger()`: if no trigger capsule exists, adds a CapsuleCollider (trigger) matching CharacterController size + `triggerPadding` so that when the enemy walks into the player, trigger fires. Damage is applied via:
  - `OnCollisionEnter`: if collider is enemy (tag or parent tag "Enemy"), `PlayerHealth.TakeDamage(touchDamage)` (default 1 = one-shot with 1 HP).
  - `OnTriggerEnter`: same check.
  - `OnControllerColliderHit`: when CharacterController hits something; if it’s an enemy, same damage. `IsEnemy()` walks parent hierarchy for tag "Enemy".

---

### PlayerBallCollisionIgnore

**Role:** Prevent thrown balls/blocks from colliding with the player body.

- Finds CharacterController (on self or via VRMovementController) and camera rig (XR Origin / Camera Rig). If `ignoreBallCollisions`: periodically (`searchInterval`) and at init, `FindAndIgnoreBalls()` finds objects by: name containing "PhysicsBall"; name containing "[BuildingBlock]" with non-kinematic Rigidbody; or `BuildingBlockProjectile` component. For each, non-trigger colliders are added to an ignore list and `Physics.IgnoreCollision(characterController, ballCollider, true)` is called. Can also `AddBallToIgnore` / `RemoveBallFromIgnore` / `ClearIgnoredCollisions`. `autoFindNewBalls` re-runs the search so newly spawned balls get ignored too.

---

## 3. VR Movement

### VRMovementController

**Role:** Smooth locomotion and snap turn from Quest controllers.

- Requires CharacterController. Uses `xrOrigin` (XR Origin or Camera Rig). Left controller: `primary2DAxis` → `leftThumbstick`. Right controller: `primary2DAxis` for snap turn when `enableSnapTurn`; threshold 0.7, cooldown 0.3s. Movement: head/camera forward and right projected to XZ, normalized; `moveDirection = (forward * leftThumbstick.y + right * leftThumbstick.x) * moveSpeed`; `CharacterController.Move(moveDirection * dt)`. Gravity: if grounded, velocity.y = -2; else velocity.y += gravity * dt; Move(velocity * dt). **Snap turn:** `SnapTurn(angle)` is called with ±45° but currently does `xrOrigin.Rotate(0, 0, 0)` (no rotation) — effectively snap turn is disabled.

---

## 4. Grab and Throw (VR)

### VRGrabThrow

**Role:** Grab and throw Rigidbody objects with controller grip/trigger. One instance per hand (LeftHandAnchor / RightHandAnchor).

- **Input:** Grip (analog or button) above `gripThreshold`, or trigger, or back button → “grab”. Release when all released. Hand is inferred from GameObject name (left/right).
- **Grab detection:** Three methods. (1) OverlapSphere at grab point (child at local zero), radius = max(grabDistance, 2). (2) If nothing: RaycastAll forward from grab point. (3) If still nothing: SphereCastAll forward. For each hit/overlap, gets Rigidbody (attachedRigidbody, GetComponent, GetComponentInParent; or from Oculus Grabbable via reflection). Skips: controller-related names (ControllerGrabLocation, PlayerController, HandAnchor, Camera Rig, etc.), objects with Oculus `Grabbable` (so Oculus hand interaction handles those). Picks closest valid Rigidbody and calls `GrabObject(rb)`.
- **GrabObject:** Optionally disables Oculus HandGrabInteractable on the object. Sets `grabbedObject`, `isGrabbing`, makes RB kinematic, parents to grab point, zero local pos/rot, zeros linear/angular velocity, resets velocity history.
- **Update (held):** `UpdateGrabbedObject()` sets grabbed transform position/rotation to grab point every frame.
- **Velocity for throw:** Frame velocity = (current position - previous position) / dt; dead zone filters small movement; stored in ring buffer. Smoothed velocity = weighted average (recent frames heavier) and optional extra smoothing. `currentVelocity` used at release.
- **Release:** Restores HandGrabInteractable if it was disabled. Unparents, sets kinematic false. If `forwardThrowEnabled`: project `currentVelocity` onto controller forward; if backward, use magnitude with `minForwardSpeed`; clamp forward to ≥ `minForwardSpeed`; filter sideways by `sideMovementFilter`; final velocity = forward component + filtered side; safety clamp so throw is never backward. Multiply by `throwForce` and set on RB (linearVelocity). Adds angular velocity (cross product or random spin). If object has `BuildingBlockProjectile`, calls `MarkThrownByPlayer()` so that only “player throw” can be used for damage (CombatSystem does not currently check this; see Combat).

---

### SetupVRGrabThrow

**Role:** Adds `VRGrabThrow` to LeftHandAnchor and RightHandAnchor if missing, with chosen defaults (grab distance, throw force, grip threshold, debug).

---

## 5. Combat

### CombatSystem

**Role:** Apply damage to the enemy this component is on — from melee (sword) and from projectiles. Attached to **enemies**, not the player.

- **Melee (trigger):** `OnTriggerEnter(other)`. If `!meleeWeaponsEnabled` return. Gets `other.attachedRigidbody`; if in `recentWeapons` (cooldown) return. Requires `SwordPhysicsFix` on that RB and `IsBladeCollider(other)`. Then gets own `EnemyController` and calls `TakeDamage(maxHealth * 100)` (instant kill). Adds weapon to `recentWeapons` and clears it after `meleeCooldown`.
- **Projectiles (collision):** `OnCollisionEnter(collision)`. Ignores if rigidbody is tagged "Sword". Requires `relativeVelocity.magnitude` ≥ threshold: if `instakillEnabled` then `instakillMinVelocity` (1.5), else `minDamageVelocity` (2). Then same enemy `TakeDamage`: if instakill, large damage; else `velocity * mass * damageMultiplier`. **Note:** It does not check `BuildingBlockProjectile.WasThrownByPlayer()`, so any high-velocity impact (e.g. rolling object) can damage the enemy.

---

### SwordPhysicsFix

**Role:** When sword is held (Oculus Grabbable), make it kinematic and set blade colliders to trigger so they don’t push physics; when released, restore dynamic and non-trigger.

- Uses Oculus `Grabbable`; `Points.Count > 0` = held. When held: RB kinematic, `SetBladeTriggers(true)` on `bladeColliders`. When released: RB non-kinematic, gravity on, `SetBladeTriggers(false)`, zero linear/angular velocity, `Physics.SyncTransforms()`. `IsBladeCollider(col)` returns whether the collider is in the configured blade list (so only blade triggers register as melee in CombatSystem).

---

## 6. Enemies

### EnemyController

**Role:** Enemy HP, damage, death.

- `currentHealth` / `maxHealth`. `TakeDamage(float)` subtracts, clamps to 0, fires `OnEnemyDamaged`, optional damage flash (swap material for `damageFlashDuration`), and if health ≤ 0 calls `Die()`. `Die()` fires `OnEnemyDeath(this)` and `Destroy(gameObject)`. RoundManager subscribes to OnEnemyDeath to remove from list and check round completion.

---

### EnemySeekAndAttack

**Role:** Move toward player (NavMesh or direct) and shoot projectiles in range.

- **Movement:** If `useNavMesh` and NavMeshAgent present and on mesh: periodically sets destination to player, agent moves. Otherwise: direct movement — desired velocity = flat direction to player * moveSpeed; if kinematic RB uses `MovePosition`, else sets linearVelocity x/z; rotation = LookRotation(direction). Position clamped to map bounds (WestWall/EastWall/NorthWall/SouthWall + padding). Rigidbody may be made kinematic when NavMesh is used so the agent drives position.
- **Ranged:** When flat distance to player ≤ `attackRange` and cooldown elapsed: `TryRangedAttack`. Instantiates `projectilePrefab` (or EnemySpawner’s enemyBallPrefab), tags "EnemyProjectile", sets layer if name exists. Applies velocity (direction * projectileSpeed, no Y). Adds `BuildingBlockProjectile` and `EnemyProjectileDamage` if missing. `DisablePlayerGrabbing` destroys components with “grab” or “interactable” in the type name (except those two) so the player can’t grab enemy projectiles.

---

### EnemySpawner

**Role:** Create and configure enemy instances for RoundManager.

- **SpawnSingleEnemyAt(index, position, isFirstRound):** Creates enemy at position, adds to `activeEnemies`, returns it. **CreateEnemyAt:** Round 1 first slot uses `enemyPrefab` (static/simple); otherwise uses `riggedEnemyPrefab` if set else `enemyPrefab`. If no prefab, `CreateEnemyProcedurally()` (cube + red material). Then `EnsureEnemyComponents(enemy, index, isFirstRound)`.
- **EnsureEnemyComponents:** If `IKTargetFollowVRRig` present (rigged): RB kinematic, no gravity. Else: RB dynamic, gravity, mass 70, damping, freeze X/Z rotation. Ensures root has a non-trigger body collider (Capsule) so PlayerLoseOnEnemyTouch can fire when enemy walks into player; size/center differ for rigged vs simple. Adds/ensures: EnemyController (e.g. maxHealth 100), CombatSystem (instakill on, instakillMinVelocity 1.5), EnemySeekAndAttack (speed, range, cooldown, projectile prefab). For rigged: adds/ensures LegBonePositionFixer.
- **GetSpawnPositionForIndex / GetSpawnPosition:** If spawn points set and not random, uses spawn points; else random X/Z from wall bounds (using PlayerRespawner padding/ground if available), Y from raycast down or groundLevel; rigged gets small height offset.
- **ClearEnemies:** Destroys all in `activeEnemies` and clears list. **GetActiveEnemies:** Returns current list (nulls removed).

---

### EnemyProjectileDamage

**Role:** When an enemy projectile (ball) hits the player, apply damage.

- OnCollisionEnter: if `IsPlayer(collision.gameObject)` and velocity ≥ minDamageVelocity, `DealDamageToPlayer`. Player detection: tag "Player", name containing "XR Origin" or "Camera Rig", or parent with those. Damage: base `damageAmount`; if velocity ≥ instakillVelocity then 9999; else optionally scaled by velocity. Finds `PlayerHealth` on hit object, parents, children, or FindObjectOfType and calls `TakeDamage`. Single hit per projectile (`hasHit`).

---

### StaticEnemy

**Role:** Optional first-round enemy that doesn’t move: holds position, rotates.

- Locks position to start. Rotation: Continuous (Rotate Y) or Random (pick random horizontal direction every `lookDuration`, Slerp toward it). Not used by default spawn flow unless the round-1 prefab has this component.

---

### EnemySystemSetup

**Role:** Editor/one-shot: create and link VRMovementController (with CharacterController), RoundManager, EnemySpawner, PlayerRespawner; wire RoundManager references and VRMovementController xrOrigin.

---

### EnemyPrefabSetupHelper / SetupRiggedEnemyPrefab

**Role:** Verify rigged enemy prefab (IKTargetFollowVRRig, EnemyController, CombatSystem, Animator, IK targets). SetupRiggedEnemyPrefab logs manual steps for prefab setup (replace cube with rigged model, add IK, etc.).

---

## 7. Projectiles and Throwables

### BuildingBlockProjectile

**Role:** Mark objects as projectiles for combat and physics; record “thrown by player” for release.

- On Start: ensures RB non-kinematic, continuous collision, collider non-trigger (or adds simple collider); registers with PlayerBallCollisionIgnore. Tracks when velocity &gt; minThrowVelocity as “thrown”; resets after THROW_TIMEOUT. `MarkThrownByPlayer()` sets timestamp; `WasThrownByPlayer()` true for THROW_VALID_SECONDS after that. CombatSystem does not currently use WasThrownByPlayer — damage is purely velocity-based.

---

### SetupBuildingBlockProjectiles

**Role:** Find objects by name "[BuildingBlock]" or by Meta BuildingBlock component and add BuildingBlockProjectile if missing.

---

### BallRespawner

**Role:** Respawn a ball when it leaves its spawn area.

- Stores initial position/rotation. If distance from spawn &gt; respawnDistance, sets hasMovedAway; after respawnDelay, instantiates copy at spawn, zeroes velocities and resets respawner state on copy, destroys self.

---

### ObjectPositionResetter

**Role:** Return throwables and weapons to initial positions between rounds.

- Tracks objects by: tags (Throwable, Sword, Weapon, Melee), BuildingBlockProjectile, ConfigureBallLikeCube, name containing "ball" + Rigidbody, SwordPhysicsFix. Stores position, rotation, scale, wasKinematic per object. Subscribes to RoundManager.OnRoundComplete; if resetOnRoundEnd, calls ResetObjects. RoundManager also calls ResetObjects at round start. ResetObjects (optionally re-finds objects): sets transform and RB state from stored state, zeros linear/angular velocity, Physics.SyncTransforms.

---

### ApplyBouncyPhysics

**Role:** Apply a PhysicsMaterial to a SphereCollider (bounciness, friction, combine mode). Used for balls.

---

### ConfigureBallLikeCube

**Role:** Make PhysicsBalls work like the Building Block cube: RB settings, bouncy material, Oculus Grabbable (reflection for kinematic while selected, throw when unselected). Tracks grab state and on release applies throw velocity from position delta. Registers with PlayerBallCollisionIgnore.

---

### SetupBallLikeCube

**Role:** Find objects with "PhysicsBall" in name and add ConfigureBallLikeCube if missing.

---

## 8. Map and UI

### MiniMap

**Role:** Show NavMesh as a mesh.

- Uses NavMesh.CalculateTriangulation() and assigns vertices/indices to a MeshFilter’s mesh (e.g. in Awake or via context menu "Bake Mesh").

---

### ResizeMap

**Role:** Scale floor and walls at runtime.

- `ResizeMapGeometry()`: scales Floor by mapScaleMultiplier (X and Z); repositions and scales NorthWall, SouthWall, EastWall, WestWall by the same multiplier (position and scale derived from wall names/directions).

---

### TextControl

**Role:** Show short “Hit” / “Miss” feedback.

- OnCollisionEnter: if other is Enemy and this is not Sword tag → ShowText("Hit", green). OnTriggerEnter: if other is Enemy and this is Sword → ShowText("Hit", green). ShowText sets resultText text/color, activates it, and hides after displayTime via coroutine.

---

## 9. Scene and Level

### MazeSceneSetup

**Role:** Placeholder; context menu logs that maze setup should be done via Editor/MCP.

---

### MazeBuilder (Editor)

**Role:** Editor window menu "Tools/Build Maze"; logs that maze building is via MCP.

---

## 10. VR Body and Rigged Characters

### IKTargetFollowVRRig

**Role:** Map VR head and hand transforms to IK targets for a rigged body.

- VRMap: vrTarget → ikTarget with position/rotation offsets. If head/left/right vrTarget are null (e.g. enemy), LateUpdate returns without applying VR. Otherwise: body position = head IK position + offset; body rotation lerped from current to head yaw; then head, leftHand, rightHand Map() so IK targets follow VR.

---

### IKFootSolver

**Role:** Procedural foot stepping on terrain.

- Raycast down from body + foot spacing to get ground point. When step distance exceeded and other foot not moving and lerp finished, sets new position/normal (step length or side step). Lerps old → new with arc (stepHeight * sin(lerp * PI)). Used for character feet so they stick to uneven ground.

---

### LegBonePositionFixer

**Role:** Fix rigged characters whose upper leg bones are collapsed on the spine.

- Finds LeftUpLeg, RightUpLeg, Hips (Animator humanoid or by name). If legs are very close to hips (&lt; 0.05), repositions them with hipWidth and legForwardOffset. Can run on Start (autoFixOnStart) or via context menu.

---

## 11. Utilities and Other

### SuppressFMODErrors

**Role:** Document that FMOD init errors can be ignored; logs a one-time message. Does not actually suppress engine FMOD logs.

---

## Data and Event Flow (Concise)

- **Round start:** RoundManager.StartRound → PlayerRespawner, ObjectPositionResetter, EnemySpawner (spawn N enemies, N = round). RoundManager subscribes to EnemyController.OnEnemyDeath.
- **Round end:** All enemies dead → RoundManager.CompleteRound → ObjectPositionResetter, OnRoundComplete → RoundCompleteCutscene (disable controls, UI, wait) → RoundManager.ContinueToNextRound → StartRound(next).
- **Player death:** PlayerHealth.Die (from touch or projectile) or Y &lt; threshold → RoundManager.HandlePlayerDeath → You Lost panel → ResetGame after 2s → StartRound(1).
- **Damage to enemy:** CombatSystem on enemy; melee via sword trigger + SwordPhysicsFix blade; projectile via OnCollisionEnter and velocity threshold (instakill or scaled).
- **Damage to player:** PlayerLoseOnEnemyTouch (touch); EnemyProjectileDamage (enemy ball collision).
- **Throw:** VRGrabThrow release applies velocity to RB and calls BuildingBlockProjectile.MarkThrownByPlayer (CombatSystem does not currently gate on this).

---

*End of mechanics and systems reference.*
