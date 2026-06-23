using UnityEngine;

public class GameManagerCheckpointHelper : MonoBehaviour
{
    [Header("Checkpoint Tracking")]
    public Transform lastClearedCheckpoint;

    private void Start()
    {
        // Initialize to activeCheckpoint if not set
        if (lastClearedCheckpoint == null && GameManager.Instance != null)
        {
            lastClearedCheckpoint = GameManager.Instance.activeCheckpoint;
        }
    }
}
