using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;

public class TerrainSetupUtility : EditorWindow
{
    [MenuItem("Velocity Quest/Setup Terrain & Paths")]
    public static void SetupTerrainAndPaths()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Velocity Quest: Cannot run terrain setup during Play Mode.");
            return;
        }
        Terrain terrain = null;

        // 1. Search in all loaded scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                GameObject[] rootGOs = scene.GetRootGameObjects();
                foreach (var go in rootGOs)
                {
                    Terrain t = go.GetComponentInChildren<Terrain>(true);
                    if (t != null)
                    {
                        terrain = t;
                        SceneManager.SetActiveScene(scene);
                        break;
                    }
                }
            }
            if (terrain != null) break;
        }

        // 2. If no terrain is open, try to open the target DemoGrassFlowers scene
        if (terrain == null)
        {
            string sceneRelPath = "Assets/Envirornment/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity";
            string sceneAbsPath = System.IO.Path.Combine(Application.dataPath, "Envirornment/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity");
            if (System.IO.File.Exists(sceneAbsPath))
            {
                Debug.Log($"Velocity Quest: Opening scene '{sceneRelPath}' to find the terrain...");
                Scene scene = EditorSceneManager.OpenScene(sceneRelPath, OpenSceneMode.Single);
                GameObject[] rootGOs = scene.GetRootGameObjects();
                foreach (var go in rootGOs)
                {
                    Terrain t = go.GetComponentInChildren<Terrain>(true);
                    if (t != null)
                    {
                        terrain = t;
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError($"Velocity Quest: Scene file not found at '{sceneAbsPath}'!");
            }
        }

        // 3. Fallback search (find active terrain or any loaded in memory)
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null) terrain = FindFirstObjectByType<Terrain>();
        if (terrain == null)
        {
            Terrain[] terrains = Resources.FindObjectsOfTypeAll<Terrain>();
            foreach (var t in terrains)
            {
                if (t.gameObject.scene.name != null && !EditorUtility.IsPersistent(t))
                {
                    terrain = t;
                    break;
                }
            }
        }

        if (terrain == null)
        {
            Debug.LogError("Velocity Quest: No Terrain found in the active scene! Please ensure the DemoGrassFlowers scene is loaded.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Setup Terrain and Paths");

        TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
        if (tc != null)
        {
            Undo.RegisterCompleteObjectUndo(tc, "Disable Tree Colliders");
            SerializedObject so = new SerializedObject(tc);
            SerializedProperty prop = so.FindProperty("m_EnableTreeColliders");
            if (prop != null)
            {
                prop.boolValue = true;
                so.ApplyModifiedProperties();
                Debug.Log("Velocity Quest: Enabled tree colliders on TerrainCollider.");
            }
        }

        TerrainData terrainData = terrain.terrainData;
        Debug.Log($"Velocity Quest: Found Terrain '{terrain.name}' of size {terrainData.size.x}x{terrainData.size.z}");

        // 0. Flatten the terrain to make it plane ground
        FlattenTerrain(terrainData);

        // 1. Setup Terrain Layers (Grass base and Road Layer)
        SetupTerrainLayers(terrainData);

        Debug.Log($"[DEBUG] Terrain Material Template: {(terrain.materialTemplate != null ? terrain.materialTemplate.name : "None (Default)")}");
        Debug.Log($"[DEBUG] Terrain Layers Count: {terrainData.terrainLayers.Length}");
        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            var l = terrainData.terrainLayers[i];
            Debug.Log($"[DEBUG] Layer {i}: {l.name} (Texture: {(l.diffuseTexture != null ? l.diffuseTexture.name : "null")})");
        }

        // 2. Setup Grass Detail Prototypes (Clear flowers, add grass01 & grass02)
        SetupDetailPrototypes(terrainData);

        // 3. Setup Tree Prototypes (4 Conifer models)
        SetupTreePrototypes(terrainData);

        // 4. Generate Path and Obstacles layout
        var paths = DefineGamePaths(terrainData.size.x, terrainData.size.z);

        // 5. Paint Path Texture
        PaintPaths(terrainData, paths);

        // 6. Generate Trees (Random positioning, avoiding paths)
        GenerateTrees(terrainData, paths);

        // 7. Generate Grass Details (Random positioning, avoiding paths)
        GenerateGrass(terrainData, paths);

        // 8. Spawn Game Elements (Managers, Player, Chests, Checkpoints)
        SpawnGameWorldObjects(terrain, paths);

        // Register scene in EditorBuildSettings
        string scenePath = "Assets/Envirornment/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity";
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(scenePath, true) };

        // Force refresh and save to disk
        terrain.Flush();
        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(terrainData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        
        Debug.Log("Velocity Quest: Terrain and Game Objects setup completed successfully!");
    }

    private static void FlattenTerrain(TerrainData terrainData)
    {
        int res = terrainData.heightmapResolution;
        float[,] heights = new float[res, res]; // Initialize all to 0
        terrainData.SetHeights(0, 0, heights);
        Debug.Log("Velocity Quest: Flattened terrain to 0 (plane ground).");
    }

    private static void SetupTerrainLayers(TerrainData terrainData)
    {
        TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Envirornment/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers/layer_Grass01_BigUVd261c09ae55ba1e3.terrainlayer");
        if (grassLayer == null) grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/TerrainLayers/Grass_A_TerrainLayer.terrainlayer");

        TerrainLayer roadLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Editor/Road_TerrainLayer.terrainlayer");

        if (grassLayer == null)
        {
            Debug.LogError("Velocity Quest: Failed to load grass terrain layer.");
            return;
        }

        if (roadLayer == null)
        {
            Debug.LogWarning("Velocity Quest: Failed to load Road_TerrainLayer. Creating one dynamically...");
            roadLayer = new TerrainLayer();
            roadLayer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Envirornment/ADG_Textures/ground_vol1/ground2/ground2_Diffuse.tga");
            roadLayer.tileSize = new Vector2(10, 10);
            AssetDatabase.CreateAsset(roadLayer, "Assets/Editor/Road_TerrainLayer.terrainlayer");
        }

        terrainData.terrainLayers = new TerrainLayer[] { grassLayer, roadLayer };
        Debug.Log("Velocity Quest: Configured Terrain Layers (Grass base and Road layer).");
    }

    private static void SetupDetailPrototypes(TerrainData terrainData)
    {
        Texture2D grass01 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Envirornment/ALP_Assets/GrassFlowersFREE/Textures/GrassFlowers/grass01.tga");
        Texture2D grass02 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Envirornment/ALP_Assets/GrassFlowersFREE/Textures/GrassFlowers/grass02.tga");

        if (grass01 == null || grass02 == null)
        {
            Debug.LogError($"Velocity Quest: Failed to load grass textures. grass01: {grass01}, grass02: {grass02}");
            return;
        }

        DetailPrototype proto1 = new DetailPrototype
        {
            prototypeTexture = grass01,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = 1.0f,
            maxWidth = 2.0f,
            minHeight = 0.8f,
            maxHeight = 1.5f,
            noiseSpread = 0.5f,
            healthyColor = new Color(0.8f, 0.95f, 0.5f),
            dryColor = new Color(0.7f, 0.8f, 0.4f)
        };

        DetailPrototype proto2 = new DetailPrototype
        {
            prototypeTexture = grass02,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = 1.0f,
            maxWidth = 2.2f,
            minHeight = 0.8f,
            maxHeight = 1.6f,
            noiseSpread = 0.5f,
            healthyColor = new Color(0.75f, 0.9f, 0.45f),
            dryColor = new Color(0.65f, 0.75f, 0.35f)
        };

        terrainData.detailPrototypes = new DetailPrototype[] { proto1, proto2 };
        Debug.Log("Velocity Quest: Setup grass detail prototypes (Removed flowers).");
    }

    private static void SetupTreePrototypes(TerrainData terrainData)
    {
        string[] treePaths = new string[]
        {
            "Assets/Envirornment/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Bare BOTD URP.prefab",
            "Assets/Envirornment/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Medium BOTD URP.prefab",
            "Assets/Envirornment/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Small BOTD URP.prefab",
            "Assets/Envirornment/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Tall BOTD URP.prefab"
        };

        List<TreePrototype> treePrototypesList = new List<TreePrototype>();

        foreach (var path in treePaths)
        {
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (treePrefab != null)
            {
                // Remove colliders from the tree prefab to allow free movement
                string assetPath = AssetDatabase.GetAssetPath(treePrefab);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                Collider[] colliders = prefabRoot.GetComponentsInChildren<Collider>(true);
                if (colliders.Length > 0)
                {
                    foreach (var c in colliders)
                    {
                        DestroyImmediate(c);
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                    Debug.Log($"Velocity Quest: Removed {colliders.Length} colliders from tree prefab '{treePrefab.name}'");
                }
                PrefabUtility.UnloadPrefabContents(prefabRoot);

                TreePrototype proto = new TreePrototype
                {
                    prefab = treePrefab,
                    bendFactor = 0.2f
                };
                treePrototypesList.Add(proto);
            }
            else
            {
                Debug.LogWarning($"Velocity Quest: Tree prefab not found at {path}");
            }
        }

        terrainData.treePrototypes = treePrototypesList.ToArray();
        Debug.Log($"Velocity Quest: Registered {terrainData.treePrototypes.Length} tree prototypes.");
    }

    // Structure to represent a segment of a path
    public struct PathSegment
    {
        public Vector2 start;
        public Vector2 end;
        public float width;

        public PathSegment(Vector2 start, Vector2 end, float width)
        {
            this.start = start;
            this.end = end;
            this.width = width;
        }

        public bool IsPointNear(Vector2 point, float tolerance)
        {
            Vector2 ab = end - start;
            Vector2 ap = point - start;
            float ab2 = Vector2.Dot(ab, ab);
            if (ab2 <= 0f) return Vector2.Distance(point, start) < (width + tolerance);

            float t = Vector2.Dot(ap, ab) / ab2;
            t = Mathf.Clamp01(t);
            Vector2 closest = start + t * ab;
            return Vector2.Distance(point, closest) < (width + tolerance);
        }
    }

    public static List<Vector2> GetGameplayKeyPoints(float w, float l)
    {
        return new List<Vector2>
        {
            new Vector2(0.2f * w, 0.1f * l),      // pStart
            new Vector2(0.2f * w, 0.22f * l),     // pL1Junc
            new Vector2(0.12f * w, 0.22f * l),    // pL1DeadE
            new Vector2(0.28f * w, 0.22f * l),    // pL1DeadW
            new Vector2(0.2f * w, 0.32f * l),     // pL1Chest

            new Vector2(0.4f * w, 0.32f * l),     // pL2Wood
            new Vector2(0.4f * w, 0.12f * l),     // pL2Stone
            new Vector2(0.46f * w, 0.12f * l),    // pL2Camp
            new Vector2(0.52f * w, 0.12f * l),    // pL2Chest

            new Vector2(0.52f * w, 0.28f * l),    // pL3J1
            new Vector2(0.68f * w, 0.28f * l),    // pL3J2
            new Vector2(0.68f * w, 0.44f * l),    // pL3Cave
            new Vector2(0.44f * w, 0.28f * l),    // pL3J1Dead
            new Vector2(0.68f * w, 0.2f * l),     // pL3J2Dead

            new Vector2(0.3f * w, 0.44f * l),     // pL4Junc
            new Vector2(0.3f * w, 0.58f * l),     // pL4BridgeJunc
            new Vector2(0.24f * w, 0.44f * l),    // pL4BridgeStart
            new Vector2(0.18f * w, 0.46f * l),    // pL4ForestStart
            new Vector2(0.12f * w, 0.48f * l),    // pL4MountainStart
            new Vector2(0.22f * w, 0.6f * l),     // pL4Chest

            new Vector2(0.22f * w, 0.72f * l),    // pL5J1
            new Vector2(0.38f * w, 0.72f * l),    // pL5J2
            new Vector2(0.38f * w, 0.85f * l),    // pL5J3
            new Vector2(0.5f * w, 0.85f * l)      // pL5Temple
        };
    }

    public static List<PathSegment> DefineGamePaths(float terrainWidth, float terrainLength)
    {
        List<PathSegment> segments = new List<PathSegment>();
        float w = terrainWidth;
        float l = terrainLength;

        // Path width is 4m
        float pw = 4.0f;

        Vector2 pStart = new Vector2(0.2f * w, 0.1f * l);
        Vector2 pL1Junc = new Vector2(0.2f * w, 0.22f * l);
        Vector2 pL1Chest = new Vector2(0.2f * w, 0.32f * l);

        Vector2 pL2Wood = new Vector2(0.4f * w, 0.32f * l);
        Vector2 pL2Chest = new Vector2(0.52f * w, 0.12f * l);

        Vector2 pL3J1 = new Vector2(0.52f * w, 0.28f * l);
        Vector2 pL3Cave = new Vector2(0.68f * w, 0.44f * l);

        Vector2 pL4Junc = new Vector2(0.3f * w, 0.44f * l);
        Vector2 pL4Chest = new Vector2(0.22f * w, 0.6f * l);
        Vector2 pL5J1 = new Vector2(0.22f * w, 0.72f * l);

        // We only define roads/paths leading TO the first junction/board of each level:
        AddZigZagPathAuto(segments, pStart, pL1Junc, pw);    // Level 1 approach (Start to Junction 1)
        AddZigZagPathAuto(segments, pL1Chest, pL2Wood, pw); // Level 2 approach (Chest 1 to Wood Junction)
        AddZigZagPathAuto(segments, pL2Chest, pL3J1, pw);   // Level 3 approach (Chest 2 to Junction 3_1)
        AddZigZagPathAuto(segments, pL3Cave, pL4Junc, pw);  // Level 4 approach (Cave to Junction 4)
        AddZigZagPathAuto(segments, pL4Chest, pL5J1, pw);   // Level 5 approach (Chest 4 to Junction 5_1)

        return segments;
    }

    private static Vector2[] GenerateZigZagPoints(Vector2 start, Vector2 end, float amplitude, int subdivisions, float sideOffset, int seedOffset)
    {
        Vector2[] points = new Vector2[subdivisions + 1];
        Vector2 dir = end - start;
        float length = dir.magnitude;
        if (length <= 0.01f)
        {
            for (int i = 0; i <= subdivisions; i++) points[i] = start;
            return points;
        }

        dir.Normalize();
        Vector2 perp = new Vector2(-dir.y, dir.x); // Perpendicular vector

        Random.State oldState = Random.state;
        Random.InitState((int)(start.x + start.y + end.x + end.y) + seedOffset);

        points[0] = start;
        points[subdivisions] = end;

        for (int i = 1; i < subdivisions; i++)
        {
            float t = (float)i / subdivisions;
            Vector2 pt = Vector2.Lerp(start, end, t);

            float envelope = Mathf.Sin(t * Mathf.PI);
            float side = (i % 2 == 0) ? 1.0f : -1.0f;
            float zigZagOffset = side * amplitude + Random.Range(-amplitude * 0.3f, amplitude * 0.3f);
            
            // Shift left/right by sideOffset and zig-zag, scaled by envelope so they merge at ends
            pt += perp * (zigZagOffset + sideOffset) * envelope;
            points[i] = pt;
        }

        Random.state = oldState;
        return points;
    }

    private static void AddZigZagPathAuto(List<PathSegment> list, Vector2 start, Vector2 end, float width)
    {
        float dist = Vector2.Distance(start, end);
        int subdivisions = Mathf.Max(3, Mathf.RoundToInt(dist / 15f));
        float amplitude = Mathf.Clamp(dist * 0.12f, 1.5f, 8f); // Slightly smaller amplitude to fit nicely
        
        // Generate Path 1 (shifted Left by 6 meters)
        Vector2[] points1 = GenerateZigZagPoints(start, end, amplitude, subdivisions, -6.0f, seedOffset: 0);

        // Generate Path 2 (shifted Right by 6 meters)
        Vector2[] points2 = GenerateZigZagPoints(start, end, amplitude, subdivisions, 6.0f, seedOffset: 9999);

        // Add main path segments
        for (int i = 0; i < subdivisions; i++)
        {
            list.Add(new PathSegment(points1[i], points1[i + 1], width));
            list.Add(new PathSegment(points2[i], points2[i + 1], width));
        }

        // Add random cross-connections between the parallel paths
        if (subdivisions >= 3)
        {
            int idx1 = subdivisions / 3;
            int idx2 = (2 * subdivisions) / 3;
            list.Add(new PathSegment(points1[idx1], points2[idx1], width));
            if (idx2 != idx1)
            {
                list.Add(new PathSegment(points1[idx2], points2[idx2], width));
            }
        }
        else if (subdivisions == 2)
        {
            list.Add(new PathSegment(points1[1], points2[1], width));
        }
    }

    private static void PaintPaths(TerrainData terrainData, List<PathSegment> paths)
    {
        int mapW = terrainData.alphamapWidth;
        int mapH = terrainData.alphamapHeight;
        float[,,] maps = terrainData.GetAlphamaps(0, 0, mapW, mapH);

        float terrW = terrainData.size.x;
        float terrL = terrainData.size.z;

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                // Convert map coordinates to world coordinates
                float worldX = ((float)x / mapW) * terrW;
                float worldZ = ((float)y / mapH) * terrL;
                Vector2 pos = new Vector2(worldX, worldZ);

                bool isPath = false;
                foreach (var seg in paths)
                {
                    if (seg.IsPointNear(pos, 0.0f)) // Tolerance is 0 because segment width is already factored in
                    {
                        isPath = true;
                        break;
                    }
                }

                if (isPath)
                {
                    maps[y, x, 0] = 0.0f; // Grass
                    maps[y, x, 1] = 1.0f; // Road
                }
                else
                {
                    maps[y, x, 0] = 1.0f; // Grass
                    maps[y, x, 1] = 0.0f; // Road
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, maps);
        Debug.Log("Velocity Quest: Painted terrain paths with Road texture (ground2_Diffuse) and rest with Grass.");
    }

    private static void GenerateTrees(TerrainData terrainData, List<PathSegment> paths)
    {
        terrainData.treeInstances = new TreeInstance[0]; // Clear existing
        List<TreeInstance> treeList = new List<TreeInstance>();

        int treeCount = 35000;
        float terrW = terrainData.size.x;
        float terrL = terrainData.size.z;

        List<Vector2> keyPoints = GetGameplayKeyPoints(terrW, terrL);

        Random.InitState(42);

        for (int i = 0; i < treeCount; i++)
        {
            float normX = Random.value;
            float normZ = Random.value;

            float worldX = normX * terrW;
            float worldZ = normZ * terrL;
            Vector2 pos = new Vector2(worldX, worldZ);

            // Check distance to paths - keep clear of paths (bring trees closer to road edges)
            bool tooClose = false;
            foreach (var seg in paths)
            {
                if (seg.IsPointNear(pos, 1.5f))
                {
                    tooClose = true;
                    break;
                }
            }

            // Avoid spawning trees on top of critical gameplay points (chests, signboards, checkpoints)
            if (!tooClose)
            {
                foreach (var kp in keyPoints)
                {
                    if (Vector2.Distance(pos, kp) < 5.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }

            if (!tooClose)
            {
                // Sample terrain height at the tree's position and normalize it
                float worldY = terrainData.GetHeight(Mathf.RoundToInt(normX * (terrainData.heightmapResolution - 1)), Mathf.RoundToInt(normZ * (terrainData.heightmapResolution - 1)));
                float normY = worldY / terrainData.size.y;

                TreeInstance tree = new TreeInstance
                {
                    position = new Vector3(normX, normY, normZ), // Normalized coordinates with correct height
                    prototypeIndex = Random.Range(0, terrainData.treePrototypes.Length),
                    widthScale = Random.Range(0.8f, 1.3f),
                    heightScale = Random.Range(0.8f, 1.5f),
                    color = Color.white,
                    lightmapColor = Color.white,
                    rotation = Random.Range(0f, Mathf.PI * 2)
                };

                treeList.Add(tree);
            }
        }

        terrainData.treeInstances = treeList.ToArray();
        Debug.Log($"Velocity Quest: Placed {treeList.Count} trees on terrain (avoiding paths and key points).");
    }

    private static void GenerateGrass(TerrainData terrainData, List<PathSegment> paths)
    {
        int detailW = terrainData.detailWidth;
        int detailH = terrainData.detailHeight;

        // Clear existing details
        for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
        {
            int[,] cleanMap = new int[detailH, detailW];
            terrainData.SetDetailLayer(0, 0, layer, cleanMap);
        }

        int[,] detailMap0 = new int[detailH, detailW];
        int[,] detailMap1 = new int[detailH, detailW];

        float terrW = terrainData.size.x;
        float terrL = terrainData.size.z;

        List<Vector2> keyPoints = GetGameplayKeyPoints(terrW, terrL);

        Random.InitState(1337);

        for (int y = 0; y < detailH; y++)
        {
            for (int x = 0; x < detailW; x++)
            {
                float worldX = ((float)x / detailW) * terrW;
                float worldZ = ((float)y / detailH) * terrL;
                Vector2 pos = new Vector2(worldX, worldZ);

                // Check distance to paths - keep clear of paths
                bool tooClose = false;
                foreach (var seg in paths)
                {
                    if (seg.IsPointNear(pos, 2.0f))
                    {
                        tooClose = true;
                        break;
                    }
                }

                // Avoid spawning detail grass on top of critical gameplay points (chests, signboards, checkpoints)
                if (!tooClose)
                {
                    foreach (var kp in keyPoints)
                    {
                        if (Vector2.Distance(pos, kp) < 3.5f)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }

                if (!tooClose)
                {
                    // Random noise grass density
                    float perlin = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f);
                    if (perlin > 0.4f)
                    {
                        int density = Mathf.RoundToInt((perlin - 0.4f) * 16.0f);
                        if (Random.value > 0.5f)
                            detailMap0[y, x] = density;
                        else
                            detailMap1[y, x] = density;
                    }
                }
            }
        }

        terrainData.SetDetailLayer(0, 0, 0, detailMap0);
        terrainData.SetDetailLayer(0, 0, 1, detailMap1);
        Debug.Log("Velocity Quest: Generated grass details on terrain (avoiding paths and key points)..");
    }

    private static void SpawnGameWorldObjects(Terrain terrain, List<PathSegment> paths)
    {
        float w = terrain.terrainData.size.x;
        float l = terrain.terrainData.size.z;

        // Parent object to clean up the hierarchy
        GameObject parentRoot = GameObject.Find("VelocityQuest_Gameplay");
        if (parentRoot != null) DestroyImmediate(parentRoot);
        parentRoot = new GameObject("VelocityQuest_Gameplay");

        // 1. Spawning the Player
        Vector3 pStartWorld = GetTerrainPos(terrain, 0.2f * w, 0.1f * l);
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            playerObj.tag = "Player";
        }
        playerObj.transform.position = pStartWorld + Vector3.up * 1f;
        playerObj.transform.SetParent(parentRoot.transform);

        // Adjust collider - destroy the duplicate CapsuleCollider to prevent conflicts with CharacterController
        CapsuleCollider cap = playerObj.GetComponent<CapsuleCollider>();
        if (cap != null) DestroyImmediate(cap);

        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc == null) cc = playerObj.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1, 0);
        cc.height = 2f;

        PlayerController pc = playerObj.GetComponent<PlayerController>();
        if (pc == null) pc = playerObj.AddComponent<PlayerController>();

        AssetDatabase.ImportAsset("Assets/compass.glb");
        GameObject compassModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/compass.glb");
        if (compassModel != null)
        {
            pc.compassPrefab = compassModel;
        }

        WindZoneEffect wze = playerObj.GetComponent<WindZoneEffect>();
        if (wze == null) wze = playerObj.AddComponent<WindZoneEffect>();

        // Set camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        mainCam.transform.SetParent(playerObj.transform);
        mainCam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        mainCam.transform.localRotation = Quaternion.identity;
        pc.vrCameraTransform = mainCam.transform;

        // 2. Compass System
        GameObject compassObj = new GameObject("CompassSystem");
        compassObj.transform.SetParent(parentRoot.transform);
        CompassSystem compass = compassObj.AddComponent<CompassSystem>();

        // 3. GameManagers
        GameObject managersObj = new GameObject("GameManagers");
        managersObj.transform.SetParent(parentRoot.transform);
        GameManager gm = managersObj.AddComponent<GameManager>();
        LevelManager lm = managersObj.AddComponent<LevelManager>();
        managersObj.AddComponent<GameManagerCheckpointHelper>();
        gm.player = pc;
        gm.levelManager = lm;

        // Canvas Setup (Automatically created by GameUIController on Awake)
        GameObject uiControllerObj = new GameObject("UIController");
        uiControllerObj.transform.SetParent(parentRoot.transform);
        GameUIController uic = uiControllerObj.AddComponent<GameUIController>();
        gm.uiController = uic;

        // Dynamic sound creator
        AudioSource victorySrc = managersObj.AddComponent<AudioSource>();
        victorySrc.playOnAwake = false;
        AudioClip victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Envirornment/Audio/Chest Creak.wav");
        if (victoryClip != null)
        {
            victorySrc.clip = victoryClip;
        }
        else
        {
            Debug.LogError("Velocity Quest: Chest Creak audio clip not found at Assets/Envirornment/Audio/Chest Creak.wav");
        }

        // 4. Create Checkpoint Objects
        Transform cpStart = CreateCheckpoint("StartCheckpoint", pStartWorld, parentRoot.transform, 1, new Vector3(0f, 0f, 6f), 0f);
        cpStart.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        Transform cpL1 = CreateCheckpoint("L1Checkpoint", GetTerrainPos(terrain, 0.2f * w, 0.32f * l), parentRoot.transform, 2, Vector3.zero, 0f);
        cpL1.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

        Transform cpL2 = CreateCheckpoint("L2Checkpoint", GetTerrainPos(terrain, 0.52f * w, 0.12f * l), parentRoot.transform, 3, Vector3.zero, 0f);
        cpL2.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        Transform cpCave = CreateCheckpoint("CaveCheckpoint", GetTerrainPos(terrain, 0.68f * w, 0.44f * l), parentRoot.transform, 4, Vector3.zero, 480f);
        cpCave.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        Transform cpL3 = CreateCheckpoint("L3Checkpoint", GetTerrainPos(terrain, 0.68f * w, 0.46f * l), parentRoot.transform, 5, Vector3.zero, 0f);
        cpL3.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

        Transform cpBridge = CreateCheckpoint("BridgeCheckpoint", GetTerrainPos(terrain, 0.24f * w, 0.44f * l), parentRoot.transform, 6, Vector3.zero, 460f);
        cpBridge.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

        Transform cpL4 = CreateCheckpoint("L4Checkpoint", GetTerrainPos(terrain, 0.22f * w, 0.6f * l), parentRoot.transform, 7, Vector3.zero, 0f);
        cpL4.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        gm.activeCheckpoint = cpStart;

        // 5. Create Treasure Chests
        lm.level1Chest = CreateChest("Chest_L1", 1, GetTerrainPos(terrain, 0.2f * w, 0.32f * l), parentRoot.transform, victorySrc);
        lm.level2Chest = CreateChest("Chest_L2", 2, GetTerrainPos(terrain, 0.52f * w, 0.12f * l), parentRoot.transform, victorySrc);
        lm.level3Chest = CreateChest("Chest_L3", 3, GetTerrainPos(terrain, 0.68f * w, 0.46f * l), parentRoot.transform, victorySrc);
        lm.level4Chest = CreateChest("Chest_L4", 4, GetTerrainPos(terrain, 0.22f * w, 0.6f * l), parentRoot.transform, victorySrc);
        lm.level5Chest = CreateChest("Chest_L5", 5, GetTerrainPos(terrain, 0.5f * w, 0.85f * l) + new Vector3(0f, 0f, -1.5f), parentRoot.transform, victorySrc);

        // 6. Level 2 Collectibles
        lm.woodCollectible = CreateCollectible("WoodCollectible", CollectibleItem.CollectibleType.Wood, GetTerrainPos(terrain, 0.4f * w, 0.32f * l), parentRoot.transform);
        lm.stoneCollectible = CreateCollectible("StoneCollectible", CollectibleItem.CollectibleType.FireStone, GetTerrainPos(terrain, 0.4f * w, 0.12f * l), parentRoot.transform);

        GameObject campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Envirornment/new_years_fire.glb");
        GameObject camp;
        if (campfirePrefab != null)
        {
            camp = Instantiate(campfirePrefab);
            camp.name = "CampfireSite";
            camp.transform.position = GetTerrainPos(terrain, 0.46f * w, 0.12f * l);
            camp.transform.localScale = Vector3.one;
            camp.transform.SetParent(parentRoot.transform);
            
            // Add BoxCollider for physics collision
            BoxCollider campCollider = camp.AddComponent<BoxCollider>();
            campCollider.size = new Vector3(2f, 1.5f, 2f);
            campCollider.center = new Vector3(0f, 0.75f, 0f);
        }
        else
        {
            camp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            camp.name = "CampfireSite";
            camp.transform.position = GetTerrainPos(terrain, 0.46f * w, 0.12f * l);
            camp.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
            camp.transform.SetParent(parentRoot.transform);
        }

        // Dynamically add a custom rising fire particle system
        GameObject firePsObj = new GameObject("CampfireParticles");
        firePsObj.transform.SetParent(camp.transform, false);
        firePsObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        
        ParticleSystem firePs = firePsObj.AddComponent<ParticleSystem>();
        var fireMain = firePs.main;
        fireMain.playOnAwake = false; // MUST be false initially!
        fireMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0f, 0.8f), new Color(1f, 0.9f, 0f, 0.8f));
        fireMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        fireMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        fireMain.loop = true;
        fireMain.gravityModifier = -0.1f;
        
        var fireEmission = firePs.emission;
        fireEmission.rateOverTime = 60f;
        
        var fireShape = firePs.shape;
        fireShape.shapeType = ParticleSystemShapeType.Cone;
        fireShape.radius = 0.4f;
        fireShape.angle = 15f;
        
        var fireColorOverLifetime = firePs.colorOverLifetime;
        fireColorOverLifetime.enabled = true;
        Gradient fireGrad = new Gradient();
        fireGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.yellow, 0.6f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        fireColorOverLifetime.color = new ParticleSystem.MinMaxGradient(fireGrad);
        
        var fireSizeOverLifetime = firePs.sizeOverLifetime;
        fireSizeOverLifetime.enabled = true;
        AnimationCurve fireSizeCurve = new AnimationCurve();
        fireSizeCurve.AddKey(0f, 0.1f);
        fireSizeCurve.AddKey(0.2f, 1f);
        fireSizeCurve.AddKey(1f, 0f);
        fireSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, fireSizeCurve);

        lm.campfireSite = camp;

        // 7. Level 3 key & cave block
        GameObject keyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        keyObj.name = "AncientKey";
        keyObj.transform.position = GetTerrainPos(terrain, 0.68f * w, 0.44f * l) + Vector3.up * 0.5f;
        keyObj.transform.localScale = new Vector3(0.5f, 0.2f, 0.8f);
        keyObj.transform.SetParent(parentRoot.transform);
        keyObj.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
        keyObj.GetComponent<Collider>().isTrigger = true;
        keyObj.AddComponent<Level3Key>();

        GameObject caveWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caveWall.name = "CaveEntranceBlocker";
        caveWall.transform.position = GetTerrainPos(terrain, 0.68f * w, 0.45f * l);
        caveWall.transform.localScale = new Vector3(8f, 6f, 2f);
        caveWall.transform.SetParent(parentRoot.transform);
        caveWall.GetComponent<Renderer>().sharedMaterial.color = Color.gray;
        lm.caveEntranceObstacle = caveWall;

        // 8. Level 4 Wolf Setup
        GameObject wolfSpawnObj = new GameObject("WolfSpawner");
        wolfSpawnObj.transform.position = GetTerrainPos(terrain, 0.5f * w, 0.44f * l);
        wolfSpawnObj.transform.SetParent(parentRoot.transform);
        lm.wolfSpawner = wolfSpawnObj.transform;

        GameObject wolfPrefabSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Envirornment/Ultimate Animated Animals - July 2021-20260624T160444Z-3-001/Ultimate Animated Animals - July 2021/FBX/Wolf.fbx");
        GameObject wolfP;
        if (wolfPrefabSource != null)
        {
            wolfP = Instantiate(wolfPrefabSource);
            wolfP.name = "WolfPrefab";
            wolfP.transform.localScale = Vector3.one;
            wolfP.transform.SetParent(parentRoot.transform);
            
            // Add WolfAI
            wolfP.AddComponent<WolfAI>();
            
            // Add CapsuleCollider
            CapsuleCollider wolfCollider = wolfP.AddComponent<CapsuleCollider>();
            wolfCollider.center = new Vector3(0f, 0.5f, 0f);
            wolfCollider.radius = 0.4f;
            wolfCollider.height = 1.0f;
            wolfCollider.direction = 2; // Z-axis forward
            
            // Setup Animator Controller
            Animator anim = wolfP.GetComponent<Animator>();
            if (anim == null) anim = wolfP.AddComponent<Animator>();
            
            var wolfController = CreateOrGetAnimatorController("Wolf", "Assets/Envirornment/Ultimate Animated Animals - July 2021-20260624T160444Z-3-001/Ultimate Animated Animals - July 2021/FBX/Wolf.fbx",
                new string[] { "Idle", "Walk", "Run" },
                new string[] { "idle", "walk", "run" });
            anim.runtimeAnimatorController = wolfController;

            ConvertMaterialsToURP(wolfP, "WolfMaterials", Color.gray);
        }
        else
        {
            wolfP = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wolfP.name = "WolfPrefab_Placeholder";
            wolfP.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            wolfP.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            wolfP.AddComponent<WolfAI>();
            wolfP.transform.SetParent(parentRoot.transform);
        }
        lm.wolfPrefab = wolfP;
        wolfP.SetActive(false); // Hide template

        // 9. Level 5 Temple
        string templePath = "Assets/Envirornment/roman-temple/source/temple/roman_house_oltar_cups.fbx";
        ModelImporter templeImporter = AssetImporter.GetAtPath(templePath) as ModelImporter;
        if (templeImporter != null)
        {
            if (templeImporter.importNormals != ModelImporterNormals.Calculate || 
                templeImporter.importTangents != ModelImporterTangents.CalculateMikk)
            {
                templeImporter.importNormals = ModelImporterNormals.Calculate;
                templeImporter.importTangents = ModelImporterTangents.CalculateMikk;
                templeImporter.SaveAndReimport();
            }
        }
        GameObject templePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(templePath);
        GameObject temple;
        if (templePrefab != null)
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsSet = false;
            foreach (var r in templePrefab.GetComponentsInChildren<Renderer>())
            {
                if (!boundsSet) { b = r.bounds; boundsSet = true; }
                else b.Encapsulate(r.bounds);
            }
            Debug.Log($"[TEMPLE BOUNDS] Prefab size: {b.size}, bounds: {b}");

            temple = Instantiate(templePrefab);
            temple.name = "FinalTemple";
            temple.transform.position = GetTerrainPos(terrain, 0.5f * w, 0.85f * l);
            temple.transform.localScale = new Vector3(2f, 2f, 2f);
            temple.transform.SetParent(parentRoot.transform);
            
            // Add MeshCollider components to all child meshes
            MeshFilter[] mfs = temple.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh != null)
                {
                    GameObject childGo = mf.gameObject;
                    if (childGo.GetComponent<Collider>() == null)
                    {
                        childGo.AddComponent<MeshCollider>();
                    }
                }
            }
            
            ConvertMaterialsToURP(temple, "TempleMaterials", new Color(0.7f, 0.6f, 0.4f));
        }
        else
        {
            temple = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temple.name = "FinalTemple";
            temple.transform.position = GetTerrainPos(terrain, 0.5f * w, 0.85f * l);
            temple.transform.localScale = new Vector3(15f, 10f, 15f);
            temple.transform.SetParent(parentRoot.transform);
            temple.GetComponent<Renderer>().sharedMaterial.color = new Color(0.7f, 0.6f, 0.4f);
        }
        lm.finalTemple = temple;

        // Falling Tree Trigger
        GameObject treeTrigger = new GameObject("FallingTreeTriggerBox");
        treeTrigger.transform.position = GetTerrainPos(terrain, 0.38f * w, 0.76f * l);
        treeTrigger.transform.SetParent(parentRoot.transform);
        BoxCollider boxTrig = treeTrigger.AddComponent<BoxCollider>();
        boxTrig.isTrigger = true;
        boxTrig.size = new Vector3(8f, 5f, 4f);
        FallingTreeTrigger ftt = treeTrigger.AddComponent<FallingTreeTrigger>();

        GameObject treeModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        treeModel.name = "StormTreeTrunk";
        treeModel.transform.position = GetTerrainPos(terrain, 0.38f * w + 4f, 0.76f * l);
        treeModel.transform.localScale = new Vector3(1f, 8f, 1f);
        treeModel.transform.SetParent(parentRoot.transform);
        treeModel.GetComponent<Renderer>().sharedMaterial.color = new Color(0.4f, 0.25f, 0.1f);
        ftt.treeTransform = treeModel.transform;

        // Storm rain system
        GameObject rainObj = new GameObject("StormRainSystem");
        rainObj.transform.SetParent(parentRoot.transform);
        rainObj.transform.position = pStartWorld;
        ParticleSystem rainPs = rainObj.AddComponent<ParticleSystem>();
        var main = rainPs.main;
        main.startColor = new Color(0.6f, 0.7f, 0.8f, 0.4f);
        main.startSpeed = 25f;
        main.startSize = 0.2f;
        var emission = rainPs.emission;
        emission.rateOverTime = 500f;
        var shape = rainPs.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(40f, 1f, 40f);
        lm.stormRainSystem = rainPs;

        // 10. Create Level Intersections (JunctionControllers) - REMOVED blocker junctions as requested by user.

        // Spawn 100m milestone checkpoints for all 5 levels
        SpawnCheckpointsForLevel(1, new List<Vector2>(GameManager.LevelWaypoints[0]), 20f, parentRoot.transform);
        SpawnCheckpointsForLevel(2, new List<Vector2>(GameManager.LevelWaypoints[1]), 33.33f, parentRoot.transform);
        SpawnCheckpointsForLevel(3, new List<Vector2>(GameManager.LevelWaypoints[2]), 50f, parentRoot.transform);
        SpawnCheckpointsForLevel(4, new List<Vector2>(GameManager.LevelWaypoints[3]), 48f, parentRoot.transform);
        SpawnCheckpointsForLevel(5, new List<Vector2>(GameManager.LevelWaypoints[4]), 50f, parentRoot.transform);

        // 11. Populate Level Configurations inside LevelManager
        SetupLevelManagerLevels(lm);

        // Turn off lighting source on directional light if found
        Light light = FindFirstObjectByType<Light>();
        if (light != null)
        {
            lm.directionalLight = light;
        }

        // 12. Spawning Ambient Animals
        SpawnAmbientWildlife(terrain, parentRoot.transform, paths);
    }

    private static Vector3 GetTerrainPos(Terrain terrain, float x, float z)
    {
        float y = terrain.SampleHeight(new Vector3(x, 0f, z));
        return new Vector3(x, y + terrain.transform.position.y, z);
    }

    private static Transform CreateCheckpoint(string name, Vector3 pos, Transform parent, int checkpointIndex, Vector3 triggerOffset = default, float distanceCovered = 0f)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos + Vector3.up * 0.2f;
        go.transform.SetParent(parent);

        var cpDist = go.AddComponent<CheckpointDistance>();
        cpDist.distanceCovered = distanceCovered;

        GameObject triggerGo = new GameObject(name + "_Trigger");
        triggerGo.transform.SetParent(go.transform, false);
        triggerGo.transform.position = go.transform.position + triggerOffset;

        SphereCollider sc = triggerGo.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 4f;

        CheckpointTrigger trigger = triggerGo.AddComponent<CheckpointTrigger>();
        trigger.checkpointIndex = checkpointIndex;

        return go.transform;
    }

    private static GameObject CreateChest(string name, int index, Vector3 pos, Transform parent, AudioSource audio)
    {
        GameObject chest = new GameObject(name);
        chest.transform.position = pos;
        chest.transform.SetParent(parent);

        string chestPath = "Assets/wooden_treasures_box.glb";
        Vector3 chestScale = new Vector3(25.0f, 25.0f, 25.0f);

        switch (index)
        {
            case 1:
                chestPath = "Assets/Envirornment/treasure-box/source/SnakeTreasureBox.fbx";
                chestScale = new Vector3(25.0f, 25.0f, 25.0f);
                break;
            case 2:
                chestPath = "Assets/Envirornment/metal_dragon_chinese_trinket_box_low_poly.glb";
                chestScale = new Vector3(25.0f, 25.0f, 25.0f);
                break;
            case 3:
                chestPath = "Assets/Envirornment/stylized-medieval-chest/source/Meshy_AI_Medieval_fantasy_trea_0604223036_texture_fbx/Meshy_AI_Medieval_fantasy_trea_0604223036_texture_fbx/Meshy_AI_Medieval_fantasy_trea_0604223036_texture.fbx";
                chestScale = new Vector3(25.0f, 25.0f, 25.0f);
                break;
            case 4:
                chestPath = "Assets/Envirornment/treasure-chest-scan/source/Box.glb";
                chestScale = new Vector3(25.0f, 25.0f, 25.0f);
                break;
            case 5:
                chestPath = "Assets/Envirornment/treasure-chest/source/finlowlowlow.fbx";
                chestScale = new Vector3(25.0f, 25.0f, 25.0f);
                break;
        }

        EnsureReadWriteEnabled(chestPath);
        AssetDatabase.ImportAsset(chestPath);
        GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(chestPath);
        Transform lidTransform = null;

        if (chestPrefab != null)
        {
            GameObject chestInstance = Instantiate(chestPrefab);
            chestInstance.name = name + "_Model";
            chestInstance.transform.SetParent(chest.transform, false);
            chestInstance.transform.localPosition = Vector3.zero;
            chestInstance.transform.localRotation = Quaternion.identity;
            chestInstance.transform.localScale = chestScale;

            // Convert built-in/standard materials to URP Lit to prevent the magenta rendering issue
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader != null)
            {
                // Ensure directory for generated materials exists
                string matDir = "Assets/Materials/ChestMaterials";
                if (!AssetDatabase.IsValidFolder(matDir))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Materials");
                    }
                    AssetDatabase.CreateFolder("Assets/Materials", "ChestMaterials");
                }

                Renderer[] rends = chestInstance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    Material[] sharedMats = r.sharedMaterials;
                    for (int m = 0; m < sharedMats.Length; m++)
                    {
                        if (sharedMats[m] != null)
                        {
                            Material oldMat = sharedMats[m];
                            string matPath = $"{matDir}/{oldMat.name}_URP.mat";
                            Material newMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (newMat == null)
                            {
                                newMat = new Material(urpLitShader);
                                newMat.name = oldMat.name + "_URP";
                                if (oldMat.HasProperty("_Color"))
                                {
                                    newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                                }
                                else
                                {
                                    newMat.SetColor("_BaseColor", new Color(0.45f, 0.28f, 0.15f)); // Default wood brown
                                }
                                if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null)
                                {
                                    newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                                }
                                BindTexturesToChestMaterial(newMat, oldMat.name, chestPath);
                                AssetDatabase.CreateAsset(newMat, matPath);
                            }
                            else
                            {
                                // Update settings in case they changed
                                if (oldMat.HasProperty("_Color"))
                                {
                                    newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                                }
                                if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null)
                                {
                                    newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                                }
                                BindTexturesToChestMaterial(newMat, oldMat.name, chestPath);
                                EditorUtility.SetDirty(newMat);
                            }
                            sharedMats[m] = newMat;
                        }
                    }
                    r.sharedMaterials = sharedMats;
                }
            }

            foreach (Transform child in chestInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("lid") || child.name.ToLower().Contains("top") || child.name.ToLower().Contains("cover"))
                {
                    lidTransform = child;
                    break;
                }
            }
        }

        if (chestPrefab == null)
        {
            // Fallback base box
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "ChestBase";
            baseBox.transform.SetParent(chest.transform, false);
            baseBox.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            baseBox.transform.localScale = new Vector3(1.5f, 0.8f, 1f);
            baseBox.GetComponent<Renderer>().sharedMaterial.color = new Color(0.4f, 0.2f, 0.1f);

            // Fallback lid box
            GameObject lidBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidBox.name = "ChestLid";
            lidBox.transform.SetParent(chest.transform, false);
            lidBox.transform.localPosition = new Vector3(0f, 0.8f, -0.5f);
            lidBox.transform.localScale = new Vector3(1.5f, 0.2f, 1f);
            lidBox.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.15f, 0.05f);
            lidTransform = lidBox.transform;
        }

        // Trigger Collider
        BoxCollider box = chest.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 7.5f, 0f);
        box.size = new Vector3(40f, 15f, 40f);

        TreasureChest tc = chest.AddComponent<TreasureChest>();
        tc.levelChestIndex = index;
        tc.chestLid = lidTransform;
        tc.victoryAudio = audio;

        // Glow light
        GameObject lightObj = new GameObject("GlowLight");
        lightObj.transform.SetParent(chest.transform);
        lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.yellow;
        light.range = 5f;
        light.intensity = 2f;

        // Glow particles
        GameObject psObj = new GameObject("GlowParticles");
        psObj.transform.SetParent(chest.transform);
        psObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = Color.yellow;
        main.startSpeed = 1f;
        main.startLifetime = 1.5f;
        main.startSize = 0.3f;
        var emission = ps.emission;
        emission.rateOverTime = 20f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.2f, 0.2f, 0.8f);
        tc.goldenGlowParticles = ps;

        // Explosion particles
        GameObject psExp = new GameObject("ExplosionParticles");
        psExp.transform.SetParent(chest.transform);
        psExp.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        ParticleSystem exp = psExp.AddComponent<ParticleSystem>();
        var expMain = exp.main;
        expMain.startColor = Color.yellow;
        expMain.startSpeed = 8f;
        expMain.startSize = 0.4f;
        expMain.loop = false;
        var expEmit = exp.emission;
        expEmit.rateOverTime = 0f;
        expEmit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 150) });
        tc.openExplosionParticles = exp;

        // 11. Instantiate pop-up rewards inside the chest (hidden by default)
        string[] rewardPaths = new string[0];
        Vector3[] rewardScales = new Vector3[0];
        Vector3[] rewardRotations = new Vector3[0];

        switch (index)
        {
            case 1:
                rewardPaths = new string[] { "Assets/compass.glb" };
                rewardScales = new Vector3[] { new Vector3(0.02f, 0.02f, 0.02f) };
                rewardRotations = new Vector3[] { new Vector3(0f, 0f, 0f) };
                break;
            case 2:
                rewardPaths = new string[] { "Assets/Envirornment/old-key-made-from-copper-and-gold/source/Key2_Low.fbx" };
                rewardScales = new Vector3[] { new Vector3(0.1f, 0.1f, 0.1f) };
                rewardRotations = new Vector3[] { new Vector3(90f, 0f, 0f) };
                break;
            case 3:
                rewardPaths = new string[] { "Assets/Envirornment/annalakshmi_-_one_who_bestows_grain.glb" };
                rewardScales = new Vector3[] { new Vector3(0.0015f, 0.0015f, 0.0015f) };
                rewardRotations = new Vector3[] { new Vector3(0f, 0f, 0f) };
                break;
            case 4:
                rewardPaths = new string[] { "Assets/Envirornment/maa-durga-statue/source/Untitled.glb" };
                rewardScales = new Vector3[] { new Vector3(0.3f, 0.3f, 0.3f) };
                rewardRotations = new Vector3[] { new Vector3(0f, 0f, 0f) };
                break;
            case 5:
                rewardPaths = new string[] { 
                    "Assets/Envirornment/realistic-gold-bar/source/GoldBar.fbx",
                    "Assets/Envirornment/gold-coins-material/source/MaterialSphere01.fbx"
                };
                rewardScales = new Vector3[] { 
                    new Vector3(0.08f, 0.08f, 0.08f),
                    new Vector3(0.2f, 0.2f, 0.2f)
                };
                rewardRotations = new Vector3[] { 
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, 0f, 0f)
                };
                break;
        }

        List<GameObject> rewardInstances = new List<GameObject>();
        Shader rewardShader = Shader.Find("Universal Render Pipeline/Lit");

        for (int rIndex = 0; rIndex < rewardPaths.Length; rIndex++)
        {
            string rPath = rewardPaths[rIndex];
            EnsureReadWriteEnabled(rPath);
            AssetDatabase.ImportAsset(rPath);
            GameObject rPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rPath);
            if (rPrefab != null)
            {
                GameObject rInstance = Instantiate(rPrefab);
                rInstance.name = name + "_Reward_" + rIndex;
                rInstance.transform.SetParent(chest.transform, false);
                
                float yOffset = 0.2f;
                Vector3 localPos = new Vector3(0f, yOffset, 0f);
                if (index == 5)
                {
                    if (rIndex == 0) localPos = new Vector3(-0.25f, yOffset, 0f);
                    else localPos = new Vector3(0.25f, yOffset, 0f);
                }
                
                rInstance.transform.localPosition = localPos;
                rInstance.transform.localRotation = Quaternion.Euler(rewardRotations[rIndex]);
                rInstance.transform.localScale = rewardScales[rIndex];

                if (rewardShader != null)
                {
                    Renderer[] rRends = rInstance.GetComponentsInChildren<Renderer>(true);
                    foreach (var rend in rRends)
                    {
                        Material[] sharedMats = rend.sharedMaterials;
                        for (int m = 0; m < sharedMats.Length; m++)
                        {
                            if (sharedMats[m] != null)
                            {
                                Material oldMat = sharedMats[m];
                                string matDir = "Assets/Materials/RewardMaterials";
                                if (!AssetDatabase.IsValidFolder(matDir))
                                {
                                    if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                                    {
                                        AssetDatabase.CreateFolder("Assets", "Materials");
                                    }
                                    AssetDatabase.CreateFolder("Assets/Materials", "RewardMaterials");
                                }
                                string matPath = $"{matDir}/{oldMat.name}_URP.mat";
                                Material newMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                                if (newMat == null)
                                {
                                    newMat = new Material(rewardShader);
                                    newMat.name = oldMat.name + "_URP";
                                    if (oldMat.HasProperty("_Color")) newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                                    if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null) newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                                    AssetDatabase.CreateAsset(newMat, matPath);
                                }
                                sharedMats[m] = newMat;
                            }
                        }
                        rend.sharedMaterials = sharedMats;
                    }
                }

                rInstance.SetActive(false);
                rewardInstances.Add(rInstance);
            }
        }
        tc.rewardItems = rewardInstances.ToArray();

        return chest;
     }

    private static void BindTexturesToChestMaterial(Material mat, string matName, string chestPath)
    {
        string modelDir = System.IO.Path.GetDirectoryName(chestPath);
        string parentDir = System.IO.Path.GetDirectoryName(modelDir); // root of the asset folder

        Texture2D baseMap = FindTextureInDirectories(new string[] { modelDir, parentDir }, matName, "BaseColor", "Albedo", "Diffuse");
        Texture2D bumpMap = FindTextureInDirectories(new string[] { modelDir, parentDir }, matName, "Normal", "Bump");
        Texture2D emissiveMap = FindTextureInDirectories(new string[] { modelDir, parentDir }, matName, "Emission", "Emissive");
        Texture2D metallicMap = FindTextureInDirectories(new string[] { modelDir, parentDir }, matName, "Metallic", "Metalness");
        Texture2D roughnessMap = FindTextureInDirectories(new string[] { modelDir, parentDir }, matName, "Roughness", "Smoothness");

        if (baseMap != null)
        {
            mat.SetTexture("_BaseMap", baseMap);
        }
        if (bumpMap != null)
        {
            mat.SetTexture("_BumpMap", bumpMap);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (emissiveMap != null)
        {
            mat.SetTexture("_EmissionMap", emissiveMap);
            mat.SetColor("_EmissionColor", Color.white);
            mat.EnableKeyword("_EMISSION");
        }
        if (metallicMap != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallicMap);
            mat.SetFloat("_Metallic", 1.0f);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        if (roughnessMap != null)
        {
            mat.SetFloat("_Smoothness", 0.5f);
        }
    }

    private static Texture2D FindTextureInDirectories(string[] dirs, string matName, params string[] keywords)
    {
        foreach (var dir in dirs)
        {
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) continue;
            string[] files = System.IO.Directory.GetFiles(dir, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".tga" || ext == ".jpeg")
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file).ToLower();
                    
                    bool matchesMat = string.IsNullOrEmpty(matName) || name.Contains(matName.ToLower().Replace(" ", ""));
                    if (matName.ToLower().Contains("material") || matName.ToLower().Contains("lambert") || matName.ToLower().Contains("blinn"))
                    {
                        matchesMat = true; // generic material name fallback
                    }

                    if (matchesMat)
                    {
                        foreach (var kw in keywords)
                        {
                            if (name.Contains(kw.ToLower()))
                            {
                                string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                                if (tex != null) return tex;
                            }
                        }
                    }
                }
            }
        }
        
        // Exact name match fallback
        foreach (var dir in dirs)
        {
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) continue;
            string[] files = System.IO.Directory.GetFiles(dir, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".tga" || ext == ".jpeg")
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file).ToLower();
                    if (!string.IsNullOrEmpty(matName) && name.Equals(matName.ToLower().Replace(" ", ""), System.StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                        if (tex != null) return tex;
                    }
                }
            }
        }
        
        return null;
    }

    private static GameObject CreateCollectible(string name, CollectibleItem.CollectibleType type, Vector3 pos, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = pos + Vector3.up * 0.3f;
        go.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
        go.transform.SetParent(parent);
        go.GetComponent<Collider>().isTrigger = true;

        CollectibleItem item = go.AddComponent<CollectibleItem>();
        item.type = type;

        Renderer r = go.GetComponent<Renderer>();
        if (type == CollectibleItem.CollectibleType.Wood) r.sharedMaterial.color = new Color(0.5f, 0.35f, 0.2f);
        else r.sharedMaterial.color = Color.red; // fire stone

        return go;
    }

    private static void CreateSignCanvas(GameObject parentObj, string text, Vector3 worldPos, Quaternion worldRot)
    {
        GameObject canvasObj = new GameObject("SignCanvas");
        
        // Add Canvas first to ensure it has a RectTransform before setting parent and position
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        // Set parent first
        canvasObj.transform.SetParent(parentObj.transform, false);

        // Set position and rotation in world space (safe now since it's already a RectTransform)
        canvasObj.transform.position = worldPos;
        canvasObj.transform.rotation = worldRot;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300f, 150f);
        canvasRect.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(canvasObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 28;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        try
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmpText.font = TMP_Settings.defaultFontAsset;
            }
        }
        catch (System.Exception) { }
    }

    private static void CreateJunction(string name, Vector3 pos, string reqDir, float reqSpeed, float reqDistance, Transform successCP, Transform parent, bool isGuidepost = false)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.SetParent(parent);

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(40f, 5f, 15f);

        JunctionController jc = go.AddComponent<JunctionController>();
        jc.requiredDirection = reqDir;
        jc.requiredSpeed = reqSpeed;
        jc.successCheckpoint = successCP;
        jc.isGuidepostOnly = isGuidepost;
        jc.distanceMilestone = reqDistance;

        // Visual helper in editor (semi transparent green block at 5x scale)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "JunctionVisualHelper";
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        visual.transform.localScale = new Vector3(40f, 0.2f, 15f);
        visual.GetComponent<Collider>().enabled = false;



        // Load and instantiate physical 3D wooden sign board
        GameObject signBoardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/low-poly-sign-board-stylized-wooden-sign/source/sign bord.fbx");
        if (signBoardPrefab != null)
        {
            GameObject signBoard = Instantiate(signBoardPrefab);
            signBoard.name = name + "_SignBoard";

            // Calculate side offset (3.5m to the side of the road) and face direction
            Vector3 offset = Vector3.zero;
            Vector3 faceDir = Vector3.back;

            if (reqDir.Equals("North", System.StringComparison.OrdinalIgnoreCase))
            {
                offset = new Vector3(-3.5f, 0f, -1f);
                faceDir = Vector3.forward; // Look North (front of sign faces South towards approaching player)
            }
            else if (reqDir.Equals("South", System.StringComparison.OrdinalIgnoreCase))
            {
                offset = new Vector3(3.5f, 0f, 1f);
                faceDir = Vector3.back; // Look South (front of sign faces North towards approaching player)
            }
            else if (reqDir.Equals("East", System.StringComparison.OrdinalIgnoreCase))
            {
                offset = new Vector3(1f, 0f, 3.5f);
                faceDir = Vector3.right; // Look East (front of sign faces West towards approaching player)
            }
            else if (reqDir.Equals("West", System.StringComparison.OrdinalIgnoreCase))
            {
                offset = new Vector3(-1f, 0f, -3.5f);
                faceDir = Vector3.left; // Look West (front of sign faces East towards approaching player)
            }

            // Find terrain and get height
            Terrain terrain = parent.GetComponentInParent<Terrain>();
            if (terrain == null) terrain = FindFirstObjectByType<Terrain>();
            Vector3 signWorldPos = GetTerrainPos(terrain, pos.x + offset.x, pos.z + offset.z);

            signBoard.transform.position = signWorldPos;
            signBoard.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
            signBoard.transform.SetParent(go.transform);

            // Add BoxCollider so the sign board is physical and doesn't allow clipping
            BoxCollider signCollider = signBoard.AddComponent<BoxCollider>();
            Vector3 localScale = signBoard.transform.localScale;
            signCollider.center = new Vector3(0f, 0.8f / localScale.y, 0f);
            signCollider.size = new Vector3(1.2f / localScale.x, 1.6f / localScale.y, 0.4f / localScale.z);

            // Create Canvas on both sides parented to the clean unrotated junction root (go)
            string text = isGuidepost ?
                $"GO {reqDir.ToUpper()}\nSpeed: {reqSpeed} m/s\nDistance Covered: {reqDistance} m" :
                $"GO {reqDir.ToUpper()}\nSpeed: {reqSpeed} m/s\nDistance: {reqDistance} m";
            
            // Center of the planks is at height 2.6m to center the text perfectly and prevent clipping.
            Vector3 boardCenter = signWorldPos + Vector3.up * 2.6f;

            // The thickness of the board is 0.46m (half-thickness is 0.23m).
            // Place canvases at 0.24m from the center to sit exactly on the wooden surface.
            Vector3 frontPos = boardCenter + faceDir * 0.24f;
            Quaternion frontRot = Quaternion.LookRotation(-faceDir, Vector3.up);
            CreateSignCanvas(go, text, frontPos, frontRot);

            Vector3 backPos = boardCenter - faceDir * 0.24f;
            Quaternion backRot = Quaternion.LookRotation(faceDir, Vector3.up);
            CreateSignCanvas(go, text, backPos, backRot);
        }
        else
        {
            Debug.LogWarning($"Velocity Quest: Sign board prefab not found at Assets/low-poly-sign-board-stylized-wooden-sign/source/sign bord.fbx");
        }
    }

    private static void SpawnCheckpointsForLevel(int levelIndex, List<Vector2> pathPoints, float speed, Transform parent)
    {
        if (pathPoints.Count < 2) return;
        
        float interval = 100f;
        float currentDistance = 0f;
        float targetDistance = interval;
        
        Vector2 currentPos = pathPoints[0];
        
        Terrain terrain = parent.GetComponentInParent<Terrain>();
        if (terrain == null) terrain = FindFirstObjectByType<Terrain>();
        
        for (int i = 1; i < pathPoints.Count; i++)
        {
            Vector2 nextPos = pathPoints[i];
            float segmentLength = Vector2.Distance(currentPos, nextPos);
            
            while (currentDistance + segmentLength >= targetDistance)
            {
                float t = (targetDistance - currentDistance) / segmentLength;
                Vector2 checkpointPos2D = Vector2.Lerp(currentPos, nextPos, t);
                
                Vector3 worldPos = GetTerrainPos(terrain, checkpointPos2D.x, checkpointPos2D.y);
                
                string reqDir = "North";
                Vector2 segDir = (nextPos - currentPos).normalized;
                if (Mathf.Abs(segDir.y) > Mathf.Abs(segDir.x))
                {
                    reqDir = segDir.y > 0 ? "North" : "South";
                }
                else
                {
                    reqDir = segDir.x > 0 ? "East" : "West";
                }
                
                // Check if this position is too close to an existing blocker junction
                bool tooClose = false;
                foreach (Transform child in parent)
                {
                    if (child.name.StartsWith("Junction_") && !child.name.Contains("_Checkpoint") && !child.name.Contains("m"))
                    {
                        if (Vector3.Distance(child.position, worldPos) < 10f)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }
                
                if (tooClose)
                {
                    targetDistance += interval;
                    continue;
                }

                string name = $"Junction_L{levelIndex}_{Mathf.RoundToInt(targetDistance)}m";
                
                GameObject cpObj = new GameObject(name + "_CheckpointCP");
                cpObj.transform.position = worldPos + Vector3.up * 0.2f;
                Vector3 roadDir3D = new Vector3(segDir.x, 0f, segDir.y).normalized;
                cpObj.transform.rotation = Quaternion.LookRotation(roadDir3D, Vector3.up);
                cpObj.transform.SetParent(parent);
                
                var cpDist = cpObj.AddComponent<CheckpointDistance>();
                cpDist.distanceCovered = targetDistance;
                
                CreateJunction(name, worldPos, reqDir, speed, targetDistance, cpObj.transform, parent, isGuidepost: true);
                
                targetDistance += interval;
            }
            
            currentDistance += segmentLength;
            currentPos = nextPos;
        }
    }

    private static void SetupLevelManagerLevels(LevelManager lm)
    {
        lm.levels = new LevelManager.LevelConfig[5];

        // Level 1
        lm.levels[0] = new LevelManager.LevelConfig
        {
            levelIndex = 1,
            levelName = "THE BEGINNER EXPLORER",
            environmentDescription = "Bright Forest, Sunny Atmosphere",
            fogDensity = 0.005f,
            fogColor = new Color(0.8f, 0.95f, 1f),
            lightColor = new Color(1f, 0.95f, 0.9f),
            lightIntensity = 1.3f,
            targetDirection = "North",
            targetSpeed = 20f,
            targetDistance = 200f,
            objectiveText = "Travel <color=#FFFF00><b>North</b></color> to find the chest"
        };

        // Level 2
        lm.levels[1] = new LevelManager.LevelConfig
        {
            levelIndex = 2,
            levelName = "SURVIVAL JOURNEY",
            environmentDescription = "Dense Forest, Reduced Visibility",
            fogDensity = 0.03f,
            fogColor = new Color(0.3f, 0.4f, 0.35f),
            lightColor = new Color(0.7f, 0.75f, 0.6f),
            lightIntensity = 0.8f,
            targetDirection = "East",
            targetSpeed = 33.33f,
            targetDistance = 500f,
            objectiveText = "Travel <color=#FFFF00><b>East</b></color> through the forest"
        };

        // Level 3
        lm.levels[2] = new LevelManager.LevelConfig
        {
            levelIndex = 3,
            levelName = "THE HIDDEN DEN",
            environmentDescription = "Fog, Caves, and Ancient Structures",
            fogDensity = 0.05f,
            fogColor = new Color(0.2f, 0.25f, 0.25f),
            lightColor = new Color(0.5f, 0.6f, 0.6f),
            lightIntensity = 0.5f,
            targetDirection = "East",
            targetSpeed = 50f,
            targetDistance = 1000f,
            objectiveText = "Travel <color=#FFFF00><b>East</b></color> toward the ruins"
        };

        // Level 4
        lm.levels[3] = new LevelManager.LevelConfig
        {
            levelIndex = 4,
            levelName = "DANGEROUS EXPEDITION",
            environmentDescription = "Mountain Trails, Wolves, and Rivers",
            fogDensity = 0.015f,
            fogColor = new Color(0.5f, 0.5f, 0.6f),
            lightColor = new Color(0.8f, 0.8f, 0.9f),
            lightIntensity = 1.0f,
            targetDirection = "South",
            targetSpeed = 48f,
            targetDistance = 1200f,
            objectiveText = "Travel <color=#FFFF00><b>South</b></color> through the jungle"
        };

        // Level 5
        lm.levels[4] = new LevelManager.LevelConfig
        {
            levelIndex = 5,
            levelName = "SANCTUARY OF LIGHT",
            environmentDescription = "Warm Sunlight and Clear Skies",
            fogDensity = 0.003f,
            fogColor = new Color(0.75f, 0.85f, 0.95f),
            lightColor = new Color(1.0f, 0.95f, 0.85f),
            lightIntensity = 1.8f,
            targetDirection = "North",
            targetSpeed = 50f,
            targetDistance = 1500f,
            objectiveText = "Travel <color=#FFFF00><b>North</b></color> to enter the temple"
        };
    }

    private static AnimationClip FindClipInFBX(string fbxPath, string keyword)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip fallback = null;
        AnimationClip keywordMatch = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                if (clip.name.Contains("__preview__")) continue;
                
                // Log for visibility
                Debug.Log($"Found clip: {clip.name} inside {fbxPath}");
                
                if (clip.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    keywordMatch = clip;
                    break; // Exact or keyword match found
                }
                
                if (fallback == null)
                {
                    fallback = clip;
                }
            }
        }
        
        return keywordMatch != null ? keywordMatch : fallback;
    }

    private static AnimatorController CreateOrGetAnimatorController(string animalName, string fbxPath, string[] stateNames, string[] keywords)
    {
        string controllerPath = $"Assets/Editor/{animalName}Animator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        // Always recreate or update to make sure clips are correctly set up
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }
        
        // Clear any existing states in the layer to rebuild cleanly
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ChildAnimatorState[] states = stateMachine.states;
        foreach (var state in states)
        {
            stateMachine.RemoveState(state.state);
        }
        
        // Add new states
        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];
            string keyword = keywords[i];
            
            AnimationClip clip = FindClipInFBX(fbxPath, keyword);
            if (clip != null)
            {
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;
                
                // Set default state if it's Idle
                if (stateName.Equals("Idle", System.StringComparison.OrdinalIgnoreCase))
                {
                    stateMachine.defaultState = state;
                }
            }
            else
            {
                Debug.LogWarning($"Could not find clip matching '{keyword}' for {animalName} in FBX {fbxPath}");
            }
        }
        
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ConvertMaterialsToURP(GameObject go, string folderName, Color? defaultColor = null)
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null) return;

        string matDir = $"Assets/Materials/{folderName}";
        if (!AssetDatabase.IsValidFolder(matDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateFolder("Assets/Materials", folderName);
        }

        Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r is ParticleSystemRenderer) continue;

            Material[] sharedMats = r.sharedMaterials;
            for (int m = 0; m < sharedMats.Length; m++)
            {
                if (sharedMats[m] != null)
                {
                    Material oldMat = sharedMats[m];
                    
                    if (oldMat.shader != null && oldMat.shader.name.Contains("Universal Render Pipeline/Lit"))
                    {
                        continue;
                    }

                    string cleanMatName = oldMat.name.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
                    string matPath = $"{matDir}/{cleanMatName}_URP.mat";
                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (newMat == null)
                    {
                        newMat = new Material(urpLitShader);
                        newMat.name = oldMat.name + "_URP";
                        
                        if (oldMat.HasProperty("_Color"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                        }
                        else if (defaultColor.HasValue)
                        {
                            newMat.SetColor("_BaseColor", defaultColor.Value);
                        }
                        
                        if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null)
                        {
                            newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                        }
                        else if (oldMat.HasProperty("_BaseMap") && oldMat.GetTexture("_BaseMap") != null)
                        {
                            newMat.SetTexture("_BaseMap", oldMat.GetTexture("_BaseMap"));
                        }

                        AssetDatabase.CreateAsset(newMat, matPath);
                    }
                    else
                    {
                        if (oldMat.HasProperty("_Color"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                        }
                        if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null)
                        {
                            newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                        }
                        EditorUtility.SetDirty(newMat);
                    }
                    sharedMats[m] = newMat;
                }
            }
            r.sharedMaterials = sharedMats;
        }
    }

    private static void SpawnAmbientWildlife(Terrain terrain, Transform parent, List<PathSegment> paths)
    {
        float terrW = terrain.terrainData.size.x;
        float terrL = terrain.terrainData.size.z;
        List<Vector2> keyPoints = GetGameplayKeyPoints(terrW, terrL);
        
        string[] animalNames = new string[] {
            "Alpaca", "Bull", "Cow", "Deer", "Donkey", "Fox", "Horse", "Horse_White", "Husky", "ShibaInu", "Stag"
        };
        
        string baseDir = "Assets/Envirornment/Ultimate Animated Animals - July 2021-20260624T160444Z-3-001/Ultimate Animated Animals - July 2021/FBX";
        
        // Root container for ambient animals
        GameObject animalsContainer = new GameObject("AmbientAnimalsContainer");
        animalsContainer.transform.SetParent(parent);
        
        Random.State oldState = Random.state;
        Random.InitState(888); // Set seed for consistent generation
        
        int targetCount = 70;
        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = 1500;
        
        while (spawnedCount < targetCount && attempts < maxAttempts)
        {
            attempts++;
            float normX = Random.value;
            float normZ = Random.value;
            
            // Keep animals slightly away from the absolute borders of the terrain
            if (normX < 0.05f || normX > 0.95f || normZ < 0.05f || normZ > 0.95f) continue;
            
            float worldX = normX * terrW;
            float worldZ = normZ * terrL;
            Vector2 pos2D = new Vector2(worldX, worldZ);
            
            // Check distance to roads - keep a safe distance of 6m so they don't spawn right on the road
            bool tooClose = false;
            foreach (var seg in paths)
            {
                if (seg.IsPointNear(pos2D, 6.0f))
                {
                    tooClose = true;
                    break;
                }
            }
            
            // Check distance to key gameplay points
            if (!tooClose)
            {
                foreach (var kp in keyPoints)
                {
                    if (Vector2.Distance(pos2D, kp) < 10f)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }
            
            if (!tooClose)
            {
                // Randomly select animal model
                string animalName = animalNames[Random.Range(0, animalNames.Length)];
                string fbxPath = $"{baseDir}/{animalName}.fbx";
                GameObject animalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                
                if (animalPrefab != null)
                {
                    try
                    {
                        Vector3 spawnPos = GetTerrainPos(terrain, worldX, worldZ);
                        GameObject animalInstance = Instantiate(animalPrefab);
                        animalInstance.name = $"Ambient_{animalName}_{spawnedCount}";
                        animalInstance.transform.position = spawnPos;
                        animalInstance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        animalInstance.transform.localScale = Vector3.one;
                        animalInstance.transform.SetParent(animalsContainer.transform);
                        
                        // Attach AmbientAnimal script
                        AmbientAnimal aa = animalInstance.AddComponent<AmbientAnimal>();
                        // Tweak speed slightly for variety around 5 m/s
                        aa.walkSpeed = Random.Range(4.5f, 5.5f);
                        aa.wanderRadius = Random.Range(20f, 40f);
                        
                        // Add CapsuleCollider
                        CapsuleCollider capCol = animalInstance.AddComponent<CapsuleCollider>();
                        capCol.center = new Vector3(0f, 0.4f, 0f);
                        capCol.radius = 0.35f;
                        capCol.height = 0.8f;
                        capCol.direction = 2; // Z-axis forward
                        
                        // Setup Animator Controller
                        Animator anim = animalInstance.GetComponent<Animator>();
                        if (anim == null) anim = animalInstance.AddComponent<Animator>();
                        
                        var controller = CreateOrGetAnimatorController(animalName, fbxPath,
                            new string[] { "Idle", "Walk", "Eat" },
                            new string[] { "idle", "walk", "eat" });
                        anim.runtimeAnimatorController = controller;
                        
                        // Convert materials to URP Lit
                        ConvertMaterialsToURP(animalInstance, $"{animalName}Materials", Color.gray);
                        
                        spawnedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Velocity Quest] Error spawning animal {animalName}: {ex}");
                    }
                }
                else
                {
                    Debug.LogError($"[Velocity Quest] Failed to load animal FBX from path: {fbxPath}");
                }
            }
        }
        
        Random.state = oldState;
        Debug.Log($"Velocity Quest: Successfully spawned {spawnedCount} ambient animals in the forest (out of {targetCount} requested).");
    }

    private static void EnsureReadWriteEnabled(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return;
        ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (modelImporter != null && !modelImporter.isReadable)
        {
            modelImporter.isReadable = true;
            modelImporter.SaveAndReimport();
        }
    }
}
