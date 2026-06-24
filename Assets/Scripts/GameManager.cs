using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Level Timer Settings")]
    public float levelTimeLimit = 25f;
    private float levelTimeRemaining = 0f;
    private bool isTimerRunning = false;

    [Header("Distance Tracker Settings")]
    [HideInInspector] public float levelDistanceTraveled = 0f;
    private Vector3 lastPlayerPosition;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = FindFirstObjectByType<PlayerController>();
        uiController = FindFirstObjectByType<GameUIController>();
        levelManager = FindFirstObjectByType<LevelManager>();

        if (currentLevelIndex == 1)
        {
            score = 0;
            if (uiController != null)
            {
                uiController.UpdateScoreUI(score);
            }
        }

        if (player != null)
        {
            GameObject initCheckpoint = new GameObject("InitialCheckpoint");
            initCheckpoint.transform.position = player.transform.position;
            initCheckpoint.transform.rotation = player.transform.rotation;
            activeCheckpoint = initCheckpoint.transform;
            
            var helper = GetComponent<GameManagerCheckpointHelper>();
            if (helper != null)
            {
                helper.lastClearedCheckpoint = activeCheckpoint;
            }
        }

        if (levelManager != null)
        {
            levelManager.SetupLevel(currentLevelIndex);
        }

        StopLevelTimer();
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

        if (activeCheckpoint == null && player != null)
        {
            GameObject initCheckpoint = new GameObject("InitialCheckpoint");
            initCheckpoint.transform.position = player.transform.position;
            initCheckpoint.transform.rotation = player.transform.rotation;
            activeCheckpoint = initCheckpoint.transform;
            
            var helper = GetComponent<GameManagerCheckpointHelper>();
            if (helper != null)
            {
                helper.lastClearedCheckpoint = activeCheckpoint;
            }
        }

        AddScore(0); // Trigger initial UI update
        StopLevelTimer();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            if (uiController != null && uiController.IsQuizActive)
            {
                return; // Pause timer during the quiz
            }

            levelTimeRemaining -= Time.deltaTime;

            if (uiController != null && uiController.hudTimerText != null)
            {
                uiController.hudTimerText.text = $"Time: {Mathf.Max(0f, levelTimeRemaining):F1}s";
                if (levelTimeRemaining <= 5f)
                {
                    uiController.hudTimerText.color = Color.red;
                }
                else
                {
                    uiController.hudTimerText.color = Color.white;
                }
            }

            if (levelTimeRemaining <= 0f)
            {
                isTimerRunning = false;
                OnLevelTimerExpired();
            }

            // Track cumulative distance traveled while gameplay timer is running
            if (player != null)
            {
                float deltaDist = Vector3.Distance(player.transform.position, lastPlayerPosition);
                // Ignore large teleports/resets
                if (deltaDist > 0f && deltaDist < 30f)
                {
                    // Only increase distance if moving in the correct direction
                    if (!player.isMovingInWrongDirection)
                    {
                        levelDistanceTraveled += deltaDist;
                    }
                }
                lastPlayerPosition = player.transform.position;
            }

            // Check if player has reached the chest or target distance
            if (player != null && levelManager != null)
            {
                bool reachedChest = false;
                GameObject activeChest = null;
                switch (currentLevelIndex)
                {
                    case 1: activeChest = levelManager.level1Chest; break;
                    case 2: activeChest = levelManager.level2Chest; break;
                    case 3: activeChest = levelManager.level3Chest; break;
                    case 4: activeChest = levelManager.level4Chest; break;
                    case 5: activeChest = levelManager.level5Chest; break;
                }

                if (activeChest != null && activeChest.activeInHierarchy)
                {
                    float distToChest = Vector3.Distance(player.transform.position, activeChest.transform.position);
                    if (distToChest < 25f)
                    {
                        reachedChest = true;
                    }
                }

                float targetDist = levelManager.GetCurrentLevelConfig().targetDistance;
                if (levelDistanceTraveled >= targetDist || reachedChest)
                {
                    isTimerRunning = false;
                    if (uiController != null && uiController.hudTimerText != null)
                    {
                        uiController.hudTimerText.text = "Goal Reached!";
                        uiController.hudTimerText.color = Color.green;
                    }
                    Debug.Log($"Goal reached! Timer paused. Distance: {levelDistanceTraveled}, ReachedChest: {reachedChest}");
                }
            }
        }
        else if (player != null)
        {
            // Keep position updated so when timer starts we don't calculate a huge initial jump
            lastPlayerPosition = player.transform.position;
        }
    }

    public void StartLevelTimer()
    {
        // Dynamic time limits per level:
        // L1 = 10s, L2 = 15s, L3 = 20s, L4 = 25s, L5 = 30s
        switch (currentLevelIndex)
        {
            case 1: levelTimeLimit = 30f; break;
            case 2: levelTimeLimit = 65f; break;
            case 3: levelTimeLimit = 125f; break;
            case 4: levelTimeLimit = 150f; break;
            case 5: levelTimeLimit = 190f; break;
            default: levelTimeLimit = 30f; break;
        }

        levelTimeRemaining = levelTimeLimit;
        isTimerRunning = true;

        // Reset distance traveled when the gameplay starts
        levelDistanceTraveled = 0f;
        if (player != null)
        {
            lastPlayerPosition = player.transform.position;
        }
    }

    public void StopLevelTimer()
    {
        isTimerRunning = false;
        levelDistanceTraveled = 0f;
        if (uiController != null && uiController.hudTimerText != null)
        {
            uiController.hudTimerText.text = "Time: --";
            uiController.hudTimerText.color = Color.white;
        }
    }

    private void OnLevelTimerExpired()
    {
        isTimerRunning = false;
        if (uiController != null)
        {
            uiController.StartTimerQuiz(currentLevelIndex);
        }
    }

    public void ReloadGameFromStart()
    {
        currentLevelIndex = 1;
        score = 0;
        isTimerRunning = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        StopLevelTimer();

        // Restore distance covered from checkpoint if available
        if (activeCheckpoint != null)
        {
            var cpDist = activeCheckpoint.GetComponent<CheckpointDistance>();
            if (cpDist != null)
            {
                levelDistanceTraveled = cpDist.distanceCovered;
            }
        }
    }

    public void CompleteLevel()
    {
        AddScore(500); // Level completion bonus
        isTimerRunning = false; // Stop timer
        
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
            StopLevelTimer(); // Next level starts in paused state until selection at junction
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

    public string GetConceptLearnedText(int level)
    {
        switch (level)
        {
            case 1: return "Velocity Requires Direction";
            case 2: return "Speed Affects Travel Time";
            case 3: return "Changing Direction Changes Velocity";
            case 4: return "Efficient Velocity Reduces Travel Time";
            case 5: return "Velocity and Speed Mastered";
            default: return "Velocity Quest Completed";
        }
    }

    public static readonly Vector2[][] LevelWaypoints = new Vector2[][]
    {
        // Level 1
        new Vector2[] { new Vector2(200f, 100f), new Vector2(200f, 320f) },
        
        // Level 2
        new Vector2[] { new Vector2(200f, 320f), new Vector2(400f, 320f), new Vector2(400f, 120f), new Vector2(520f, 120f) },
        
        // Level 3
        new Vector2[] { new Vector2(520f, 120f), new Vector2(520f, 280f), new Vector2(680f, 280f), new Vector2(680f, 460f) },
        
        // Level 4
        new Vector2[] { new Vector2(680f, 460f), new Vector2(300f, 460f), new Vector2(300f, 440f), new Vector2(220f, 440f), new Vector2(220f, 600f) },
        
        // Level 5
        new Vector2[] { new Vector2(220f, 600f), new Vector2(220f, 720f), new Vector2(380f, 720f), new Vector2(380f, 850f), new Vector2(500f, 850f) }
    };

    public string GetCurrentCorrectDirection(Vector3 playerPos)
    {
        int idx = currentLevelIndex - 1;
        if (idx < 0 || idx >= LevelWaypoints.Length) return "North";
        
        Vector2[] wps = LevelWaypoints[idx];
        if (wps.Length < 2) return "North";
        
        int closestSegIndex = 0;
        float minDistance = float.MaxValue;
        
        Vector2 pPos2D = new Vector2(playerPos.x, playerPos.z);
        
        for (int i = 0; i < wps.Length - 1; i++)
        {
            Vector2 start = wps[i];
            Vector2 end = wps[i + 1];
            
            float dist = GetDistanceToSegment(pPos2D, start, end);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestSegIndex = i;
            }
        }
        
        Vector2 s = wps[closestSegIndex];
        Vector2 e = wps[closestSegIndex + 1];
        Vector2 dir = (e - s).normalized;
        
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            return dir.y > 0 ? "North" : "South";
        }
        else
        {
            return dir.x > 0 ? "East" : "West";
        }
    }
    
    private float GetDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;
        float ab2 = Vector2.Dot(ab, ab);
        if (ab2 <= 0f) return Vector2.Distance(p, a);
        float t = Vector2.Dot(ap, ab) / ab2;
        t = Mathf.Clamp01(t);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }
}
