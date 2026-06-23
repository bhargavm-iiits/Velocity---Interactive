using UnityEngine;

public class Level3Key : MonoBehaviour
{
    [Header("Visual Effects")]
    public ParticleSystem pickupParticles;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectKey();
        }
    }

    private void CollectKey()
    {
        Debug.Log("Ancient Key collected!");

        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.ShowNotification("Obtained Ancient Key!\nUnlock Treasure Chest 3.");
        }

        if (pickupParticles != null)
        {
            ParticleSystem p = Instantiate(pickupParticles, transform.position, Quaternion.identity);
            Destroy(p.gameObject, 2f);
        }

        // Add score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(50);
        }

        // Disable cave entrance obstacle so player can go to the chest room
        LevelManager lm = GameManager.Instance.levelManager;
        if (lm != null && lm.caveEntranceObstacle != null)
        {
            lm.caveEntranceObstacle.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}
