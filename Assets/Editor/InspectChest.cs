using UnityEditor;
using UnityEngine;
using System.IO;

public class InspectChest
{
    [MenuItem("Velocity Quest/Inspect Chest")]
    public static void Inspect()
    {
        string[] chestPaths = new string[]
        {
            "Assets/Envirornment/treasure-box/source/SnakeTreasureBox.fbx",
            "Assets/Envirornment/metal_dragon_chinese_trinket_box_low_poly.glb",
            "Assets/Envirornment/stylized-medieval-chest/source/Meshy_AI_Medieval_fantasy_trea_0604223036_texture_fbx/Meshy_AI_Medieval_fantasy_trea_0604223036_texture_fbx/Meshy_AI_Medieval_fantasy_trea_0604223036_texture.fbx",
            "Assets/Envirornment/treasure-chest-scan/source/Box.glb",
            "Assets/Envirornment/treasure-chest/source/finlowlowlow.fbx"
        };

        string report = "=== Chest Models Hierarchy Inspection ===\n\n";

        for (int i = 0; i < chestPaths.Length; i++)
        {
            string path = chestPaths[i];
            int levelIndex = i + 1;
            report += $"--- LEVEL {levelIndex} CHEST (Path: {path}) ---\n";
            
            AssetDatabase.ImportAsset(path);
            GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (chestPrefab == null)
            {
                report += "ERROR: Could not load asset!\n\n";
                continue;
            }

            report += $"Prefab Name: {chestPrefab.name}\n";
            report += "Hierarchy:\n";
            PrintHierarchy(chestPrefab.transform, 1, ref report);
            report += "\nRenderers in Prefab:\n";
            Renderer[] renderers = chestPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                report += $"  Renderer name: {r.name}, Type: {r.GetType().Name}, Enabled: {r.enabled}\n";
            }
            report += "\n\n";
        }

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
