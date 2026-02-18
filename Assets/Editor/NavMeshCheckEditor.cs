using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.IO;

/// <summary>
/// Editor utility to check which areas of the scene are on the NavMesh.
/// Run via Tools > Check NavMesh Coverage. Results written to Assets/NavMeshCheckResults.txt
/// </summary>
public static class NavMeshCheckEditor
{
    [MenuItem("Tools/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        var surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogError("[NavMesh] No NavMesh Surface found in scene.");
            return;
        }
        surface.BuildNavMesh();
        Debug.Log("[NavMesh] Bake complete.");
    }

    [MenuItem("Tools/Check NavMesh Coverage")]
    public static void CheckNavMeshCoverage()
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            var tri = NavMesh.CalculateTriangulation();

            if (tri.vertices == null || tri.vertices.Length == 0)
            {
                sb.AppendLine("[NavMesh Check] No NavMesh data found. Bake the NavMesh first (select NavMesh Surface > Bake).");
                WriteResults(sb);
                return;
            }

            // Compute NavMesh bounds
            Vector3 min = tri.vertices[0];
            Vector3 max = tri.vertices[0];
            foreach (var v in tri.vertices)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            sb.AppendLine($"[NavMesh Check] NavMesh bounds: min={min}, max={max}, vertexCount={tri.vertices.Length}");

            // Sample positions: floors + bridge (should be ON), roofs excluded (ignoreFromBuild)
            var testPositions = new (string label, Vector3 position, bool expectOnNavMesh)[]
            {
                ("First floor center (0,0.5,0)", new Vector3(0, 0.5f, 0), true),
                ("First floor (-5,0.5,5)", new Vector3(-5, 0.5f, 5), true),
                ("Bridge/second floor (0,6,0)", new Vector3(0, 6, 0), true),
                ("Bridge elevated (-1,7,0)", new Vector3(-1, 7, 0), true),
                ("Floor Time Machine (-10,1,7)", new Vector3(-10, 1, 7), true),
                ("Bridge area (0,1,0)", new Vector3(0, 1, 0), true),
                ("Above NavMesh roof (0,12,0)", new Vector3(0, 12, 0), false),
                ("Above NavMesh (0,15,0)", new Vector3(0, 15, 0), false),
            };

            int onNavMesh = 0;
            int floorOk = 0, roofOk = 0;
            foreach (var (label, pos, expectOn) in testPositions)
            {
                bool hit = NavMesh.SamplePosition(pos, out NavMeshHit hitInfo, 3f, NavMesh.AllAreas);
                if (hit)
                {
                    onNavMesh++;
                    bool correct = expectOn; // Floor: expect ON = correct
                    if (correct) floorOk++;
                    sb.AppendLine($"[NavMesh Check] {(correct ? "OK" : "UNEXPECTED")} ON: {label} -> ({hitInfo.position.x:F2}, {hitInfo.position.y:F2}, {hitInfo.position.z:F2})");
                }
                else
                {
                    bool correct = !expectOn; // Roof: expect NOT = correct when we get NOT
                    if (correct) roofOk++;
                    sb.AppendLine($"[NavMesh Check] {(correct ? "OK" : "MISSING")} NOT: {label}");
                }
            }

            int floorCount = 6, roofCount = 2;
            sb.AppendLine($"[NavMesh Check] Summary: Floors+Bridge covered={floorOk}/{floorCount}, Above-roof excluded={roofOk}/{roofCount}, Total on NavMesh={onNavMesh}/{testPositions.Length}");
        }
        catch (System.Exception e)
        {
            sb.AppendLine($"[NavMesh Check] Error: {e.Message}\n{e.StackTrace}");
        }
        WriteResults(sb);
    }

    static void WriteResults(System.Text.StringBuilder sb)
    {
        string path = Path.Combine(Application.dataPath, "NavMeshCheckResults.txt");
        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[NavMesh Check] Results written to {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NavMesh Check] Write failed: {e.Message}");
        }
        AssetDatabase.Refresh();
    }
}
