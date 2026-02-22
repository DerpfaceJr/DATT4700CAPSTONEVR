using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class MiniMap : MonoBehaviour
{
    [SerializeField] MeshFilter filter;
    [SerializeField] NavMeshSurface targetSurface;
    [SerializeField] NavMeshSurface[] otherSurfaces;

    void Awake()
    {
        Bake();
    }

    void Bake()
    {
        // Remove other NavMeshes
        foreach (var s in otherSurfaces)
            s.RemoveData();

        // Triangulate ONLY the target surface
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        Mesh mesh = new Mesh();
        mesh.vertices = triangulation.vertices;
        mesh.triangles = triangulation.indices;
        mesh.RecalculateNormals();

        filter.mesh = mesh;

        // Restore other NavMeshes
        foreach (var s in otherSurfaces)
            s.AddData();
    }
}
