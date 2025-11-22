using UnityEngine;

/// <summary>
/// First enemy behavior - stands still and rotates to look around
/// Used only for the first enemy in round 1
/// </summary>
public class StaticEnemy : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 30f;
    
    [Tooltip("Rotation pattern")]
    public RotationPattern pattern = RotationPattern.Continuous;
    
    [Tooltip("Time to look in each direction (for Random pattern)")]
    public float lookDuration = 2f;
    
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float rotationTimer = 0f;
    private Vector3 targetDirection;
    
    public enum RotationPattern
    {
        Continuous,  // Continuous 360 degree rotation
        Random       // Random directions with pauses
    }
    
    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        if (pattern == RotationPattern.Random)
        {
            ChooseRandomDirection();
        }
    }
    
    void Update()
    {
        // Keep position locked
        transform.position = startPosition;
        
        // Handle rotation based on pattern
        if (pattern == RotationPattern.Continuous)
        {
            // Continuous rotation
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        else if (pattern == RotationPattern.Random)
        {
            // Random direction rotation
            rotationTimer += Time.deltaTime;
            
            if (rotationTimer >= lookDuration)
            {
                ChooseRandomDirection();
                rotationTimer = 0f;
            }
            
            // Smoothly rotate towards target direction
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime / 90f
            );
        }
    }
    
    private void ChooseRandomDirection()
    {
        // Choose a random horizontal direction
        float randomAngle = Random.Range(0f, 360f);
        targetDirection = new Vector3(
            Mathf.Sin(randomAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Cos(randomAngle * Mathf.Deg2Rad)
        );
    }
}

