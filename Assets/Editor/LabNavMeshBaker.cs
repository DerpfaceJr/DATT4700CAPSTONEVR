#if UNITY_EDITOR
using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Restricts NavMesh baking to The_Lab_2 hierarchy, adds NavMeshModifier ignore rules for ceilings/props,
/// and bakes synchronously (writes NavMesh asset next to the scene, same layout as the Navigation package).
/// </summary>
public static class LabNavMeshBaker
{
    const string SimpleAreaScenePath = "Assets/Scenes/SimpleArea.unity/SimpleArea.unity";
    const string LabRootName = "The_Lab_2";

    [MenuItem("Tools/Lab NavMesh/Configure The_Lab_2 Only + Bake")]
    public static void ConfigureLabOnlyAndBake()
    {
        var scene = EditorSceneManager.OpenScene(SimpleAreaScenePath, OpenSceneMode.Single);
        try
        {
            var lab = FindLabRoot();
            if (lab == null)
            {
                EditorUtility.DisplayDialog("Lab NavMesh", $"Could not find GameObject named \"{LabRootName}\" in the scene.", "OK");
                return;
            }

            ApplyIgnoreModifiersUnder(lab.transform);
            ApplyControlPanelIgnoreModifiersOutsideLab(lab.transform);

            DisableStandaloneNavMeshSurfaces(lab.transform);

            var surface = lab.GetComponent<NavMeshSurface>();
            if (surface == null)
                surface = lab.AddComponent<NavMeshSurface>();

            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = ~0;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.defaultArea = 0;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            surface.overrideVoxelSize = false;
            surface.overrideTileSize = false;
            surface.voxelSize = 0.05f;
            surface.minRegionArea = 2f;
            surface.buildHeightMesh = false;

            EditorUtility.SetDirty(lab);

            var oldDataPath = surface.navMeshData != null ? AssetDatabase.GetAssetPath(surface.navMeshData) : null;

            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogError("[LabNavMesh] BuildNavMesh produced no data.");
                return;
            }

            if (!string.IsNullOrEmpty(oldDataPath))
            {
                bool usedElsewhere = false;
                var allSurfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var s in allSurfaces)
                {
                    if (s == null || s == surface) continue;
                    if (AssetDatabase.GetAssetPath(s.navMeshData) == oldDataPath)
                    {
                        usedElsewhere = true;
                        break;
                    }
                }
                if (!usedElsewhere && AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(oldDataPath) != null)
                    AssetDatabase.DeleteAsset(oldDataPath);
            }

            var combinedAssetPath = GetNavMeshAssetPath(scene, surface);
            combinedAssetPath = AssetDatabase.GenerateUniqueAssetPath(combinedAssetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, combinedAssetPath);

            if (surface.isActiveAndEnabled)
                surface.AddData();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LabNavMesh] Bake complete. NavMesh asset: {combinedAssetPath}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Lab NavMesh", "Bake failed — see Console.", "OK");
        }
    }

    static string GetNavMeshAssetPath(Scene scene, NavMeshSurface surface)
    {
        var activeScenePath = scene.path;
        var targetPath = "Assets";
        if (!string.IsNullOrEmpty(activeScenePath))
        {
            var dir = Path.GetDirectoryName(activeScenePath);
            var fileNoExt = Path.GetFileNameWithoutExtension(activeScenePath);
            if (!string.IsNullOrEmpty(dir))
                targetPath = Path.Combine(dir, fileNoExt);
        }
        targetPath = targetPath.Replace('\\', '/');
        EnsureFolderExists(targetPath);

        var id = SanitizeAssetName(surface.gameObject.name);
        return Path.Combine(targetPath, "NavMesh-" + id + ".asset").Replace('\\', '/');
    }

    static void EnsureFolderExists(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;
        var parts = assetFolder.Split('/');
        if (parts.Length == 0) return;
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string SanitizeAssetName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Surface";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    static GameObject FindLabRoot()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            foreach (var root in s.GetRootGameObjects())
            {
                var t = FindChildRecursive(root.transform, LabRootName);
                if (t != null)
                    return t.gameObject;
            }
        }
        return null;
    }

    static Transform FindChildRecursive(Transform t, string name)
    {
        if (t.name == name)
            return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var f = FindChildRecursive(t.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    /// <summary>Only the NavMeshSurface on The_Lab_2 root should bake; any other surface can pull in All-scene geometry or duplicate data.</summary>
    static void DisableStandaloneNavMeshSurfaces(Transform labRoot)
    {
        var surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in surfaces)
        {
            if (s == null || labRoot == null) continue;
            if (s.transform == labRoot) continue;

            s.enabled = false;
            EditorUtility.SetDirty(s);
            if (!s.transform.IsChildOf(labRoot))
                s.gameObject.SetActive(false);
            EditorUtility.SetDirty(s.gameObject);
        }
    }

    static void ApplyIgnoreModifiersUnder(Transform root)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        int added = 0;
        foreach (var tr in transforms)
        {
            if (tr == null) continue;
            if (!ShouldIgnoreTransform(tr))
                continue;

            var mod = tr.GetComponent<NavMeshModifier>();
            if (mod == null)
            {
                mod = tr.gameObject.AddComponent<NavMeshModifier>();
                added++;
            }

            mod.ignoreFromBuild = true;
            mod.applyToChildren = true;
            EditorUtility.SetDirty(tr.gameObject);
        }
        Debug.Log($"[LabNavMesh] NavMeshModifier ignore-from-build: {added} new, all matching objects ensured under {LabRootName}.");
    }

    /// <summary>Control panels placed outside The_Lab_2 hierarchy (e.g. scene root) still get modifiers for consistent setup.</summary>
    static void ApplyControlPanelIgnoreModifiersOutsideLab(Transform labRoot)
    {
        int touched = 0;
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || labRoot == null) continue;
            if (go.transform == labRoot || go.transform.IsChildOf(labRoot)) continue;
            var n = go.name.ToLowerInvariant();
            if (!IsControlPanelObjectName(n)) continue;

            var mod = go.GetComponent<NavMeshModifier>();
            if (mod == null)
                mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
            mod.applyToChildren = true;
            touched++;
            EditorUtility.SetDirty(go);
        }
        if (touched > 0)
            Debug.Log($"[LabNavMesh] Control panel NavMeshModifier applied to {touched} object(s) outside {LabRootName}.");
    }

    static bool IsWalkableStructureName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var n = name.ToLowerInvariant();
        if (n.Contains("tm floor") || n.Contains("tmfloor"))
            return true;
        if (n.Contains("floor") || n.Contains("ground") || n.Contains("stair") ||
            n.Contains("ramp") || n.Contains("bridge") || n.Contains("walkway") ||
            n.Contains("catwalk") || n.Contains("balcony") || n.Contains("platform"))
            return true;
        return false;
    }

    /// <summary>True if this transform (by name or any ancestor) should be excluded from NavMesh, while preserving walkable subtrees (e.g. TM Floor).</summary>
    static bool ShouldIgnoreTransform(Transform tr)
    {
        if (tr == null) return false;
        if (IsWalkableStructureName(tr.name)) return false;
        if (ShouldIgnoreLeafName(tr.name)) return true;

        for (Transform p = tr.parent; p != null; p = p.parent)
        {
            if (IsWalkableStructureName(p.name)) return false;
            if (MatchesIgnoreAncestor(p.name)) return true;
        }

        return false;
    }

    /// <summary>Ancestor names that imply the whole subtree is non-walkable (except when a walkable-named branch sits in between).</summary>
    static bool MatchesIgnoreAncestor(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var n = name.ToLowerInvariant().Trim();

        if (n == "time machine") return true;
        if (n.Contains("ceiling")) return true;
        if (n.Contains("roof")) return true;
        if (n.Contains("duct")) return true;
        if (n.Contains("truss")) return true;
        if (n.Contains("fluorescent")) return true;
        if (n.Contains("soffit")) return true;
        if (n.Contains("acoustic")) return true;
        if (n.Contains("plenum")) return true;
        if (IsControlPanelObjectName(n)) return true;
        if (n.Contains("locker")) return true;

        return false;
    }

    /// <summary>Heuristic: exclude ceilings/overhead and prop geometry on this object; keep floors and stairs (incl. 2nd floor).</summary>
    static bool ShouldIgnoreLeafName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (IsWalkableStructureName(name)) return false;

        var n = name.ToLowerInvariant().Trim();

        // Exact match avoids false positives (e.g. TimeMachineBGM) from loose "time"+"machine" checks.
        if (n == "time machine") return true;

        if (n.Contains("ceiling")) return true;
        if (n.Contains("roof")) return true;
        if (n.Contains("duct")) return true;
        if (n.Contains("truss")) return true;
        if (n.Contains("fluorescent")) return true;
        if (n.Contains("soffit")) return true;
        if (n.Contains("acoustic")) return true;
        if (n.Contains("plenum")) return true;

        if (n.Contains("locker")) return true;
        if (n.Contains("bench")) return true;
        if (n.Contains("chair")) return true;
        if (n.Contains("shelf")) return true;
        if (n.Contains("cabinet")) return true;
        if (n.Contains("counter")) return true;
        if (n.Contains("desk")) return true;
        if (IsControlPanelObjectName(n)) return true;
        if (n.Contains("kitchen_table")) return true;
        if (n.Contains("table")) return true;

        return false;
    }

    /// <summary>Matches lab control panel meshes (any common naming).</summary>
    static bool IsControlPanelObjectName(string nameLower)
    {
        if (string.IsNullOrEmpty(nameLower)) return false;
        if (nameLower.Contains("controlpanel")) return true;
        if (nameLower.Contains("control panel")) return true;
        if (nameLower.Contains("control_panel")) return true;
        if (nameLower.Contains("control-panel")) return true;
        if (nameLower.Contains("control") && nameLower.Contains("panel")) return true;
        return false;
    }
}
#endif
