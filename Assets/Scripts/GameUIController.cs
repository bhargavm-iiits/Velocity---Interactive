using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance { get; private set; }

    [Header("UI Canvas")]
    public Canvas mainCanvas;

    [Header("Top Center HUD")]
    public GameObject topCenterHUD;
    public TextMeshProUGUI hudDirectionText;
    public TextMeshProUGUI hudSpeedText;
    public TextMeshProUGUI hudDistanceText;
    public TextMeshProUGUI hudObjectiveText;
    public TextMeshProUGUI hudTimerText;
    [HideInInspector] public bool isTimerQuiz = false;
    [HideInInspector] public int activeTimerLevelIndex = 1;
    [HideInInspector] public bool isLevelStartQuiz = false;
    [HideInInspector] public int activeLevelStartLevelIndex = 1;
    private HashSet<int> askedQuestionIndices = new HashSet<int>();
    private int activeQuestionIndex = 0;

    [Header("Velocity Selection Panel")]
    public GameObject selectionPanel;
    public Button[] directionButtons; // N, S, E, W
    public Button[] speedButtons; // 5, 10, 15, 20
    public Button confirmButton;
    public TextMeshProUGUI selectedVelocityPreviewText;

    [Header("Notifications")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;

    [Header("Interaction Prompt")]
    public GameObject interactionPromptPanel;
    public TextMeshProUGUI interactionPromptText;

    [Header("General Stats")]
    public TextMeshProUGUI scoreText;

    [Header("Compass UI")]
    public RectTransform compassArrowRect;
    public GameObject compassContainer;

    [Header("Final Summary Panel")]
    public GameObject summaryPanel;
    public TextMeshProUGUI finalScoreText;

    [Header("Quiz Panel References")]
    public GameObject quizPanel;
    public TextMeshProUGUI quizTitleText;
    public TextMeshProUGUI quizQuestionText;
    public TextMeshProUGUI[] optionTexts;
    public Image[] optionBgs;
    public TextMeshProUGUI quizControlsText;
    public GameObject quizResultOverlay;
    public TextMeshProUGUI quizResultTitleText;
    public TextMeshProUGUI quizResultDescText;
    public Image screenFadeImage;

    [System.Serializable]
    public struct QuizQuestion
    {
        public string question;
        public string optionA;
        public string optionB;
        public string optionC;
        public string optionD;
        public char correctOption; // 'A', 'B', 'C', 'D'
        public string feedbackCorrect;
        public string feedbackIncorrect;
    }

    [System.Serializable]
    public struct LevelQuestionData
    {
        public string question;
        public string optionA;
        public string optionB;
        public string optionC;
        public string optionD;
        public char correctOption;
        public string formula;
        public string answer;
        public string navigation;
    }

    private LevelQuestionData[] levelQuestions = new LevelQuestionData[]
    {
        new LevelQuestionData
        {
            question = "A traveler moves 200 meters along a sand-colored road in 10 seconds.\n\nWhat is the velocity?",
            optionA = "15 m/s",
            optionB = "20 m/s",
            optionC = "25 m/s",
            optionD = "30 m/s",
            correctOption = 'B',
            formula = "Velocity = Distance ÷ Time",
            answer = "200 ÷ 10 = 20 m/s",
            navigation = "Travel <color=#FFFF00><b>North</b></color> through the sand-colored path."
        },
        new LevelQuestionData
        {
            question = "A traveler moves 500 meters through a thick forest in 15 seconds.\n\nFind the speed.",
            optionA = "30 m/s",
            optionB = "33.33 m/s",
            optionC = "35 m/s",
            optionD = "40 m/s",
            correctOption = 'B',
            formula = "Speed = Distance ÷ Time",
            answer = "500 ÷ 15 = 33.33 m/s",
            navigation = "Travel <color=#FFFF00><b>East</b></color> through the forest."
        },
        new LevelQuestionData
        {
            question = "A traveler moves 1000 meters through a thick forest in 20 seconds.\n\nFind the speed.",
            optionA = "40 m/s",
            optionB = "45 m/s",
            optionC = "50 m/s",
            optionD = "60 m/s",
            correctOption = 'C',
            formula = "Speed = Distance ÷ Time",
            answer = "1000 ÷ 20 = 50 m/s",
            navigation = "Travel <color=#FFFF00><b>East</b></color> toward the ancient ruins."
        },
        new LevelQuestionData
        {
            question = "A traveler moves 1200 meters through a thick forest in 25 seconds.\n\nFind the speed.",
            optionA = "45 m/s",
            optionB = "48 m/s",
            optionC = "50 m/s",
            optionD = "55 m/s",
            correctOption = 'B',
            formula = "Speed = Distance ÷ Time",
            answer = "1200 ÷ 25 = 48 m/s",
            navigation = "Travel <color=#FFFF00><b>South</b></color> through the jungle."
        },
        new LevelQuestionData
        {
            question = "A traveler moves 1500 meters through a thick forest in 30 seconds.\n\nFind the speed.",
            optionA = "45 m/s",
            optionB = "50 m/s",
            optionC = "55 m/s",
            optionD = "60 m/s",
            correctOption = 'B',
            formula = "Speed = Distance ÷ Time",
            answer = "1500 ÷ 30 = 50 m/s",
            navigation = "Travel <color=#FFFF00><b>North</b></color> toward the temple."
        }
    };

    private QuizQuestion[] questionPool = new QuizQuestion[]
    {
        new QuizQuestion
        {
            question = "What is Velocity?",
            optionA = "Distance covered regardless of direction",
            optionB = "Speed in a specific direction",
            optionC = "Force acting on an object",
            optionD = "Time taken to travel",
            correctOption = 'B',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        },
        new QuizQuestion
        {
            question = "Velocity is",
            optionA = "Scalar quantity",
            optionB = "Vector quantity",
            optionC = "Constant quantity",
            optionD = "None",
            correctOption = 'B',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        },
        new QuizQuestion
        {
            question = "Velocity depends on",
            optionA = "Speed only",
            optionB = "Direction only",
            optionC = "Speed and Direction",
            optionD = "Time only",
            correctOption = 'C',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        },
        new QuizQuestion
        {
            question = "Velocity is equal to",
            optionA = "Distance ÷ Time",
            optionB = "Displacement ÷ Time",
            optionC = "Speed × Time",
            optionD = "Distance × Time",
            correctOption = 'B',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        },
        new QuizQuestion
        {
            question = "To convert a speed from km/h to m/s, what should you multiply it by?",
            optionA = "18/5",
            optionB = "5/18",
            optionC = "3.6",
            optionD = "1/3.6",
            correctOption = 'B',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        },
        new QuizQuestion
        {
            question = "The cave door opens only if your speed is 90 km/h. Convert it into m/s.",
            optionA = "20 m/s",
            optionB = "22 m/s",
            optionC = "25 m/s",
            optionD = "30 m/s",
            correctOption = 'C',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        },
        new QuizQuestion
        {
            question = "Ancient Compass Message\n\"To reach the wooden bridge, travel at 18 km/h.\"\nConvert the speed into m/s.",
            optionA = "3 m/s",
            optionB = "5 m/s",
            optionC = "8 m/s",
            optionD = "10 m/s",
            correctOption = 'B',
            feedbackCorrect = "You have successfully understood the velocity concept.",
            feedbackIncorrect = "The magical compass rejects your answer."
        }
    };

    private CheckpointTrigger activeQuizTrigger;
    private int selectedOptionIndex = 0;
    private bool isQuizActive = false;
    private bool isDisplayingResult = false;
    private AudioSource uiAudioSource;
    private float lastThumbstickTime = 0f;
    private const float thumbstickCooldown = 0.2f;
    private Coroutine notificationCoroutine;

    public bool IsQuizActive { get { return isQuizActive; } }

    private JunctionController activeJunction;
    private string selectedDirection = "North";
    private float selectedSpeed = 2f;
    private TMP_FontAsset defaultFont;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Programmatically build UI if not pre-configured
        if (mainCanvas == null)
        {
            CreateDefaultUI();
        }
    }

    private void Start()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (notificationPanel != null) notificationPanel.SetActive(false);
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
        if (summaryPanel != null) summaryPanel.SetActive(false);

        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f; // 2D Sound
    }

    private void Update()
    {
        if (isQuizActive)
        {
            // Force cursor to be visible and unlocked during the quiz
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }

            HandleQuizInputs();
            return;
        }

        // Dynamically update distance text based on cumulative distance traveled
        if (PlayerController.Instance != null && GameManager.Instance != null && GameManager.Instance.levelManager != null)
        {
            if (hudDistanceText != null)
            {
                hudDistanceText.text = $"Distance: {Mathf.RoundToInt(GameManager.Instance.levelDistanceTraveled)} m";
            }

            // Dynamically update player's real-time speed in m/s
            if (hudDirectionText != null)
            {
                if (PlayerController.Instance.isMovingInWrongDirection)
                {
                    hudDirectionText.text = $"Speed: {PlayerController.Instance.currentSpeedMPS:F1} m/s <color=red>(Wrong Direction!)</color>";
                }
                else
                {
                    hudDirectionText.text = $"Speed: {PlayerController.Instance.currentSpeedMPS:F1} m/s";
                }
            }
        }
    }

    private Vector3 GetCurrentTargetPos()
    {
        LevelManager lm = GameManager.Instance.levelManager;
        int level = GameManager.Instance.currentLevelIndex;
        switch (level)
        {
            case 1: if (lm.level1Chest != null) return lm.level1Chest.transform.position; break;
            case 2:
                if (lm.woodCollectible != null && lm.woodCollectible.activeSelf) return lm.woodCollectible.transform.position;
                if (lm.stoneCollectible != null && lm.stoneCollectible.activeSelf) return lm.stoneCollectible.transform.position;
                if (lm.level2Chest != null) return lm.level2Chest.transform.position;
                break;
            case 3: if (lm.level3Chest != null) return lm.level3Chest.transform.position; break;
            case 4: if (lm.level4Chest != null) return lm.level4Chest.transform.position; break;
            case 5: if (lm.level5Chest != null) return lm.level5Chest.transform.position; break;
        }
        return Vector3.zero;
    }

    public void SetTargetInstructions(string direction, float speed, float distance, string objective)
    {
        // Direction is now guided by the 3D scope arrow in front of the player camera.
        // The hudDirectionText now displays the real-time speed in Update().
        if (hudSpeedText != null) hudSpeedText.text = $"Required Speed: {speed} m/s";
        if (hudDistanceText != null) hudDistanceText.text = $"Distance: {distance} m";
        if (hudObjectiveText != null) hudObjectiveText.text = $"Objective: {objective}";
    }

    public void UpdateScoreUI(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void ShowNotification(string text)
    {
        if (notificationPanel == null || notificationText == null) return;
        
        notificationText.text = text;
        notificationPanel.SetActive(true);
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(HideNotificationAfterDelay(3.5f));
    }

    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationPanel != null) notificationPanel.SetActive(false);
    }

    public void ShowInteractionPrompt(string text)
    {
        if (interactionPromptPanel != null && interactionPromptText != null)
        {
            interactionPromptText.text = text;
            interactionPromptPanel.SetActive(true);
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptPanel != null)
        {
            interactionPromptPanel.SetActive(false);
        }
    }

    public void ShowVelocityPanel(JunctionController junction)
    {
        activeJunction = junction;
        selectedDirection = "North";
        selectedSpeed = 2f; // Defaults to 2f to match the option buttons
        UpdatePreviewText();
        UpdateVelocitySelectionHighlights();

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }

        // Stop player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
        }

        // Unlock cursor for PC controls
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SelectDirection(string dir)
    {
        selectedDirection = dir;
        UpdatePreviewText();
        UpdateVelocitySelectionHighlights();
    }

    public void SelectSpeed(float speed)
    {
        selectedSpeed = speed;
        UpdatePreviewText();
        UpdateVelocitySelectionHighlights();
    }

    private void UpdatePreviewText()
    {
        if (selectedVelocityPreviewText != null)
        {
            selectedVelocityPreviewText.text = $"Selected: {selectedDirection} @ {selectedSpeed} m/s";
        }
    }

    private void UpdateVelocitySelectionHighlights()
    {
        string[] dirs = { "North", "South", "East", "West" };
        if (directionButtons != null)
        {
            for (int i = 0; i < directionButtons.Length; i++)
            {
                if (directionButtons[i] != null)
                {
                    Image img = directionButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        bool isSelected = dirs[i].Equals(selectedDirection, System.StringComparison.OrdinalIgnoreCase);
                        img.color = isSelected ? new Color(0.1f, 0.6f, 0.1f, 1f) : new Color(0.2f, 0.25f, 0.35f, 1f);
                    }
                }
            }
        }

        float[] speeds = { 2f, 3f, 4f, 6f };
        if (speedButtons != null)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                if (speedButtons[i] != null)
                {
                    Image img = speedButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        bool isSelected = Mathf.Approximately(speeds[i], selectedSpeed);
                        img.color = isSelected ? new Color(0.1f, 0.6f, 0.1f, 1f) : new Color(0.2f, 0.25f, 0.35f, 1f);
                    }
                }
            }
        }
    }

    public void ConfirmVelocity()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = true;
        }

        if (activeJunction != null)
        {
            activeJunction.ConfirmVelocitySelection(selectedDirection, selectedSpeed);
        }
    }

    public void ShowFinalSummary(int finalScore)
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
        }
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {finalScore}";
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Programmatically build UI Canvas hierarchy with beautiful, modern glassmorphic look
    private void CreateDefaultUI()
    {
        GameObject canvasObj = new GameObject("VelocityQuestCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // Match height to prevent vertical clipping on wide/Free Aspect screens
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        // Ensure EventSystem exists for UI interactions (works with both new & old input system)
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            #if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            #endif
            DontDestroyOnLoad(eventSystemObj);
        }

        // Core font setup
        defaultFont = null;
        try
        {
            defaultFont = TMP_Settings.defaultFontAsset;
        }
        catch (System.Exception)
        {
            // Fallback silently, TMPro will assign its default internal font
        }
        TMP_FontAsset font = defaultFont;

        // 1. Stats Panel (Top-Left Corner)
        GameObject statsObj = new GameObject("StatsPanel");
        statsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 1f);
        statsRect.anchorMax = new Vector2(0f, 1f);
        statsRect.pivot = new Vector2(0f, 1f);
        statsRect.anchoredPosition = new Vector2(50f, -20f); // Shifted right to be fully visible from the left edge
        statsRect.sizeDelta = new Vector2(380f, 160f);

        Image statsBg = statsObj.AddComponent<Image>();
        statsBg.color = new Color(0.1f, 0.15f, 0.2f, 0.75f); // Glassmorphism Dark Blue

        // Add child texts
        scoreText = CreateText(statsObj, "ScoreText", "Score: 0", 18, new Vector2(0f, 45f), font, Color.white);
        ConfigureHUDText(scoreText, 45f);

        hudDirectionText = CreateText(statsObj, "SpeedText", "Speed: 0.0 m/s", 16, new Vector2(0f, 15f), font, Color.cyan);
        ConfigureHUDText(hudDirectionText, 15f);

        hudDistanceText = CreateText(statsObj, "DistanceText", "Distance: 0 m", 16, new Vector2(0f, -15f), font, Color.yellow);
        ConfigureHUDText(hudDistanceText, -15f);

        hudObjectiveText = CreateText(statsObj, "ObjectiveText", "Objective: Start", 14, new Vector2(0f, -45f), font, Color.green);
        ConfigureHUDText(hudObjectiveText, -45f);
        topCenterHUD = statsObj;

        // 1b. Timer Panel (Top-Right Corner)
        GameObject timerPanelObj = new GameObject("TimerPanel");
        timerPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform timerPanelRect = timerPanelObj.AddComponent<RectTransform>();
        timerPanelRect.anchorMin = new Vector2(1f, 1f);
        timerPanelRect.anchorMax = new Vector2(1f, 1f);
        timerPanelRect.pivot = new Vector2(1f, 1f);
        timerPanelRect.anchoredPosition = new Vector2(-50f, -20f);
        timerPanelRect.sizeDelta = new Vector2(160f, 50f);

        Image timerBg = timerPanelObj.AddComponent<Image>();
        timerBg.color = new Color(0.2f, 0.1f, 0.1f, 0.75f); // Reddish glassmorphism

        hudTimerText = CreateText(timerPanelObj, "TimerText", "Time: --", 18, Vector2.zero, font, Color.white);
        hudTimerText.alignment = TextAlignmentOptions.Center;
        RectTransform timerTextRect = hudTimerText.GetComponent<RectTransform>();
        timerTextRect.anchorMin = Vector2.zero;
        timerTextRect.anchorMax = Vector2.one;
        timerTextRect.sizeDelta = Vector2.zero;
        timerTextRect.anchoredPosition = Vector2.zero;

        // Required speed doesn't need to be displayed on screen-space HUD as it's on the physical sign board.
        // We set hudSpeedText to a dummy text on a disabled object to avoid NullReferenceException
        GameObject dummySpeedObj = new GameObject("DummySpeedText");
        dummySpeedObj.transform.SetParent(statsObj.transform, false);
        hudSpeedText = dummySpeedObj.AddComponent<TextMeshProUGUI>();
        hudSpeedText.enabled = false;

        // 3. Compass arrow on screen (simple pointer overlay)
        GameObject compassUI = new GameObject("CompassUI");
        compassUI.transform.SetParent(canvasObj.transform, false);
        RectTransform compassRect = compassUI.AddComponent<RectTransform>();
        compassRect.anchorMin = new Vector2(0.5f, 0.5f);
        compassRect.anchorMax = new Vector2(0.5f, 0.5f);
        compassRect.pivot = new Vector2(0.5f, 0.5f);
        compassRect.anchoredPosition = new Vector2(0f, 220f);
        compassRect.sizeDelta = new Vector2(80f, 80f);
        Image compassBg = compassUI.AddComponent<Image>();
        compassBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        compassContainer = compassUI;

        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(compassUI.transform, false);
        compassArrowRect = arrowObj.AddComponent<RectTransform>();
        compassArrowRect.sizeDelta = new Vector2(20f, 60f);
        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = Color.red;

        // 4. Interaction Prompt
        GameObject interactObj = new GameObject("InteractionPrompt");
        interactObj.transform.SetParent(canvasObj.transform, false);
        RectTransform interactRect = interactObj.AddComponent<RectTransform>();
        interactRect.anchorMin = new Vector2(0.5f, 0.3f);
        interactRect.anchorMax = new Vector2(0.5f, 0.3f);
        interactRect.pivot = new Vector2(0.5f, 0.5f);
        interactRect.sizeDelta = new Vector2(300f, 60f);
        Image interactBg = interactObj.AddComponent<Image>();
        interactBg.color = new Color(0f, 0f, 0f, 0.7f);
        interactionPromptText = CreateText(interactObj, "PromptText", "Press E to Interact", 16, Vector2.zero, font, Color.yellow);
        interactionPromptPanel = interactObj;

        // 5. Notifications
        GameObject notifyObj = new GameObject("NotificationPanel");
        notifyObj.transform.SetParent(canvasObj.transform, false);
        RectTransform notifyRect = notifyObj.AddComponent<RectTransform>();
        notifyRect.anchorMin = new Vector2(0.5f, 0.8f);
        notifyRect.anchorMax = new Vector2(0.5f, 0.8f);
        notifyRect.pivot = new Vector2(0.5f, 0.5f);
        notifyRect.sizeDelta = new Vector2(400f, 100f);
        Image notifyBg = notifyObj.AddComponent<Image>();
        notifyBg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Bold notification red
        notificationText = CreateText(notifyObj, "NotifyText", "Checkpoint Saved", 18, Vector2.zero, font, Color.white);
        notificationPanel = notifyObj;

        // 6. Final Summary Panel
        GameObject sumObj = new GameObject("SummaryPanel");
        sumObj.transform.SetParent(canvasObj.transform, false);
        RectTransform sumRect = sumObj.AddComponent<RectTransform>();
        sumRect.anchorMin = Vector2.zero;
        sumRect.anchorMax = Vector2.one;
        sumRect.sizeDelta = Vector2.zero;
        Image sumBg = sumObj.AddComponent<Image>();
        sumBg.color = new Color(0.05f, 0.05f, 0.1f, 0.98f);
        
        CreateText(sumObj, "Title", "🎉 Congratulations!", 30, new Vector2(0f, 180f), font, Color.yellow);
        
        string educationalText = "You have successfully completed all five levels.\n\nYou mastered the concepts of:\n• Velocity\n• Speed\n• Direction\n\nYou discovered every hidden treasure.\n\n🏆 LEVEL IS FINISHED";
        CreateText(sumObj, "EducationText", educationalText, 18, new Vector2(0f, 30f), font, Color.white);
        
        finalScoreText = CreateText(sumObj, "FinalScore", "Final Score: 0", 22, new Vector2(0f, -90f), font, Color.cyan);

        GameObject playAgainBtn = CreateButton(sumObj, "PlayAgainBtn", "Play Again", new Vector2(-120f, -170f), new Vector2(160f, 45f));
        Button playBtn = playAgainBtn.GetComponent<Button>();
        playBtn.onClick.AddListener(() => {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReloadGameFromStart();
            }
        });
        
        GameObject exitBtn = CreateButton(sumObj, "ExitBtn", "Exit Game", new Vector2(120f, -170f), new Vector2(160f, 45f));
        Button exBtn = exitBtn.GetComponent<Button>();
        exBtn.onClick.AddListener(() => {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
        
        summaryPanel = sumObj;

        // 7. Velocity Selection Panel
        GameObject selObj = new GameObject("VelocitySelectionPanel");
        selObj.transform.SetParent(canvasObj.transform, false);
        RectTransform selRect = selObj.AddComponent<RectTransform>();
        selRect.anchorMin = new Vector2(0.5f, 0.5f);
        selRect.anchorMax = new Vector2(0.5f, 0.5f);
        selRect.pivot = new Vector2(0.5f, 0.5f);
        selRect.sizeDelta = new Vector2(500f, 380f);
        Image selBg = selObj.AddComponent<Image>();
        selBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        CreateText(selObj, "SelTitle", "CHOOSE VELOCITY", 22, new Vector2(0f, 160f), font, Color.yellow);
        CreateText(selObj, "DirSubtitle", "Select Direction", 14, new Vector2(0f, 110f), font, Color.cyan);

        // Direction buttons layout (N, S, E, W)
        string[] dirs = { "North", "South", "East", "West" };
        directionButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            string dirName = dirs[i];
            GameObject btnObj = CreateButton(selObj, $"Btn_{dirName}", dirName, new Vector2(-150f + i * 100f, 70f), new Vector2(90f, 40f));
            directionButtons[i] = btnObj.GetComponent<Button>();
            directionButtons[i].onClick.AddListener(() => SelectDirection(dirName));
        }

        CreateText(selObj, "SpeedSubtitle", "Select Speed", 14, new Vector2(0f, 15f), font, Color.cyan);

        // Speed buttons layout
        float[] speeds = { 2f, 3f, 4f, 6f };
        speedButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float speedVal = speeds[i];
            GameObject btnObj = CreateButton(selObj, $"Btn_{speedVal}", $"{speedVal} m/s", new Vector2(-150f + i * 100f, -25f), new Vector2(90f, 40f));
            speedButtons[i] = btnObj.GetComponent<Button>();
            speedButtons[i].onClick.AddListener(() => SelectSpeed(speedVal));
        }

        selectedVelocityPreviewText = CreateText(selObj, "PreviewText", "Selected: North @ 2 m/s", 16, new Vector2(0f, -90f), font, Color.white);

        GameObject confirmBtnObj = CreateButton(selObj, "ConfirmBtn", "CONFIRM", new Vector2(0f, -140f), new Vector2(160f, 45f));
        confirmButton = confirmBtnObj.GetComponent<Button>();
        confirmButton.onClick.AddListener(ConfirmVelocity);
        
        // Style confirm button slightly differently (green)
        confirmBtnObj.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f, 1f);
        confirmBtnObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;

        selectionPanel = selObj;

        // 8. Compass Toggle Button (Top-Right Corner)
        GameObject compBtnObj = CreateButton(canvasObj, "CompassToggleButton", "COMPASS", new Vector2(-50f, -20f), new Vector2(100f, 40f)); // Shifted left for symmetry and safe area spacing
        RectTransform compBtnRect = compBtnObj.GetComponent<RectTransform>();
        compBtnRect.anchorMin = new Vector2(1f, 1f);
        compBtnRect.anchorMax = new Vector2(1f, 1f);
        compBtnRect.pivot = new Vector2(1f, 1f);
        
        // Style button with nice cyan border/text and dark blue transparent glassmorphic background
        compBtnObj.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 0.8f);
        compBtnObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.cyan;
        
        Button compBtn = compBtnObj.GetComponent<Button>();
        compBtn.onClick.AddListener(() => {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.Toggle3DCompass();
            }
        });

        // 9. Checkpoint Quiz Panel
        GameObject qPanel = new GameObject("QuizPanel");
        qPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform qRect = qPanel.AddComponent<RectTransform>();
        qRect.anchorMin = Vector2.zero;
        qRect.anchorMax = Vector2.one;
        qRect.sizeDelta = Vector2.zero;
        
        Image qBg = qPanel.AddComponent<Image>();
        qBg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f); // Semi-transparent dark background
        
        // Inner quiz box (glassmorphism look)
        GameObject qBox = new GameObject("QuizBox");
        qBox.transform.SetParent(qPanel.transform, false);
        RectTransform qBoxRect = qBox.AddComponent<RectTransform>();
        qBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        qBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        qBoxRect.pivot = new Vector2(0.5f, 0.5f);
        qBoxRect.sizeDelta = new Vector2(600f, 410f);
        Image qBoxBg = qBox.AddComponent<Image>();
        qBoxBg.color = new Color(0.1f, 0.12f, 0.2f, 0.95f); // Dark blueish glass panel
        
        // Title (Removed 📖 emoji to resolve LiberationSans font warning)
        quizTitleText = CreateText(qBox, "QuizTitle", "Velocity Knowledge Check", 20, new Vector2(0f, 170f), font, Color.yellow);
        
        // Question
        quizQuestionText = CreateText(qBox, "QuizQuestion", "Question text goes here?", 16, new Vector2(0f, 105f), font, Color.white);
        quizQuestionText.rectTransform.sizeDelta = new Vector2(520f, 60f);
        quizQuestionText.textWrappingMode = TextWrappingModes.Normal;
        
        // 4 Options (A, B, C, D)
        optionTexts = new TextMeshProUGUI[4];
        optionBgs = new Image[4];
        
        for (int i = 0; i < 4; i++)
        {
            GameObject optObj = new GameObject($"Option_{i}");
            optObj.transform.SetParent(qBox.transform, false);
            RectTransform optRect = optObj.AddComponent<RectTransform>();
            optRect.anchoredPosition = new Vector2(0f, 30f - i * 44f);
            optRect.sizeDelta = new Vector2(500f, 36f);
            
            Image optBg = optObj.AddComponent<Image>();
            optBg.color = new Color(0.15f, 0.18f, 0.25f, 1f);
            optionBgs[i] = optBg;
            
            // Add Button component for mouse clickability!
            Button btn = optObj.AddComponent<Button>();
            btn.targetGraphic = optBg;
            int optionIndex = i;
            btn.onClick.AddListener(() => OnOptionClicked(optionIndex));
            
            TextMeshProUGUI tmp = CreateText(optObj, "Text", $"Option {i}", 16, Vector2.zero, font, Color.white);
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.sizeDelta = Vector2.zero;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = new Vector4(20f, 0f, 20f, 0f);
            tmp.raycastTarget = false; // Let mouse clicks pass through to the button background
            
            optionTexts[i] = tmp;
        }
        
        // Green Confirm Button below Options
        GameObject qConfirmBtnObj = CreateButton(qBox, "QuizConfirmBtn", "CONFIRM", new Vector2(0f, -150f), new Vector2(160f, 34f));
        Button qConfirmButton = qConfirmBtnObj.GetComponent<Button>();
        qConfirmButton.onClick.AddListener(ConfirmQuizAnswer);
        qConfirmBtnObj.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f, 1f);
        qConfirmBtnObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
        
        // Controls help
        quizControlsText = CreateText(qBox, "QuizControls", "Controls: W/S -> Navigate | E -> Confirm\nOr click Options & click CONFIRM", 12, new Vector2(0f, -185f), font, Color.gray);
        quizControlsText.rectTransform.sizeDelta = new Vector2(500f, 36f);
        
        // 10. Quiz Result Overlay
        GameObject rOverlay = new GameObject("QuizResultOverlay");
        rOverlay.transform.SetParent(qPanel.transform, false);
        RectTransform rOverlayRect = rOverlay.AddComponent<RectTransform>();
        rOverlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        rOverlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        rOverlayRect.pivot = new Vector2(0.5f, 0.5f);
        rOverlayRect.sizeDelta = new Vector2(520f, 320f);
        
        Image rOverlayBg = rOverlay.AddComponent<Image>();
        rOverlayBg.color = new Color(0.1f, 0.5f, 0.1f, 0.98f);
        
        quizResultTitleText = CreateText(rOverlay, "Title", "Correct Answer!", 24, new Vector2(0f, 90f), font, Color.white);
        quizResultDescText = CreateText(rOverlay, "Desc", "Description text...", 16, new Vector2(0f, -20f), font, Color.white);
        quizResultDescText.rectTransform.sizeDelta = new Vector2(460f, 180f);
        quizResultDescText.textWrappingMode = TextWrappingModes.Normal;
        
        quizResultOverlay = rOverlay;
        quizResultOverlay.SetActive(false);
        
        quizPanel = qPanel;
        quizPanel.SetActive(false);

        // 11. Screen Fade Image
        GameObject fadeObj = new GameObject("ScreenFadeImage");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform fadeRect = fadeObj.AddComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        
        screenFadeImage = fadeObj.AddComponent<Image>();
        screenFadeImage.color = new Color(0f, 0f, 0f, 0f);
        screenFadeImage.raycastTarget = false;
    }

    private int GetQuestionIndexForCheckpoint(int cpIndex)
    {
        switch (cpIndex)
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 4: return 5; // Cave (90 km/h)
            case 5: return 3; // Velocity is equal to
            case 6: return 6; // Bridge (18 km/h)
            case 7: return 4; // km/h to m/s conversion
            default: return Random.Range(0, 7);
        }
    }

    public void StartLevelStartQuiz(int levelIndex)
    {
        if (isQuizActive) return;

        isLevelStartQuiz = true;
        activeLevelStartLevelIndex = levelIndex;
        isQuizActive = true;
        isDisplayingResult = false;
        selectedOptionIndex = 0;

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.movementFrozen = true;
            PlayerController.Instance.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LevelQuestionData question = levelQuestions[levelIndex - 1];

        if (quizTitleText != null)
        {
            quizTitleText.text = $"LEVEL {levelIndex} CHALLENGE";
        }

        if (quizQuestionText != null)
        {
            quizQuestionText.text = question.question;
        }

        if (optionTexts != null && optionTexts.Length >= 4)
        {
            optionTexts[0].text = "○ A. " + question.optionA;
            optionTexts[1].text = "○ B. " + question.optionB;
            optionTexts[2].text = "○ C. " + question.optionC;
            optionTexts[3].text = "○ D. " + question.optionD;
        }

        HighlightQuizOption(selectedOptionIndex);

        if (quizResultOverlay != null)
        {
            quizResultOverlay.SetActive(false);
        }

        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        if (CompassSystem.Instance != null)
        {
            CompassSystem.Instance.SetCompassVisible(false);
        }
    }

    public void StartTimerQuiz(int levelIndex)
    {
        if (isQuizActive) return;

        isTimerQuiz = true;
        activeTimerLevelIndex = levelIndex;
        isQuizActive = true;
        isDisplayingResult = false;
        selectedOptionIndex = 0;

        // Pause gameplay
        Time.timeScale = 0f;

        // Disable player controller movement and inputs
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Populate quiz question based on levelIndex
        int qIdx = GetUniqueQuestionIndex(GetQuestionIndexForTimer(levelIndex));
        activeQuestionIndex = qIdx;
        QuizQuestion question = questionPool[qIdx];

        if (quizQuestionText != null)
        {
            quizQuestionText.text = "TIME EXPIRED! Final Challenge:\n" + question.question;
        }

        if (optionTexts != null && optionTexts.Length >= 4)
        {
            optionTexts[0].text = "○ A. " + question.optionA;
            optionTexts[1].text = "○ B. " + question.optionB;
            optionTexts[2].text = "○ C. " + question.optionC;
            optionTexts[3].text = "○ D. " + question.optionD;
        }

        HighlightQuizOption(selectedOptionIndex);

        if (quizResultOverlay != null)
        {
            quizResultOverlay.SetActive(false);
        }

        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }
    }

    public void ResetQuestionHistory()
    {
        askedQuestionIndices.Clear();
    }

    private int GetUniqueQuestionIndex(int preferredIndex)
    {
        if (!askedQuestionIndices.Contains(preferredIndex))
        {
            askedQuestionIndices.Add(preferredIndex);
            return preferredIndex;
        }

        // Try to find another unasked index
        for (int i = 0; i < questionPool.Length; i++)
        {
            if (!askedQuestionIndices.Contains(i))
            {
                askedQuestionIndices.Add(i);
                return i;
            }
        }

        // Reset if all are asked, except the preferred one
        askedQuestionIndices.Clear();
        askedQuestionIndices.Add(preferredIndex);
        return preferredIndex;
    }

    private int GetQuestionIndexForTimer(int levelIndex)
    {
        switch (levelIndex)
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 4: return 4;
            default: return 3;
        }
    }

    public void StartQuiz(CheckpointTrigger trigger)
    {
        if (isQuizActive) return;

        activeQuizTrigger = trigger;
        isQuizActive = true;
        isDisplayingResult = false;
        selectedOptionIndex = 0;

        // Pause gameplay
        Time.timeScale = 0f;

        // Disable player controller movement and inputs
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Populate quiz question
        int qIdx = GetUniqueQuestionIndex(GetQuestionIndexForCheckpoint(trigger.checkpointIndex));
        activeQuestionIndex = qIdx;
        QuizQuestion question = questionPool[qIdx];

        if (quizQuestionText != null)
        {
            quizQuestionText.text = question.question;
        }

        if (optionTexts != null && optionTexts.Length >= 4)
        {
            optionTexts[0].text = "○ A. " + question.optionA;
            optionTexts[1].text = "○ B. " + question.optionB;
            optionTexts[2].text = "○ C. " + question.optionC;
            optionTexts[3].text = "○ D. " + question.optionD;
        }

        HighlightQuizOption(selectedOptionIndex);

        if (quizResultOverlay != null)
        {
            quizResultOverlay.SetActive(false);
        }

        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }
    }

    private void HandleQuizInputs()
    {
        if (isDisplayingResult) return;

        bool selectionChanged = false;
        bool upPressed = false;
        bool downPressed = false;
        bool confirmPressed = false;

        // 1. New Input System checks
        if (Keyboard.current != null)
        {
            upPressed |= Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame;
            downPressed |= Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame;
            confirmPressed |= Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
        }

        // 2. Legacy Input fallback checks (completely bulletproof under Time.timeScale = 0, wrapped in try-catch to prevent crashing under new Input System settings)
        try
        {
            upPressed |= Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            downPressed |= Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            confirmPressed |= Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
        }
        catch (System.InvalidOperationException)
        {
            // Ignore if legacy input system is disabled in player settings
        }

        // 3. VR Controller / Gamepad checks
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            upPressed |= stick.y > 0.5f;
            downPressed |= stick.y < -0.5f;
            confirmPressed |= Gamepad.current.buttonWest.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame;
        }

        // Cooldown check for scrolling
        if (Time.unscaledTime - lastThumbstickTime > thumbstickCooldown)
        {
            if (upPressed)
            {
                selectedOptionIndex = (selectedOptionIndex - 1 + 4) % 4;
                selectionChanged = true;
                lastThumbstickTime = Time.unscaledTime;
            }
            else if (downPressed)
            {
                selectedOptionIndex = (selectedOptionIndex + 1) % 4;
                selectionChanged = true;
                lastThumbstickTime = Time.unscaledTime;
            }
        }

        // Confirm answer if pressed
        if (confirmPressed)
        {
            ConfirmQuizAnswer();
            return;
        }

        if (selectionChanged)
        {
            HighlightQuizOption(selectedOptionIndex);
            PlayTickSound();
        }
    }

    public void OnOptionClicked(int index)
    {
        if (isDisplayingResult) return;
        selectedOptionIndex = index;
        HighlightQuizOption(selectedOptionIndex);
        PlayTickSound();
    }

    private void HighlightQuizOption(int index)
    {
        for (int i = 0; i < 4; i++)
        {
            if (i == index)
            {
                // Glow golden highlight
                optionBgs[i].color = new Color(1.0f, 0.85f, 0.2f, 1f); // Golden background
                optionTexts[i].color = Color.black; // Dark text for contrast
            }
            else
            {
                // Default style
                optionBgs[i].color = new Color(0.15f, 0.18f, 0.25f, 1f);
                optionTexts[i].color = Color.white;
            }
        }
    }

    private void ConfirmQuizAnswer()
    {
        if (isDisplayingResult) return;
        isDisplayingResult = true;

        if (isLevelStartQuiz)
        {
            LevelQuestionData lq = levelQuestions[activeLevelStartLevelIndex - 1];
            char lqChosenChar = (char)('A' + selectedOptionIndex);
            bool lqIsCorrect = lqChosenChar == lq.correctOption;
            StartCoroutine(ShowLevelStartQuizResultCoroutine(lqIsCorrect, lq));
            return;
        }

        QuizQuestion question = questionPool[activeQuestionIndex];

        char chosenChar = (char)('A' + selectedOptionIndex);
        bool isCorrect = chosenChar == question.correctOption;

        StartCoroutine(ShowQuizResultCoroutine(isCorrect, question));
    }

    private IEnumerator ShowLevelStartQuizResultCoroutine(bool isCorrect, LevelQuestionData question)
    {
        int correctIdx = question.correctOption - 'A';

        if (isCorrect)
        {
            if (uiAudioSource != null)
            {
                AudioClip chime = CreateSuccessChimeClip();
                uiAudioSource.PlayOneShot(chime);
            }

            for (int i = 0; i < 5; i++)
            {
                optionBgs[correctIdx].color = (i % 2 == 0) ? Color.green : new Color(1.0f, 0.85f, 0.2f, 1f);
                optionTexts[correctIdx].color = (i % 2 == 0) ? Color.white : Color.black;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            optionBgs[correctIdx].color = Color.green;
            optionTexts[correctIdx].color = Color.white;

            if (quizResultOverlay != null)
            {
                Image overlayImg = quizResultOverlay.GetComponent<Image>();
                if (overlayImg != null)
                {
                    overlayImg.color = new Color(0.08f, 0.45f, 0.08f, 0.98f);
                }

                if (quizResultTitleText != null)
                {
                    quizResultTitleText.text = "Correct Answer!";
                }

                if (quizResultDescText != null)
                {
                    quizResultDescText.text = $"Formula: {question.formula}\n\nAnswer: {question.answer}\n\n* Compass Activated *\n{question.navigation}";
                }

                quizResultOverlay.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(4.5f);

            if (quizResultOverlay != null) quizResultOverlay.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);

            isQuizActive = false;
            isLevelStartQuiz = false;
            Time.timeScale = 1f;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.movementFrozen = false;
                PlayerController.Instance.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (CompassSystem.Instance != null)
            {
                CompassSystem.Instance.SetCompassVisible(true);
            }

            ShowNotification($"* Compass Activated *\n{question.navigation}");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartLevelTimer();
            }
        }
        else
        {
            if (uiAudioSource != null)
            {
                AudioClip buzzer = CreateErrorBuzzerClip();
                uiAudioSource.PlayOneShot(buzzer);
            }

            for (int i = 0; i < 5; i++)
            {
                optionBgs[selectedOptionIndex].color = (i % 2 == 0) ? Color.red : new Color(0.15f, 0.18f, 0.25f, 1f);
                optionTexts[selectedOptionIndex].color = (i % 2 == 0) ? Color.white : Color.gray;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            optionBgs[selectedOptionIndex].color = Color.red;
            optionTexts[selectedOptionIndex].color = Color.white;

            if (quizResultOverlay != null)
            {
                Image overlayImg = quizResultOverlay.GetComponent<Image>();
                if (overlayImg != null)
                {
                    overlayImg.color = new Color(0.7f, 0.08f, 0.08f, 0.98f);
                }

                if (quizResultTitleText != null)
                {
                    quizResultTitleText.text = "Incorrect Answer!";
                }

                if (quizResultDescText != null)
                {
                    quizResultDescText.text = "Incorrect. Try Again.";
                }

                quizResultOverlay.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(2.0f);

            if (quizResultOverlay != null)
            {
                quizResultOverlay.SetActive(false);
            }

            HighlightQuizOption(selectedOptionIndex);
            isDisplayingResult = false;
        }
    }

    private IEnumerator ShowQuizResultCoroutine(bool isCorrect, QuizQuestion question)
    {
        int correctIdx = question.correctOption - 'A';

        if (isCorrect)
        {
            if (uiAudioSource != null)
            {
                AudioClip chime = CreateSuccessChimeClip();
                uiAudioSource.PlayOneShot(chime);
            }

            // Animate correct answer
            for (int i = 0; i < 5; i++)
            {
                optionBgs[correctIdx].color = (i % 2 == 0) ? Color.green : new Color(1.0f, 0.85f, 0.2f, 1f);
                optionTexts[correctIdx].color = (i % 2 == 0) ? Color.white : Color.black;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            optionBgs[correctIdx].color = Color.green;
            optionTexts[correctIdx].color = Color.white;
        }
        else
        {
            if (uiAudioSource != null)
            {
                AudioClip buzzer = CreateErrorBuzzerClip();
                uiAudioSource.PlayOneShot(buzzer);
            }

            // Animate incorrect selection and show correct answer
            for (int i = 0; i < 5; i++)
            {
                optionBgs[selectedOptionIndex].color = (i % 2 == 0) ? Color.red : new Color(0.15f, 0.18f, 0.25f, 1f);
                optionTexts[selectedOptionIndex].color = (i % 2 == 0) ? Color.white : Color.gray;
                optionBgs[correctIdx].color = (i % 2 == 0) ? Color.green : new Color(0.15f, 0.18f, 0.25f, 1f);
                optionTexts[correctIdx].color = (i % 2 == 0) ? Color.white : Color.gray;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            optionBgs[selectedOptionIndex].color = Color.red;
            optionTexts[selectedOptionIndex].color = Color.white;
            optionBgs[correctIdx].color = Color.green;
            optionTexts[correctIdx].color = Color.white;
        }

        // Show result overlay
        if (quizResultOverlay != null)
        {
            Image overlayImg = quizResultOverlay.GetComponent<Image>();
            if (overlayImg != null)
            {
                overlayImg.color = isCorrect ? new Color(0.08f, 0.45f, 0.08f, 0.98f) : new Color(0.7f, 0.08f, 0.08f, 0.98f);
            }

            if (quizResultTitleText != null)
            {
                quizResultTitleText.text = isCorrect ? "Correct Answer!" : "Incorrect Answer!";
            }

            if (quizResultDescText != null)
            {
                if (isTimerQuiz)
                {
                    quizResultDescText.text = isCorrect 
                        ? $"{question.feedbackCorrect}\n\nLevel Cleared.\nAdvancing to the next objective."
                        : $"{question.feedbackIncorrect}\n\nTime expired. Restarting the game from the beginning...";
                }
                else
                {
                    quizResultDescText.text = isCorrect 
                        ? $"{question.feedbackCorrect}\n\nCheckpoint Cleared.\nProceed to the next objective."
                        : $"{question.feedbackIncorrect}\n\nReloading from the last checkpoint...";
                }
            }

            quizResultOverlay.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(2.5f);

        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        if (isTimerQuiz)
        {
            isQuizActive = false;
            isTimerQuiz = false;
            Time.timeScale = 1f;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (isCorrect)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CompleteLevel();
                }
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReloadGameFromStart();
                }
            }
            yield break;
        }

        if (isCorrect)
        {
            isQuizActive = false;
            Time.timeScale = 1f;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (activeQuizTrigger != null)
            {
                activeQuizTrigger.OnQuizCleared();
            }
        }
        else
        {
            if (activeQuizTrigger != null)
            {
                activeQuizTrigger.OnQuizFailed();
            }
        }
    }

    public IEnumerator FadeToBlackAndRespawn(Vector3 respawnPos, Quaternion respawnRot)
    {
        if (screenFadeImage != null)
        {
            screenFadeImage.raycastTarget = true;
            float elapsed = 0f;
            float duration = 1.0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                screenFadeImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            screenFadeImage.color = new Color(0f, 0f, 0f, 1f);
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ResetToPosition(respawnPos, respawnRot);
        }

        if (GameManager.Instance != null && GameManager.Instance.levelManager != null)
        {
            GameManager.Instance.levelManager.SetupLevel(GameManager.Instance.currentLevelIndex);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;
        isQuizActive = false;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (screenFadeImage != null)
        {
            float elapsed = 0f;
            float duration = 1.0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                screenFadeImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            screenFadeImage.color = new Color(0f, 0f, 0f, 0f);
            screenFadeImage.raycastTarget = false;
        }
    }

    private void PlayTickSound()
    {
        if (uiAudioSource == null) return;
        int sampleRate = 44100;
        float duration = 0.05f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * 800f * t) * Mathf.Max(0, 1 - (t / duration)) * 0.15f;
        }
        AudioClip clip = AudioClip.Create("Tick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        uiAudioSource.PlayOneShot(clip);
    }

    private AudioClip CreateSuccessChimeClip()
    {
        int sampleRate = 44100;
        float duration = 1.2f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            
            float amplitude1 = Mathf.Max(0, 1 - (t / 0.4f)) * 0.25f;
            float wave1 = Mathf.Sin(2 * Mathf.PI * 523.25f * t); // C5

            float amplitude2 = t > 0.15f ? Mathf.Max(0, 1 - ((t - 0.15f) / 0.4f)) * 0.25f : 0f;
            float wave2 = Mathf.Sin(2 * Mathf.PI * 659.25f * t); // E5

            float amplitude3 = t > 0.3f ? Mathf.Max(0, 1 - ((t - 0.3f) / 0.4f)) * 0.25f : 0f;
            float wave3 = Mathf.Sin(2 * Mathf.PI * 783.99f * t); // G5

            float amplitude4 = t > 0.45f ? Mathf.Max(0, 1 - ((t - 0.45f) / 0.6f)) * 0.25f : 0f;
            float wave4 = Mathf.Sin(2 * Mathf.PI * 1046.50f * t); // C6

            float sparkle = 0f;
            if (t > 0.2f)
            {
                float sparkleFreq = 2000f + Mathf.PingPong(t * 8000f, 2000f);
                sparkle = Mathf.Sin(2 * Mathf.PI * sparkleFreq * t) * Mathf.Max(0, 1 - ((t - 0.2f) / 0.9f)) * 0.05f;
            }

            samples[i] = (wave1 * amplitude1 + wave2 * amplitude2 + wave3 * amplitude3 + wave4 * amplitude4) + sparkle;
        }

        AudioClip clip = AudioClip.Create("SuccessChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateErrorBuzzerClip()
    {
        int sampleRate = 44100;
        float duration = 1.0f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            float freq = 130f;
            float squareWave = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * freq * t));
            float amplitude1 = Mathf.Max(0, 1 - (t / 0.4f)) * 0.3f;

            float sweepFreq = Mathf.Lerp(400f, 80f, t);
            float sweepWave = Mathf.Sin(2 * Mathf.PI * sweepFreq * t);
            float amplitude2 = Mathf.Max(0, 1 - (t / 0.8f)) * 0.25f;

            samples[i] = (squareWave * amplitude1) + (sweepWave * amplitude2);
        }

        AudioClip clip = AudioClip.Create("ErrorBuzzer", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private TextMeshProUGUI CreateText(GameObject parent, string name, string initialText, int fontSize, Vector2 position, TMP_FontAsset font, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400f, 50f);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = initialText;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        if (font != null) tmp.font = font;

        return tmp;
    }

    private GameObject CreateButton(GameObject parent, string name, string label, Vector2 position, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.35f, 1f); // Dark buttons

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        // Text child
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpText = txtObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = label;
        tmpText.fontSize = 14;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.raycastTarget = false; // Let clicks pass through to the button background
        if (defaultFont != null) tmpText.font = defaultFont;

        return btnObj;
    }

    private void ConfigureHUDText(TextMeshProUGUI tmp, float yOffset)
    {
        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(15f, yOffset);
        rect.sizeDelta = new Vector2(-30f, 35f); // 15px margins on left/right
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }
}
