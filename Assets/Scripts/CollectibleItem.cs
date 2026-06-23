using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public enum CollectibleType { Wood, FireStone }
    public CollectibleType type;

    [Header("Visual Effects")]
    public ParticleSystem pickupParticles;

    private static bool collectedWood = false;
    private static bool collectedStone = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        Debug.Log($"Collected item: {type}");

        if (type == CollectibleType.Wood)
        {
            collectedWood = true;
            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.ShowNotification("Collected Wood!\nNow find the Fire Stone.");
            }
        }
        else if (type == CollectibleType.FireStone)
        {
            collectedStone = true;
            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.ShowNotification("Collected Fire Stone!\nProceed to Campfire site.");
            }
        }

        if (pickupParticles != null)
        {
            ParticleSystem p = Instantiate(pickupParticles, transform.position, Quaternion.identity);
            Destroy(p.gameObject, 2f);
        }

        // Add score for collection
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(25);
        }

        // Check if both collected
        if (collectedWood && collectedStone)
        {
            TriggerCampfireCreation();
        }

        gameObject.SetActive(false);
    }

    private void TriggerCampfireCreation()
    {
        Debug.Log("Campfire creation triggered!");
        
        LevelManager lm = GameManager.Instance.levelManager;
        if (lm != null)
        {
            if (lm.campfireSite != null)
            {
                lm.campfireSite.SetActive(true);
                // Turn on campfire particle fire
                ParticleSystem fire = lm.campfireSite.GetComponentInChildren<ParticleSystem>();
                if (fire != null) fire.Play();
            }

            // Reveal Treasure Chest 2
            if (lm.level2Chest != null)
            {
                lm.level2Chest.SetActive(true);
                if (GameUIController.Instance != null)
                {
                    GameUIController.Instance.ShowNotification("Campfire lit!\nTreasure Chest 2 Location Revealed.");
                }
            }
        }
    }

    // Call this when level resets or starts
    public static void ResetCollectionState()
    {
        collectedWood = false;
        collectedStone = false;
    }
}
