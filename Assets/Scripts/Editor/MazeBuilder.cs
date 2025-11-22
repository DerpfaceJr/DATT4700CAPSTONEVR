using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to build a maze in the SimpleArea scene
/// </summary>
public class MazeBuilder : EditorWindow
{
    [MenuItem("Tools/Build Maze")]
    static void BuildMaze()
    {
        // This will be called from Unity Editor menu
        Debug.Log("Maze Builder - Use Unity MCP to add walls");
    }
}

