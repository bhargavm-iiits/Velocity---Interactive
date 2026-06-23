using UnityEditor;
using UnityEngine;
using System.IO;

public class DumpCompassHierarchy
{
    [MenuItem("Velocity Quest/Dump Compass Hierarchy")]
    public static void Dump()
    {
        GameObject compass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/compass.glb");
        if (compass == null)
        {
            Debug.LogError("Dump Compass Hierarchy: compass.glb not found!");
            return;
        }

        string report = "";
        DumpTransform(compass.transform, "", ref report);

        File.WriteAllText("compass_hierarchy_dump.txt", report);
        Debug.Log("Dump Compass Hierarchy: Saved to compass_hierarchy_dump.txt");
    }

    private static void DumpTransform(Transform t, string indent, ref string report)
    {
        report += $"{indent}Name: {t.name}\n";
        report += $"{indent}  Local Position: {t.localPosition}\n";
        report += $"{indent}  Local Rotation (Euler): {t.localRotation.eulerAngles}\n";
        report += $"{indent}  Local Scale: {t.localScale}\n";

        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
        {
            report += $"{indent}  Renderer: {r.GetType().Name}, Bounds Center: {r.bounds.center}, Size: {r.bounds.size}\n";
        }

        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransform(t.GetChild(i), indent + "  ", ref report);
        }
    }
}
