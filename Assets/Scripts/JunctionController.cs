using UnityEngine;

public class JunctionController : MonoBehaviour
{
    [Header("Required Velocity Solution")]
    public string requiredDirection = "North"; // "North", "South", "East", "West"
    public float requiredSpeed = 5f; // m/s

    [Header("Checkpoint To Set On Success")]
    public Transform successCheckpoint;

    [Header("Dead End Blockers")]
    public GameObject correctPathBlocker; // Optional path blockers that open on correct choice
    public GameObject incorrectPathBlockers;

    [Header("Guidepost Settings")]
    public bool isGuidepostOnly = false;
    public float distanceMilestone = 50f;

    private bool isSolved = false;

    private void Start()
    {
        if (correctPathBlocker != null) correctPathBlocker.SetActive(true);
        if (incorrectPathBlockers != null) incorrectPathBlockers.SetActive(false);

        // Dynamically style the visual helper at runtime to avoid edit-time material leaks or package modifications
        Transform visualHelper = transform.Find("JunctionVisualHelper");
        if (visualHelper != null)
        {
            Renderer rend = visualHelper.GetComponent<Renderer>();
            if (rend != null)
            {
                Material greenMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                greenMat.color = new Color(0f, 1f, 0f, 0.4f);
                if (greenMat.shader != null && greenMat.shader.name.Contains("Universal Render Pipeline"))
                {
                    greenMat.SetFloat("_Surface", 1); // Transparent
                    greenMat.SetFloat("_Blend", 0); // Alpha
                    greenMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    greenMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    greenMat.SetInt("_ZWrite", 0);
                    greenMat.DisableKeyword("_ALPHATEST_ON");
                    greenMat.EnableKeyword("_ALPHABLEND_ON");
                    greenMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    greenMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                else
                {
                    greenMat.SetFloat("_Mode", 3); // Transparent
                    greenMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    greenMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    greenMat.SetInt("_ZWrite", 0);
                    greenMat.DisableKeyword("_ALPHATEST_ON");
                    greenMat.EnableKeyword("_ALPHABLEND_ON");
                    greenMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    greenMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                rend.material = greenMat;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSolved) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered Junction trigger: {gameObject.name}");
            if (isGuidepostOnly)
            {
                isSolved = true;
                if (GameManager.Instance != null)
                {
                    Transform cp = successCheckpoint != null ? successCheckpoint : transform;
                    GameManager.Instance.SetCheckpoint(cp);
                }
                if (GameUIController.Instance != null)
                {
                    GameUIController.Instance.ShowNotification($"Checkpoint Reached!\n{distanceMilestone} m Covered");
                }
            }
            else
            {
                if (GameUIController.Instance != null)
                {
                    GameUIController.Instance.ShowVelocityPanel(this);
                }
                else
                {
                    ConfirmVelocitySelection(requiredDirection, requiredSpeed);
                }
            }
        }
    }

    public void ConfirmVelocitySelection(string selectedDirection, float selectedSpeed)
    {
        bool directionCorrect = selectedDirection.Equals(requiredDirection, System.StringComparison.OrdinalIgnoreCase);
        bool speedCorrect = Mathf.Approximately(selectedSpeed, requiredSpeed);

        if (directionCorrect && speedCorrect)
        {
            // Fully Correct Selection
            isSolved = true;
            Debug.Log("Velocity Selection: Correct!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(50); // +50 for direction
                GameManager.Instance.AddScore(50); // +50 for speed

                // Set new checkpoint
                if (successCheckpoint != null)
                {
                    GameManager.Instance.SetCheckpoint(successCheckpoint);
                }

                GameManager.Instance.StartLevelTimer();
            }

            // Open correct path
            if (correctPathBlocker != null)
            {
                correctPathBlocker.SetActive(false);
            }

            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.ShowNotification("Junction Cleared!\nProceeding Forward");
            }
        }
        else
        {
            // Incorrect Selection
            string errorReason = "";
            if (!directionCorrect && !speedCorrect) errorReason = "Wrong Direction & Speed";
            else if (!directionCorrect) errorReason = "Wrong Direction";
            else errorReason = "Wrong Speed";

            Debug.Log($"Velocity Selection: Incorrect! Reason: {errorReason}");

            // Trigger wrong path blocker (optional visual feedback)
            if (incorrectPathBlockers != null)
            {
                incorrectPathBlockers.SetActive(true);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.HandleWrongChoice(errorReason);
            }
        }
    }
}
