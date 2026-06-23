using UnityEditor;
using UnityEngine;
using System.IO;

public class InspectCompass
{
    [MenuItem("Velocity Quest/Inspect Compass")]
    public static void Inspect()
    {
        AssetDatabase.ImportAsset("Assets/compass.glb");
        GameObject compass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/compass.glb");
        if (compass == null)
        {
            Debug.LogError("Inspect Compass: compass.glb not found!");
            return;
        }

        string report = $"--- Compass GLB Inspection ---\n";
        report += $"Name: {compass.name}\n";

        // Find all Renderers
        Renderer[] renderers = compass.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            report += $"Renderer: {r.name}, Bounds Center: {r.bounds.center}, Size: {r.bounds.size}\n";
            if (r is MeshRenderer mr)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    report += $"  Mesh name: {mf.sharedMesh.name}, Vertices: {mf.sharedMesh.vertexCount}, Mesh Bounds Size: {mf.sharedMesh.bounds.size}\n";
                }
            }
        }

        File.WriteAllText("inspect_compass_result.txt", report);
        Debug.Log("Inspect Compass: Report saved to inspect_compass_result.txt");
    }
}
