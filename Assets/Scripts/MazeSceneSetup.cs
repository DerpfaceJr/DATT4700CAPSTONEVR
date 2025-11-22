using UnityEngine;

/// <summary>
/// Helper script to set up the maze scene
/// Run this in the Unity Editor to configure the scene
/// </summary>
public class MazeSceneSetup : MonoBehaviour
{
    [ContextMenu("Setup Maze Scene")]
    void SetupMazeScene()
    {
        // This will be called from Unity Editor context menu
        Debug.Log("Maze scene setup - Use Unity MCP or Editor to configure");
    }
}

