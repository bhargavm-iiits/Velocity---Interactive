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
        SetCompassVisible(false); // Hidden by default at game start
    }

    private void Update()
    {
        Vector3 targetPos = GetCurrentTargetPosition();
        if (targetPos == Vector3.zero)
        {
            // Hide compass container and wrist arrow when no active target/collected
            var container = GameUIController.Instance != null ? GameUIController.Instance.compassContainer : null;
            if (container != null && container.activeSelf)
            {
                container.SetActive(false);
            }
            if (vrWristArrow != null && vrWristArrow.gameObject.activeSelf)
            {
                vrWristArrow.gameObject.SetActive(false);
            }
            return;
        }

        Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

        // Vector to target
        Vector3 dirToTarget = (targetPos - playerPos);
        dirToTarget.y = 0; // Flat projection
        dirToTarget.Normalize();

        // 1. Desktop Screen Arrow rotation
        var arrow = GameUIController.Instance != null ? GameUIController.Instance.compassArrowRect : null;
        if (arrow != null && isCompassVisible)
        {
            // Point the arrow in the absolute world direction of the target (independent of player's camera look rotation)
            float angle = Vector3.SignedAngle(Vector3.forward, dirToTarget, Vector3.up);
            arrow.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        // 2. VR 3D Wrist Arrow rotation
        if (vrWristArrow != null && isCompassVisible)
        {
            // Point the Z-axis of the arrow towards the target
            vrWristArrow.LookAt(new Vector3(targetPos.x, vrWristArrow.position.y, targetPos.z));
        }
    }

    public void ToggleCompass()
    {
        SetCompassVisible(!isCompassVisible);
    }

    public void SetCompassVisible(bool visible)
    {
        isCompassVisible = visible;
        var container = GameUIController.Instance != null ? GameUIController.Instance.compassContainer : null;
        if (container != null)
        {
            container.SetActive(visible);
        }
        if (vrWristArrow != null)
        {
            vrWristArrow.gameObject.SetActive(visible);
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
                if (lm.level1Chest != null && lm.level1Chest.activeSelf)
                {
                    TreasureChest tc = lm.level1Chest.GetComponent<TreasureChest>();
                    if (tc != null && !tc.isOpened) return lm.level1Chest.transform.position;
                }
                break;
            case 2:
                if (lm.woodCollectible != null && lm.woodCollectible.activeSelf) return lm.woodCollectible.transform.position;
                if (lm.stoneCollectible != null && lm.stoneCollectible.activeSelf) return lm.stoneCollectible.transform.position;
                if (lm.level2Chest != null && lm.level2Chest.activeSelf)
                {
                    TreasureChest tc = lm.level2Chest.GetComponent<TreasureChest>();
                    if (tc != null && !tc.isOpened) return lm.level2Chest.transform.position;
                }
                break;
            case 3:
                if (lm.level3Chest != null && lm.level3Chest.activeSelf)
                {
                    TreasureChest tc = lm.level3Chest.GetComponent<TreasureChest>();
                    if (tc != null && !tc.isOpened) return lm.level3Chest.transform.position;
                }
                break;
            case 4:
                if (lm.level4Chest != null && lm.level4Chest.activeSelf)
                {
                    TreasureChest tc = lm.level4Chest.GetComponent<TreasureChest>();
                    if (tc != null && !tc.isOpened) return lm.level4Chest.transform.position;
                }
                break;
            case 5:
                if (lm.level5Chest != null && lm.level5Chest.activeSelf)
                {
                    TreasureChest tc = lm.level5Chest.GetComponent<TreasureChest>();
                    if (tc != null && !tc.isOpened) return lm.level5Chest.transform.position;
                }
                break;
        }

        return Vector3.zero;
    }
}
