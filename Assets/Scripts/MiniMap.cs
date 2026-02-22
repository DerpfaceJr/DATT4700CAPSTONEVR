using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MiniMap : MonoBehaviour
{
    [SerializeField] MeshFilter filter;

    void Start()
    {
        StartCoroutine(BakeWhenReady());
    }

    IEnumerator BakeWhenReady()
    {
        yield return null;  // Wait one frame for NavMesh to be active
        Bake();
    }

    void Bake()
    {
        if (filter == null)
        {
            Debug.LogWarning("MiniMap: MeshFilter not assigned");
            return;
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            Debug.LogWarning("MiniMap: NavMesh triangulation is empty. Ensure NavMesh is baked.");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = triangulation.vertices;
        mesh.triangles = triangulation.indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        filter.mesh = mesh;
    }
}
