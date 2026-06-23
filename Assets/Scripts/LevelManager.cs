using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public struct LevelConfig
    {
        public int levelIndex;
        public string levelName;
        public string environmentDescription;
        public float fogDensity;
        public Color fogColor;
        public Color lightColor;
        public float lightIntensity;
        
        [Header("Compass Target Instructions")]
        public string targetDirection;
        public float targetSpeed; // in km/h
        public float targetDistance; // in meters
        public string objectiveText;
    }

    public LevelConfig[] levels;

    [Header("Environment References")]
    public Light directionalLight;
    public ParticleSystem stormRainSystem;
    public Transform wolfSpawner;
    public GameObject wolfPrefab;
    public GameObject campfireSite;
    public GameObject caveEntranceObstacle; // Hidden den cave wall blocker
    public GameObject bridgeJunction;
    public GameObject finalTemple;

    [Header("Collectibles & Chests")]
    public GameObject level1Chest;
    public GameObject level2Chest;
    public GameObject level3Chest;
    public GameObject level4Chest;
    public GameObject level5Chest;
    
    // Level 2 collectibles
    public GameObject woodCollectible;
    public GameObject stoneCollectible;

    private int activeLevelIndex = 1;
    private GameObject spawnedWolfInstance;

    private void Start()
    {
        SetupLevel(1);
    }

    public void SetupLevel(int levelIndex)
    {
        activeLevelIndex = levelIndex;
        if (levelIndex - 1 >= levels.Length) return;

        LevelConfig config = levels[levelIndex - 1];
        Debug.Log($"Setting up Level {levelIndex}: {config.levelName}");

        // Adjust lighting & fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = config.fogDensity;
        RenderSettings.fogColor = config.fogColor;

        if (directionalLight != null)
        {
            directionalLight.color = config.lightColor;
            directionalLight.intensity = config.lightIntensity;
        }

        // Toggle level-specific components
        if (stormRainSystem != null)
        {
            if (levelIndex == 5) stormRainSystem.Play();
            else stormRainSystem.Stop();
        }

        // Level 2: Enable wood and stone collectibles
        if (woodCollectible != null) woodCollectible.SetActive(levelIndex == 2);
        if (stoneCollectible != null) stoneCollectible.SetActive(levelIndex == 2);
        if (campfireSite != null) campfireSite.SetActive(levelIndex == 2);
        if (levelIndex == 2)
        {
            CollectibleItem.ResetCollectionState();
        }

        // Level 3: Secret Cave / Cave Entrance Obstacle active initially, disabled when key is found
        if (caveEntranceObstacle != null) caveEntranceObstacle.SetActive(levelIndex == 3);

        // Level 4: Spawn wolves
        if (levelIndex == 4 && wolfSpawner != null && wolfPrefab != null && spawnedWolfInstance == null)
        {
            spawnedWolfInstance = Instantiate(wolfPrefab, wolfSpawner.position, wolfSpawner.rotation);
        }
        else if (levelIndex != 4 && spawnedWolfInstance != null)
        {
            Destroy(spawnedWolfInstance);
        }

        // Level 5: Temple active
        if (finalTemple != null) finalTemple.SetActive(levelIndex == 5);

        // Activate corresponding treasure chests, deactivate others
        if (level1Chest != null) level1Chest.SetActive(levelIndex == 1);
        if (level2Chest != null) level2Chest.SetActive(false); // Only active after campfire is lit!
        if (level3Chest != null) level3Chest.SetActive(levelIndex == 3);
        if (level4Chest != null) level4Chest.SetActive(levelIndex == 4);
        if (level5Chest != null) level5Chest.SetActive(levelIndex == 5);

        // Update UI HUD
        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.SetTargetInstructions(config.targetDirection, config.targetSpeed, config.targetDistance, config.objectiveText);
        }
    }

    public LevelConfig GetCurrentLevelConfig()
    {
        if (activeLevelIndex - 1 < levels.Length)
        {
            return levels[activeLevelIndex - 1];
        }
        return new LevelConfig();
    }
}
