using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class OpenDefaultScene
{
    static OpenDefaultScene()
    {
        EditorApplication.delayCall += () =>
        {
            string targetScene = "Assets/Envirornment/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity";
            Scene activeScene = SceneManager.GetActiveScene();
            
            // If the active scene is not the flat terrain scene, open it automatically
            if (string.IsNullOrEmpty(activeScene.path) || activeScene.path != targetScene)
            {
                string absPath = System.IO.Path.Combine(Application.dataPath, "Envirornment/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity");
                if (System.IO.File.Exists(absPath))
                {
                    Debug.Log($"Velocity Quest: Automatically switching active scene to the plane ground scene: {targetScene}");
                    EditorSceneManager.OpenScene(targetScene, OpenSceneMode.Single);
                    
                    // Trigger setup if it hasn't been done yet
                    TriggerSetupIfMissing();
                }
            }
            else
            {
                // If it is the correct scene but the gameplay objects are missing, trigger setup
                TriggerSetupIfMissing();
            }
        };

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            TriggerSetupIfMissing();
        }
    }

    private static void TriggerSetupIfMissing()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // Check if TextMesh Pro folders are imported. If not, trigger menu import
        string tmpPath = System.IO.Path.Combine(Application.dataPath, "TextMesh Pro");
        if (!System.IO.Directory.Exists(tmpPath))
        {
            Debug.LogWarning("Velocity Quest: TextMesh Pro folder is missing! Triggering Import TMP Essential Resources...");
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        }

        Terrain terrain = GameObject.FindFirstObjectByType<Terrain>();
        bool isNotFlat = false;
        if (terrain != null && terrain.terrainData != null)
        {
            // Sample a 10x10 block in the corner/center to see if heights are non-zero
            float[,] heights = terrain.terrainData.GetHeights(0, 0, 10, 10);
            foreach (float h in heights)
            {
                if (h > 0.001f)
                {
                    isNotFlat = true;
                    break;
                }
            }
        }

        bool isSparse = terrain != null && terrain.terrainData != null && terrain.terrainData.treeInstances.Length < 20000;
        bool forceRun = !EditorPrefs.GetBool("VelocityQuest_3DSignBoards_v41", false);

        if (GameObject.Find("VelocityQuest_Gameplay") == null || isNotFlat || isSparse || forceRun)
        {
            Debug.Log("Velocity Quest: Automatically running setup to apply winding zig-zag roads and m/s settings...");
            TerrainSetupUtility.SetupTerrainAndPaths();
            InspectChest.Inspect();
            InspectCompass.Inspect();
            DumpCompassHierarchy.Dump();
            DumpSceneObjects.MakeEverythingVisible();
            EditorPrefs.SetBool("VelocityQuest_3DSignBoards_v41", true);
        }
        else
        {
            DumpSceneObjects.MakeEverythingVisible();
        }
    }
}
