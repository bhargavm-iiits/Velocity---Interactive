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

        // Only apply wind during Level 5
        if (GameManager.Instance.currentLevelIndex == 5)
        {
            // Simulate wind gusts using Perlin noise
            float noise = Mathf.PerlinNoise(Time.time * gustFrequency, 0f);
            float currentStrength = windStrength * noise;
            
            Vector3 appliedWind = baseWindDirection.normalized * currentStrength;
            PlayerController.Instance.windForce = appliedWind;
        }
        else
        {
            // Reset wind force
            PlayerController.Instance.windForce = Vector3.zero;
        }
    }
}
