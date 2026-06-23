using UnityEditor;
using UnityEngine;
using System.IO;

public class InspectChest
{
    [MenuItem("Velocity Quest/Inspect Chest")]
    public static void Inspect()
    {
        AssetDatabase.ImportAsset("Assets/wooden_treasures_box.glb");
        GameObject chest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden_treasures_box.glb");
        if (chest == null)
        {
            Debug.LogError("Inspect Chest: wooden_treasures_box.glb not found!");
            return;
        }

        string report = $"--- Chest GLB Inspection ---\n";
        report += $"Name: {chest.name}\n";

        // Find all Renderers
        Renderer[] renderers = chest.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            report += $"Renderer: {r.name}, Bounds Center: {r.bounds.center}, Size: {r.bounds.size}\n";
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null)
                {
                    report += $"  Material: {mat.name}, Shader: {mat.shader.name}\n";
                    if (mat.HasProperty("_MainTex")) report += $"    _MainTex: {mat.GetTexture("_MainTex")?.name}\n";
                    if (mat.HasProperty("_BaseMap")) report += $"    _BaseMap: {mat.GetTexture("_BaseMap")?.name}\n";
                    if (mat.HasProperty("_Color")) report += $"    _Color: {mat.GetColor("_Color")}\n";
                }
                else
                {
                    report += $"  Material: NULL\n";
                }
            }
            if (r is MeshRenderer mr)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    report += $"  Mesh name: {mf.sharedMesh.name}, Vertices: {mf.sharedMesh.vertexCount}, Mesh Bounds Size: {mf.sharedMesh.bounds.size}\n";
                }
            }
        }

        // Print full transform hierarchy
        report += "\n--- Hierarchy ---\n";
        PrintHierarchy(chest.transform, 0, ref report);

        File.WriteAllText("inspect_chest_result.txt", report);
        Debug.Log("Inspect Chest: Report saved to inspect_chest_result.txt");
    }

    private static void PrintHierarchy(Transform t, int indent, ref string report)
    {
        report += new string(' ', indent * 2) + t.name + "\n";
        for (int i = 0; i < t.childCount; i++)
        {
            PrintHierarchy(t.GetChild(i), indent + 1, ref report);
        }
    }
}
