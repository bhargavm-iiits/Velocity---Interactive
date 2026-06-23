using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class DumpSceneObjects
{
    static DumpSceneObjects()
    {
        // Run automatically when the compilation reload occurs
        MakeEverythingVisible();
    }

    [MenuItem("Velocity Quest/Make Everything Visible")]
    public static void MakeEverythingVisible()
    {
        Debug.Log("Velocity Quest: Running MakeEverythingVisible...");

        // 1. Show all hidden objects in the Scene view and enable picking
        SceneVisibilityManager.instance.ShowAll();
        SceneVisibilityManager.instance.EnableAllPicking();
        Debug.Log("- Restored Scene Visibility for all GameObjects.");

        // 2. Configure Terrain settings to ensure all trees, foliage, and details render at maximum distances
        Terrain[] terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (var t in terrains)
        {
            t.drawTreesAndFoliage = true;
            t.editorRenderFlags = TerrainRenderFlags.All;
            t.treeDistance = 5000f;             // Maximize draw distance
            t.treeBillboardDistance = 2000f;    // Maximize billboard distance
            t.detailObjectDistance = 1000f;     // Maximize detail/foliage distance
            t.basemapDistance = 2000f;          // Maximize texture base map distance
            
            // Force refresh of terrain
            t.Flush();
            EditorUtility.SetDirty(t);
            if (t.terrainData != null)
            {
                EditorUtility.SetDirty(t.terrainData);
            }
            Debug.Log($"- Configured Terrain '{t.name}' render settings (DrawTrees=true, RenderFlags=All).");
        }

        // 3. Make sure Scene View settings are turned on (draw skybox, fog, etc.)
        if (SceneView.sceneViews != null)
        {
            foreach (var viewObj in SceneView.sceneViews)
            {
                if (viewObj is SceneView sv)
                {
                    sv.sceneViewState.showFog = true;
                    sv.sceneViewState.showSkybox = true;
                    sv.sceneViewState.showFlares = true;
                    sv.sceneViewState.alwaysRefresh = true;
                    
                    // Focus on the player or terrain if we can find them
                    GameObject player = GameObject.FindWithTag("Player");
                    if (player != null)
                    {
                        sv.AlignViewToObject(player.transform);
                        Debug.Log($"- Aligned SceneView camera to Player at {player.transform.position}.");
                    }
                    else if (terrains.Length > 0)
                    {
                        // Focus on center of terrain
                        Vector3 center = terrains[0].transform.position + new Vector3(250f, 10f, 250f);
                        sv.LookAt(center);
                        Debug.Log($"- Aligned SceneView camera to Terrain center at {center}.");
                    }
                    
                    sv.Repaint();
                }
            }
        }

        // 4. Also perform the file dump for diagnostics
        DumpDiagnostics();
    }

    private static void DumpDiagnostics()
    {
        string report = "--- Scene Diagnostics Dump ---\n";
        
        Terrain[] terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        report += $"Total Terrains in scene: {terrains.Length}\n";
        foreach (var t in terrains)
        {
            report += $"- Terrain Name: {t.name}, Active: {t.gameObject.activeInHierarchy}, Position: {t.transform.position}\n";
            if (t.terrainData != null)
            {
                report += $"  Size: {t.terrainData.size}, Tree Instances: {t.terrainData.treeInstances.Length}, Detail Layers: {t.terrainData.detailPrototypes.Length}\n";
            }
        }
        report += "\n";

        Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        report += $"Total Cameras in scene: {cameras.Length}\n";
        foreach (var c in cameras)
        {
            report += $"- Camera Name: {c.name}, Active: {c.gameObject.activeInHierarchy}, Position: {c.transform.position}, Tag: {c.tag}, CullingMask: {c.cullingMask}\n";
        }
        report += "\n";

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        report += $"Total GameObjects: {allObjects.Length}\n\n";

        foreach (var go in allObjects)
        {
            if (go.transform.parent == null)
            {
                DumpObject(go, "", ref report);
            }
        }

        string outputPath = @"C:\Users\Dr Himangshu\.gemini\antigravity\brain\013eb59f-9642-4773-9f53-c9d6a8337156\scene_diagnostics.txt";
        File.WriteAllText(outputPath, report);
        Debug.Log("Diagnostics dumped successfully to: " + outputPath);
    }

    private static void DumpObject(GameObject go, string indent, ref string report)
    {
        report += $"{indent}- {go.name} (Active: {go.activeSelf}, Layer: {go.layer}, Pos: {go.transform.position}, Tag: {go.tag})\n";
        for (int i = 0; i < go.transform.childCount; i++)
        {
            DumpObject(go.transform.GetChild(i).gameObject, indent + "  ", ref report);
        }
    }
}
