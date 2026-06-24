using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

public class CheckPlayerStuck : EditorWindow
{
    [MenuItem("Velocity Quest/Check Player Stuck")]
    public static void DiagnoseStuckPlayer()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("--- STUCK PLAYER DIAGNOSTICS REPORT ---");
        
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        
        if (player == null)
        {
            sb.AppendLine("Error: No Player GameObject found in the scene (tagged 'Player' or named 'Player')!");
            File.WriteAllText("player_stuck_report.txt", sb.ToString());
            Debug.LogError("Diagnostics: No Player GameObject found!");
            return;
        }
        
        sb.AppendLine($"Player GameObject Name: {player.name}");
        sb.AppendLine($"Active in Hierarchy: {player.activeInHierarchy}");
        sb.AppendLine($"Position: {player.transform.position}");
        sb.AppendLine($"Rotation (Euler): {player.transform.rotation.eulerAngles}");
        sb.AppendLine($"Scale: {player.transform.localScale}");
        sb.AppendLine($"Layer: {player.gameObject.layer} ({LayerMask.LayerToName(player.gameObject.layer)})");
        
        // CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            sb.AppendLine("--- CharacterController settings ---");
            sb.AppendLine($"Enabled: {cc.enabled}");
            sb.AppendLine($"isGrounded: {cc.isGrounded}");
            sb.AppendLine($"Center: {cc.center}");
            sb.AppendLine($"Height: {cc.height}");
            sb.AppendLine($"Radius: {cc.radius}");
            sb.AppendLine($"Slope Limit: {cc.slopeLimit}");
            sb.AppendLine($"Step Offset: {cc.stepOffset}");
            sb.AppendLine($"Velocity: {cc.velocity}");
        }
        else
        {
            sb.AppendLine("No CharacterController component found on Player!");
        }
        
        // PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            sb.AppendLine("--- PlayerController settings ---");
            sb.AppendLine($"Enabled: {pc.enabled}");
            sb.AppendLine($"baseWalkSpeedMPS: {pc.baseWalkSpeedMPS}");
            sb.AppendLine($"sprintSpeedMPS: {pc.sprintSpeedMPS}");
            sb.AppendLine($"currentSpeedMPS: {pc.currentSpeedMPS}");
            sb.AppendLine($"currentDirectionString: {pc.currentDirectionString}");
            sb.AppendLine($"windForce: {pc.windForce}");
        }
        else
        {
            sb.AppendLine("No PlayerController component found on Player!");
        }
        
        // Check for overlapping colliders using CharacterController dimensions
        if (cc != null)
        {
            Vector3 center = player.transform.position + cc.center;
            float halfHeight = cc.height / 2f;
            float radius = cc.radius;
            
            // Capsule points
            Vector3 point1 = center + Vector3.up * (halfHeight - radius);
            Vector3 point2 = center - Vector3.up * (halfHeight - radius);
            
            sb.AppendLine("--- Overlapping Colliders ---");
            Collider[] colls = Physics.OverlapCapsule(point1, point2, radius);
            sb.AppendLine($"Total overlapping colliders: {colls.Length}");
            foreach (var col in colls)
            {
                if (col.gameObject == player) continue;
                sb.AppendLine($"- Name: {col.name}");
                sb.AppendLine($"  GameObject: {col.gameObject.name}");
                sb.AppendLine($"  Path: {GetGameObjectPath(col.gameObject)}");
                sb.AppendLine($"  Tag: {col.tag}");
                sb.AppendLine($"  Layer: {col.gameObject.layer} ({LayerMask.LayerToName(col.gameObject.layer)})");
                sb.AppendLine($"  isTrigger: {col.isTrigger}");
                sb.AppendLine($"  Enabled: {col.enabled}");
                sb.AppendLine($"  Type: {col.GetType().Name}");
            }
        }
        
        // Check for all colliders within 5 meters
        sb.AppendLine("--- Colliders within 5m ---");
        Collider[] nearby = Physics.OverlapSphere(player.transform.position, 5f);
        sb.AppendLine($"Total nearby colliders: {nearby.Length}");
        foreach (var col in nearby)
        {
            if (col.gameObject == player) continue;
            sb.AppendLine($"- Name: {col.name} (Dist: {Vector3.Distance(player.transform.position, col.transform.position):F2}m, isTrigger: {col.isTrigger})");
        }
        
        File.WriteAllText("player_stuck_report.txt", sb.ToString());
        Debug.Log("Diagnostics report written to player_stuck_report.txt");
    }
    
    private static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        Transform curr = obj.transform;
        while (curr.parent != null)
        {
            curr = curr.parent;
            path = "/" + curr.name + path;
        }
        return path;
    }
}
