# Enemy Persistence Fix

## Problem
The enemy system was clearing all enemies each round and spawning N new enemies, but the user wanted:
- Round 1: 1 enemy (static)
- Round 2: Keep round 1 enemy + spawn 1 new enemy (replays round 1 movement) = 2 total
- Round 3: Keep round 1 & 2 enemies + spawn 1 new enemy (replays round 2 movement) = 3 total
- Round 4: Keep all previous + spawn 1 new = 4 total
- etc.

## Solution

### RoundManager.cs Changes
1. **Changed storage**: Replaced `previousRoundMovements` with `Dictionary<int, List<MovementFrame>> roundMovementData` to store movement data for ALL rounds
2. **Keep enemies alive**: Only clear enemies on round 1, otherwise keep existing enemies
3. **Spawn 1 enemy per round**: Changed from spawning N enemies to spawning just 1 new enemy per round
4. **Movement data lookup**: Each new enemy gets movement data from the previous round (round N-1)

### EnemySpawner.cs Changes
1. **New method**: Added `SpawnSingleEnemy()` method to spawn just one enemy
2. **Keep legacy method**: Kept `SpawnEnemies()` for backwards compatibility
3. **Enemy naming**: Changed to `Enemy_Round{N}` to reflect which round the enemy is from
4. **Better tracking**: Improved `GetActiveEnemies()` to remove null enemies

## How It Works Now

### Round 1
- Clears all enemies
- Spawns 1 static enemy
- Records player movements

### Round 2
- Keeps round 1 enemy alive
- Spawns 1 new enemy with round 1 movement data
- Records player movements

### Round 3
- Keeps round 1 & 2 enemies alive
- Spawns 1 new enemy with round 2 movement data
- Records player movements

### Round N
- Keeps all previous enemies alive
- Spawns 1 new enemy with round (N-1) movement data
- Records player movements

## Testing

1. **Round 1**: Should see 1 static enemy
2. **Round 2**: Should see 2 enemies total (1 from round 1, 1 new replaying round 1)
3. **Round 3**: Should see 3 enemies total (1 from round 1, 1 from round 2, 1 new replaying round 2)
4. **Round 5**: Should see 5 enemies total

## Debug Logs

The system now logs:
- "Round N: Current enemies: X" - shows how many enemies exist before spawning
- "Spawned new enemy for round N. Total enemies: X" - confirms new enemy was added
- Movement data is stored per round and logged when recorded








