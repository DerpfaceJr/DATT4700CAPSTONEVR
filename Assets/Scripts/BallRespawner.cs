using UnityEngine;

public class BallRespawner : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Distance from spawn position before respawning")]
    public float respawnDistance = 3f;
    
    [Tooltip("Time delay before respawning (seconds)")]
    public float respawnDelay = 0.5f;
    
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Rigidbody rb;
    private bool hasMovedAway = false;
    private float respawnTimer = 0f;
    
    void Start()
    {
        // Store initial position and rotation
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        // Check if ball has moved away from spawn position
        float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if (distanceFromSpawn > respawnDistance && !hasMovedAway)
        {
            hasMovedAway = true;
            respawnTimer = 0f;
        }
        
        // If ball has moved away, start respawn timer
        if (hasMovedAway)
        {
            respawnTimer += Time.deltaTime;
            
            if (respawnTimer >= respawnDelay)
            {
                RespawnBall();
            }
        }
    }
    
    void RespawnBall()
    {
        // Spawn a new ball at the original position
        GameObject newBall = Instantiate(gameObject, spawnPosition, spawnRotation);
        
        // Reset the new ball's state
        Rigidbody newRb = newBall.GetComponent<Rigidbody>();
        if (newRb != null)
        {
            newRb.linearVelocity = Vector3.zero;
            newRb.angularVelocity = Vector3.zero;
        }
        
        // Reset the respawner on the new ball
        BallRespawner newRespawner = newBall.GetComponent<BallRespawner>();
        if (newRespawner != null)
        {
            newRespawner.hasMovedAway = false;
            newRespawner.respawnTimer = 0f;
        }
        
        // Destroy the old ball
        Destroy(gameObject);
    }
}
