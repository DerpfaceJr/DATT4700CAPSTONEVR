using UnityEngine;

/// <summary>
/// Resizes the SimpleArea map by 4x at runtime
/// </summary>
public class ResizeMap : MonoBehaviour
{
    [Header("Map Resize Settings")]
    [Tooltip("Scale multiplier for map size (4x = 4.0)")]
    public float mapScaleMultiplier = 4.0f;
    
    [Tooltip("Automatically resize on Start")]
    public bool resizeOnStart = true;
    
    void Start()
    {
        if (resizeOnStart)
        {
            ResizeMapGeometry();
        }
    }
    
    /// <summary>
    /// Resize the map geometry (Floor and Walls) by the scale multiplier
    /// </summary>
    public void ResizeMapGeometry()
    {
        // Resize Floor
        GameObject floor = GameObject.Find("Floor");
        if (floor != null)
        {
            Vector3 currentScale = floor.transform.localScale;
            floor.transform.localScale = new Vector3(
                currentScale.x * mapScaleMultiplier,
                currentScale.y,
                currentScale.z * mapScaleMultiplier
            );
            Debug.Log($"Resized Floor: {currentScale} -> {floor.transform.localScale}");
        }
        else
        {
            Debug.LogWarning("ResizeMap: Floor not found!");
        }
        
        // Resize and reposition walls
        ResizeWall("NorthWall", new Vector3(0, 0, 1), new Vector3(1, 1, 0.1f));
        ResizeWall("SouthWall", new Vector3(0, 0, -1), new Vector3(1, 1, 0.1f));
        ResizeWall("EastWall", new Vector3(1, 0, 0), new Vector3(0.1f, 1, 1));
        ResizeWall("WestWall", new Vector3(-1, 0, 0), new Vector3(0.1f, 1, 1));
        
        Debug.Log($"Map resized by {mapScaleMultiplier}x");
    }
    
    private void ResizeWall(string wallName, Vector3 positionDirection, Vector3 scaleDirection)
    {
        GameObject wall = GameObject.Find(wallName);
        if (wall != null)
        {
            // Calculate new position (multiply by scale multiplier)
            Vector3 currentPos = wall.transform.position;
            Vector3 newPos = positionDirection * (Mathf.Abs(currentPos.x) + Mathf.Abs(currentPos.z)) * mapScaleMultiplier;
            newPos.y = currentPos.y; // Keep Y position
            
            // Calculate new scale
            Vector3 currentScale = wall.transform.localScale;
            Vector3 newScale = new Vector3(
                scaleDirection.x > 0.5f ? currentScale.x * mapScaleMultiplier : currentScale.x,
                currentScale.y,
                scaleDirection.z > 0.5f ? currentScale.z * mapScaleMultiplier : currentScale.z
            );
            
            wall.transform.position = newPos;
            wall.transform.localScale = newScale;
            
            Debug.Log($"Resized {wallName}: Position {currentPos} -> {newPos}, Scale {currentScale} -> {newScale}");
        }
        else
        {
            Debug.LogWarning($"ResizeMap: {wallName} not found!");
        }
    }
}







