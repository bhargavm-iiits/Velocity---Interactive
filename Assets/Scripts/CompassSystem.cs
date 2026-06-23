using UnityEngine;

public class CompassSystem : MonoBehaviour
{
    public static CompassSystem Instance { get; private set; }

    [Header("VR Wrist Reference")]
    public Transform vrWristArrow; // 3D pointer on wrist

    private bool isCompassVisible = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        var container = GameUIController.Instance != null ? GameUIController.Instance.compassContainer : null;
        if (container != null)
        {
            container.SetActive(isCompassVisible);
        }
    }

    private void Update()
    {
        Vector3 targetPos = GetCurrentTargetPosition();
        if (targetPos == Vector3.zero) return;

        Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

        // Vector to target
        Vector3 dirToTarget = (targetPos - playerPos);
        dirToTarget.y = 0; // Flat projection
        dirToTarget.Normalize();

        // 1. Desktop Screen Arrow rotation
        var arrow = GameUIController.Instance != null ? GameUIController.Instance.compassArrowRect : null;
        if (arrow != null && isCompassVisible)
        {
            // Point the arrow relative to player camera forward
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            if (camTransform != null)
            {
                Vector3 camForward = camTransform.forward;
                camForward.y = 0;
                camForward.Normalize();

                float angle = Vector3.SignedAngle(camForward, dirToTarget, Vector3.up);
                arrow.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        // 2. VR 3D Wrist Arrow rotation
        if (vrWristArrow != null)
        {
            // Point the Z-axis of the arrow towards the target
            vrWristArrow.LookAt(new Vector3(targetPos.x, vrWristArrow.position.y, targetPos.z));
        }
    }

    public void ToggleCompass()
    {
        isCompassVisible = !isCompassVisible;
        var container = GameUIController.Instance != null ? GameUIController.Instance.compassContainer : null;
        if (container != null)
        {
            container.SetActive(isCompassVisible);
        }
    }

    private Vector3 GetCurrentTargetPosition()
    {
        if (GameManager.Instance == null || GameManager.Instance.levelManager == null) return Vector3.zero;
        
        LevelManager lm = GameManager.Instance.levelManager;
        int level = GameManager.Instance.currentLevelIndex;

        switch (level)
        {
            case 1:
                if (lm.level1Chest != null && lm.level1Chest.activeSelf) return lm.level1Chest.transform.position;
                break;
            case 2:
                // Level 2 has dynamic targets: first wood, then fire stone, then chest 2
                if (lm.woodCollectible != null && lm.woodCollectible.activeSelf) return lm.woodCollectible.transform.position;
                if (lm.stoneCollectible != null && lm.stoneCollectible.activeSelf) return lm.stoneCollectible.transform.position;
                if (lm.level2Chest != null && lm.level2Chest.activeSelf) return lm.level2Chest.transform.position;
                break;
            case 3:
                // Point to cave key or chest 3
                if (lm.level3Chest != null && lm.level3Chest.activeSelf) return lm.level3Chest.transform.position;
                break;
            case 4:
                if (lm.level4Chest != null && lm.level4Chest.activeSelf) return lm.level4Chest.transform.position;
                break;
            case 5:
                if (lm.level5Chest != null && lm.level5Chest.activeSelf) return lm.level5Chest.transform.position;
                break;
        }

        // Fallback: look for the active level chest
        return Vector3.zero;
    }
}
