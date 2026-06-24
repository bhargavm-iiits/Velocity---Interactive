using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public int checkpointIndex = 1; // 1 = L1, 2 = L2, 3 = L3, 4 = L4, 5 = L5

    private bool isCleared = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCleared) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered Checkpoint {checkpointIndex} trigger!");
            // Checkpoints are now directly cleared without popping up quizzes
            OnQuizCleared();
        }
    }

    public void OnQuizCleared()
    {
        isCleared = true;
        Debug.Log($"Checkpoint {checkpointIndex} cleared successfully!");

        if (GameManager.Instance != null)
        {
            // Record this checkpoint as both the active and last cleared checkpoint
            GameManager.Instance.SetCheckpoint(transform);
            
            // Set last cleared checkpoint
            var helper = GameManager.Instance.GetComponent<GameManagerCheckpointHelper>();
            if (helper != null)
            {
                helper.lastClearedCheckpoint = transform;
            }
        }
    }

    public void OnQuizFailed()
    {
        Debug.Log($"Checkpoint {checkpointIndex} quiz failed! Respawning at last cleared checkpoint...");

        Transform respawnPoint = null;
        if (GameManager.Instance != null)
        {
            var helper = GameManager.Instance.GetComponent<GameManagerCheckpointHelper>();
            if (helper != null)
            {
                respawnPoint = helper.lastClearedCheckpoint;
            }

            if (respawnPoint == null)
            {
                respawnPoint = GameManager.Instance.activeCheckpoint;
            }
        }

        Vector3 pos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        Quaternion rot = respawnPoint != null ? respawnPoint.rotation : Quaternion.identity;

        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.StartCoroutine(GameUIController.Instance.FadeToBlackAndRespawn(pos, rot));
        }
    }
}
