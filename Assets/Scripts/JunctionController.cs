using UnityEngine;

public class JunctionController : MonoBehaviour
{
    [Header("Required Velocity Solution")]
    public string requiredDirection = "North"; // "North", "South", "East", "West"
    public float requiredSpeed = 5f; // km/h

    [Header("Checkpoint To Set On Success")]
    public Transform successCheckpoint;

    [Header("Dead End Blockers")]
    public GameObject correctPathBlocker; // Optional path blockers that open on correct choice
    public GameObject incorrectPathBlockers;

    private bool isSolved = false;

    private void Start()
    {
        if (correctPathBlocker != null) correctPathBlocker.SetActive(true);
        if (incorrectPathBlockers != null) incorrectPathBlockers.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSolved) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered Junction trigger: {gameObject.name}");
            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.ShowVelocityPanel(this);
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
            }

            // Open correct path
            if (correctPathBlocker != null)
            {
                correctPathBlocker.SetActive(false);
            }

            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.ShowNotification("Correct Velocity Selected!\nProceeding Forward");
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
