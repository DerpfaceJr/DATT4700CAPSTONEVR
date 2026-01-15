# Enemy Replay System - Proper Round Data Assignment

## Problem
In Round 3 and later, the enemy replay system wasn't properly assigning movement data from earlier rounds. When enemies died and respawned, or when tracking was out of sync, enemies would get incorrect data assignments.

### Original Issues
1. **Index-Based Assignment**: Used `currentEnemies.Count` for enemy index, which would break when enemies died
2. **Single Round Data**: Only retrieved "previous round" data, didn't handle multiple different round assignments
3. **No Tracking**: Didn't track which round data each enemy was assigned to
4. **Lost Assignments**: When enemies died, there was no way to respawn them with the correct data

## Solution: Slot-Based Round Assignment

Implemented a **slot-based system** where each enemy occupies a specific "round slot" that determines which movement data they replay.

### Key Changes

#### 1. Added Enemy Round Tracking
```csharp
private Dictionary<GameObject, int> enemyRoundAssignments = new Dictionary<GameObject, int>();
```

Tracks which round slot each enemy represents.

#### 2. Slot-Based Spawning Logic

Instead of spawning based on count, the system now:
1. Determines which slots (1 to N) need to be filled
2. Checks which slots already have living enemies
3. Spawns enemies for missing slots with the correct data

```csharp
// Slot 1 = Round 1 (static)
// Slot 2 = Replays Round 1 data
// Slot 3 = Replays Round 2 data
// Slot N = Replays Round N-1 data
```

#### 3. Proper Data Assignment Per Slot
```csharp
for (int roundSlot = 1; roundSlot <= targetEnemyCount; roundSlot++)
{
    if (existingRoundSlots.Contains(roundSlot))
        continue; // Slot already filled
    
    // Assign correct movement data for this slot
    List<MovementFrame> movementDataForSlot = null;
    bool shouldBeStatic = (roundSlot == 1);
    
    if (!shouldBeStatic && roundMovementData.ContainsKey(roundSlot - 1))
    {
        movementDataForSlot = roundMovementData[roundSlot - 1];
    }
    
    // Spawn and track
    GameObject newEnemy = enemySpawner.SpawnSingleEnemy(...);
    enemyRoundAssignments[newEnemy] = roundSlot;
}
```

#### 4. Cleanup on Enemy Death
When an enemy dies, it's removed from the tracking dictionary, allowing that slot to be refilled in the next round.

## How It Works Now

### Round 1
- **Slot 1**: Static enemy (no replay)
- **Tracking**: `{Enemy1 → Slot 1}`

### Round 2
- **Existing**: Enemy1 still in Slot 1
- **Slot 2**: NEW enemy replaying Round 1 data
- **Tracking**: `{Enemy1 → Slot 1, Enemy2 → Slot 2}`

### Round 3
- **Existing**: Enemy1 (Slot 1), Enemy2 (Slot 2)
- **Slot 3**: NEW enemy replaying Round 2 data
- **Tracking**: `{Enemy1 → Slot 1, Enemy2 → Slot 2, Enemy3 → Slot 3}`

### Round 3 (If Enemy2 Died)
- **Existing**: Enemy1 (Slot 1)
- **Slot 2**: Respawn enemy replaying Round 1 data (correct!)
- **Slot 3**: NEW enemy replaying Round 2 data
- **Tracking**: `{Enemy1 → Slot 1, Enemy2_new → Slot 2, Enemy3 → Slot 3}`

### Round 10
- **Slots 1-10**: Each slot has the correct data
  - Slot 1: Static
  - Slot 2: Round 1 data
  - Slot 3: Round 2 data
  - ...
  - Slot 10: Round 9 data

## Benefits

### 1. Consistent Data Assignment
Each enemy always gets the correct movement data based on their slot, regardless of when they spawn or respawn.

### 2. Handles Enemy Deaths
If an enemy dies, their slot can be refilled in the next round with the same correct data.

### 3. Preserves Earlier Rounds
Round 10 enemies will correctly replay data from Rounds 1-9, not just Round 9.

### 4. Self-Healing
The sync logic at the start of each round cleans up any inconsistencies and refills missing slots.

## Debug Information

The system now logs detailed information:
- Which slots exist vs. which need to be filled
- What data each new enemy receives
- When enemies are defeated and which slot they occupied

Example debug output:
```
Round 3: Existing enemy slots: [1, 2], Target slots: 1-3
Spawned enemy for slot 3 with Round 2 data (458 frames). Total enemies: 3
Round 3: Finished spawning. Total enemies: 3 (target: 3)
```

## Testing Checklist

### Normal Progression (No Deaths)
- ✅ Round 1: 1 static enemy
- ✅ Round 2: 2 enemies (1 static, 1 replaying R1)
- ✅ Round 3: 3 enemies (1 static, 1 replaying R1, 1 replaying R2)
- ✅ Round N: N enemies with correct data

### With Enemy Deaths
- ✅ Kill static enemy in Round 1, verify it respawns in Round 2
- ✅ Kill Round 1 replay enemy in Round 2, verify correct respawn in Round 3
- ✅ Kill multiple enemies, verify all slots refill correctly

### Edge Cases
- ✅ All enemies die in a round → All respawn next round
- ✅ Enemy dies and immediately respawns → Gets correct slot data
- ✅ Late game (Round 10+) → Each enemy has distinct data from their respective round

## Files Modified
- `Assets/Scripts/RoundManager.cs`
  - Added `enemyRoundAssignments` dictionary
  - Rewrote spawning logic to use slot-based assignment
  - Updated cleanup logic in `OnEnemyDefeated()`
  - Added tracking cleanup in Round 1 start

## Implementation Details

### Data Structures
```csharp
// Which round data each enemy uses
Dictionary<GameObject, int> enemyRoundAssignments

// Movement data per round
Dictionary<int, List<MovementFrame>> roundMovementData

// Currently alive enemies
List<GameObject> currentEnemies
```

### Assignment Formula
```
Enemy Slot N:
  - If N == 1: Static (no replay)
  - If N > 1: Replays roundMovementData[N-1]
```

### Synchronization
At the start of each round (Round 2+):
1. Sync `currentEnemies` with actual scene enemies
2. Remove dead enemies from `enemyRoundAssignments`
3. Identify missing slots
4. Spawn enemies for missing slots with correct data
5. Track new assignments
