using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int score = 0;
    public int currentLevelIndex = 1;
    public Transform activeCheckpoint;

    [Header("References")]
    public PlayerController player;
    public GameUIController uiController;
    public LevelManager levelManager;

    [Header("Dark Environment Settings")]
    public bool isDarkEnvironment = false;
    private System.Collections.Generic.List<Light> disabledLights = new System.Collections.Generic.List<Light>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Ensure it is a root GameObject
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
        if (uiController == null)
        {
            uiController = FindFirstObjectByType<GameUIController>();
        }
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        // Save starting position as initial checkpoint if none set
        if (activeCheckpoint == null && player != null)
        {
            GameObject initCheckpoint = new GameObject("InitialCheckpoint");
            initCheckpoint.transform.position = player.transform.position;
            initCheckpoint.transform.rotation = player.transform.rotation;
            activeCheckpoint = initCheckpoint.transform;
        }

        AddScore(0); // Trigger initial UI update
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (uiController != null)
        {
            uiController.UpdateScoreUI(score);
        }
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        activeCheckpoint = newCheckpoint;
        AddScore(25); // +25 for checkpoint reached
        if (uiController != null)
        {
            uiController.ShowNotification("CHECKPOINT SAVED\nProgress Recorded");
        }
    }

    public void HandleWrongChoice(string reason)
    {
        AddScore(-20); // -20 for wrong choice (direction or speed)
        if (uiController != null)
        {
            uiController.ShowNotification($"Incorrect Velocity Selected: {reason}\nReturning To Checkpoint");
        }
        
        // Reset player to checkpoint
        if (player != null && activeCheckpoint != null)
        {
            player.ResetToPosition(activeCheckpoint.position, activeCheckpoint.rotation);
        }
    }

    public void CompleteLevel()
    {
        AddScore(500); // Level completion bonus
        
        SetDarkEnvironment(false); // Restore environment to normal on level completion
        
        if (currentLevelIndex < 5)
        {
            if (uiController != null)
            {
                uiController.ShowNotification($"LEVEL {currentLevelIndex} COMPLETE\nConcept Learned:\n" + GetConceptLearnedText(currentLevelIndex));
            }
            currentLevelIndex++;
            if (levelManager != null)
            {
                levelManager.SetupLevel(currentLevelIndex);
            }
        }
        else
        {
            // Final Level Complete
            AddScore(1000); // Final treasure bonus
            if (uiController != null)
            {
                uiController.ShowFinalSummary(score);
            }
        }
    }

    private string GetConceptLearnedText(int level)
    {
        switch (level)
        {
            case 1: return "Velocity Requires Direction";
            case 2: return "Speed Affects Travel Time";
            case 3: return "Changing Direction Changes Velocity";
            case 4: return "Efficient Velocity Reduces Travel Time";
            default: return "Velocity = Speed + Direction";
        }
    }

    private void Update()
    {
        if (isDarkEnvironment)
        {
            EnforceDarkEnvironment();
        }
    }

    public void SetDarkEnvironment(bool active)
    {
        isDarkEnvironment = active;
        if (active)
        {
            DisableEnvironmentLights();
            EnforceDarkEnvironment();
            if (player != null)
            {
                player.SetCompassHighlight(player.Is3DCompassActive());
            }
        }
        else
        {
            RestoreEnvironmentLights();
            if (levelManager != null)
            {
                levelManager.SetupLevel(currentLevelIndex);
            }
            if (player != null)
            {
                player.SetCompassHighlight(false);
            }
        }
    }

    private void DisableEnvironmentLights()
    {
        disabledLights.Clear();
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.gameObject.name == "CompassHighlightLight") continue;
            if (l.gameObject.activeSelf && l.enabled)
            {
                l.enabled = false;
                disabledLights.Add(l);
            }
        }
    }

    private void RestoreEnvironmentLights()
    {
        foreach (var l in disabledLights)
        {
            if (l != null)
            {
                l.enabled = true;
            }
        }
        disabledLights.Clear();
    }

    private void EnforceDarkEnvironment()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientSkyColor = Color.black;
        RenderSettings.ambientEquatorColor = Color.black;
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.15f;
        RenderSettings.skybox = null;

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }

        if (levelManager != null && levelManager.directionalLight != null)
        {
            levelManager.directionalLight.enabled = false;
        }

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.gameObject.name == "CompassHighlightLight") continue;
            if (l.enabled)
            {
                l.enabled = false;
                if (!disabledLights.Contains(l))
                {
                    disabledLights.Add(l);
                }
            }
        }
    }
}
