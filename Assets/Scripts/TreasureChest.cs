using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class TreasureChest : MonoBehaviour
{
    [Header("Chest Properties")]
    public int levelChestIndex = 1;
    public Transform chestLid; // Rotate this to open
    public float openAngle = -80f;

    [Header("Visual Effects")]
    public ParticleSystem goldenGlowParticles;

    [Header("Reward Properties")]
    public GameObject[] rewardItems;
    public float rewardRiseHeight = 1.2f;
    public float rewardRotateSpeed = 90f;
    public ParticleSystem openExplosionParticles; // Coins, gems, fireworks
    public AudioSource victoryAudio;

    public bool isOpened = false;
    private bool playerInRange = false;
    private bool materialsConverted = false;
    private Renderer[] chestRenderers;

    private void Start()
    {
        chestRenderers = GetComponentsInChildren<Renderer>(true);

        if (goldenGlowParticles != null)
        {
            goldenGlowParticles.Play();
        }

        // Force enable all non-reward and non-particle renderers
        foreach (var r in chestRenderers)
        {
            if (r != null && r is not ParticleSystemRenderer)
            {
                bool isReward = false;
                if (rewardItems != null)
                {
                    foreach (var reward in rewardItems)
                    {
                        if (reward != null && r.transform.IsChildOf(reward.transform))
                        {
                            isReward = true;
                            break;
                        }
                    }
                }
                if (!isReward)
                {
                    r.enabled = true;
                }
            }
        }

        // Force apply beautiful procedural materials to ensure zero pink/missing shader rendering
        ApplyPremiumMaterials();

        // Ensure scale is boosted for visibility
        Transform modelTransform = null;
        foreach (Transform child in transform)
        {
            if (child.name.EndsWith("_Model"))
            {
                modelTransform = child;
                break;
            }
        }
        if (modelTransform != null)
        {
            modelTransform.localScale = Vector3.one * 25.0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.ShowInteractionPrompt("OPEN TREASURE CHEST\nPress E / Use Trigger");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.HideInteractionPrompt();
            }
        }
    }

    private void Update()
    {
        if (!materialsConverted)
        {
            bool hasCustomModel = false;
            foreach (Transform child in transform)
            {
                if (child.name.EndsWith("_Model"))
                {
                    hasCustomModel = true;
                    break;
                }
            }

            if (!hasCustomModel && transform.Find("ChestBase") != null)
            {
                ApplyPremiumMaterials();
            }
            materialsConverted = true;
        }

        // Distance check for visibility
        float visibilityLimit = GetVisibilityLimit();
        if (visibilityLimit > 0f)
        {
            Transform player = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
            bool withinLimit = false;
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                withinLimit = dist <= visibilityLimit;
            }
            
            if (chestRenderers == null) chestRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in chestRenderers)
            {
                if (r != null)
                {
                    // Check if this renderer is part of the reward items
                    bool isReward = false;
                    if (rewardItems != null)
                    {
                        foreach (var reward in rewardItems)
                        {
                            if (reward != null && r.transform.IsChildOf(reward.transform))
                            {
                                isReward = true;
                                break;
                            }
                        }
                    }

                    if (!isReward && r is not ParticleSystemRenderer)
                    {
                        r.enabled = withinLimit;
                    }
                }
            }

            // Also manage particles visibility
            if (goldenGlowParticles != null)
            {
                if (withinLimit && !isOpened)
                {
                    if (!goldenGlowParticles.isPlaying) goldenGlowParticles.Play();
                }
                else
                {
                    if (goldenGlowParticles.isPlaying) goldenGlowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            // Manage glow light visibility
            Light glowLight = GetComponentInChildren<Light>(true);
            if (glowLight != null)
            {
                glowLight.enabled = withinLimit && !isOpened;
            }
        }

        if (isOpened) return;
        if (GameUIController.Instance != null && GameUIController.Instance.IsQuizActive) return;

        // Check for interaction
        if (playerInRange)
        {
            bool interactPressed = false;
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            // VR interaction fallback: trigger / grip or joystick press
            if (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            if (interactPressed)
            {
                OpenChest();
            }
        }
    }

    public void OpenChest()
    {
        if (isOpened) return;
        isOpened = true;

        Debug.Log($"Opening Treasure Chest {levelChestIndex}");

        // Hide prompt and show level complete pop-up notification immediately
        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.HideInteractionPrompt();
            string concept = "";
            if (GameManager.Instance != null)
            {
                concept = "\nConcept Mastered: " + GameManager.Instance.GetConceptLearnedText(levelChestIndex);
            }
            GameUIController.Instance.ShowNotification($"LEVEL {levelChestIndex} COMPLETED!{concept}");
        }

        // Open animation: rotate lid
        if (chestLid != null)
        {
            chestLid.localRotation = Quaternion.Euler(openAngle, 0f, 0f);
        }

        // Stop glow, start explosion
        if (goldenGlowParticles != null) goldenGlowParticles.Stop();
        if (openExplosionParticles != null) openExplosionParticles.Play();

        // Audio
        if (victoryAudio != null) victoryAudio.Play();

        // Spawn/Animate Pop-up rewards
        if (rewardItems != null)
        {
            foreach (var item in rewardItems)
            {
                if (item != null)
                {
                    item.SetActive(true);
                    StartCoroutine(AnimateReward(item));
                }
            }
        }

        // Scoring
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(100); // +100 for treasure discovery
            StartCoroutine(WaitAndCompleteLevel());
        }
    }

    private IEnumerator WaitAndCompleteLevel()
    {
        yield return new WaitForSeconds(2.0f); // Wait for rise animation to finish
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }

    private void ApplyPremiumMaterials()
    {
        Shader targetShader = Shader.Find("Universal Render Pipeline/Lit");
        if (targetShader == null) targetShader = Shader.Find("Standard");
        if (targetShader == null) targetShader = Shader.Find("Mobile/Diffuse");

        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        if (targetShader != null)
        {
            // Create procedural materials
            Material woodMat = new Material(targetShader);
            woodMat.name = "ChestWood_Procedural";
            
            Material metalMat = new Material(targetShader);
            metalMat.name = "ChestMetal_Procedural";

            // Set colors
            if (woodMat.HasProperty("_BaseColor")) woodMat.SetColor("_BaseColor", new Color(0.35f, 0.20f, 0.08f));
            else if (woodMat.HasProperty("_Color")) woodMat.SetColor("_Color", new Color(0.35f, 0.20f, 0.08f));

            if (metalMat.HasProperty("_BaseColor")) metalMat.SetColor("_BaseColor", new Color(0.85f, 0.70f, 0.20f));
            else if (metalMat.HasProperty("_Color")) metalMat.SetColor("_Color", new Color(0.85f, 0.70f, 0.20f));

            // Set smoothness / metallic
            if (woodMat.HasProperty("_Smoothness")) woodMat.SetFloat("_Smoothness", 0.2f);
            else if (woodMat.HasProperty("_Glossiness")) woodMat.SetFloat("_Glossiness", 0.2f);
            if (woodMat.HasProperty("_Metallic")) woodMat.SetFloat("_Metallic", 0.0f);

            if (metalMat.HasProperty("_Smoothness")) metalMat.SetFloat("_Smoothness", 0.75f);
            else if (metalMat.HasProperty("_Glossiness")) metalMat.SetFloat("_Glossiness", 0.75f);
            if (metalMat.HasProperty("_Metallic")) metalMat.SetFloat("_Metallic", 0.9f);

            foreach (var r in rends)
            {
                // Skip particle systems or lights
                if (r is ParticleSystemRenderer) continue;

                // Skip reward items
                bool isReward = false;
                if (rewardItems != null)
                {
                    foreach (var reward in rewardItems)
                    {
                        if (reward != null && r.transform.IsChildOf(reward.transform))
                        {
                            isReward = true;
                            break;
                        }
                    }
                }
                if (isReward) continue;

                // Determine whether this part is metal or wood
                string objName = r.gameObject.name.ToLower();
                if (r.transform.parent != null)
                {
                    objName += "_" + r.transform.parent.name.ToLower();
                }

                bool isMetal = objName.Contains("lock") || 
                              objName.Contains("screw") || 
                              objName.Contains("handle") || 
                              objName.Contains("sphere") || 
                              objName.Contains("side") ||
                              objName.Contains("metal") ||
                              objName.Contains("hinge");

                Material targetMat = isMetal ? metalMat : woodMat;

                // Assign the premium material to all slots on this renderer
                Material[] newMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    newMats[i] = targetMat;
                }
                r.materials = newMats;
            }
            materialsConverted = true;
        }
        else
        {
            Debug.LogWarning("TreasureChest: No compatible shader found! Cannot apply premium materials.");
        }
    }

    private IEnumerator AnimateReward(GameObject item)
    {
        Vector3 startPos = item.transform.localPosition;
        Vector3 targetPos = startPos + Vector3.up * rewardRiseHeight;
        
        float duration = 2.0f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smooth step
            
            item.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            item.transform.Rotate(Vector3.up, rewardRotateSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }
        
        // Spin indefinitely
        while (true)
        {
            item.transform.Rotate(Vector3.up, rewardRotateSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    private float GetVisibilityLimit()
    {
        // Disable distance hiding so chests are always visible and active from the start of the level
        return -1f;
    }
}
