using UnityEngine;

public class WindZoneEffect : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector3 baseWindDirection = new Vector3(1f, 0f, 0.5f);
    public float windStrength = 2f;
    public float gustFrequency = 0.5f;

    private void Update()
    {
        if (GameManager.Instance == null || PlayerController.Instance == null) return;

        // Reset wind force (no wind applied in Sanctuary of Light)
        PlayerController.Instance.windForce = Vector3.zero;
    }
}
