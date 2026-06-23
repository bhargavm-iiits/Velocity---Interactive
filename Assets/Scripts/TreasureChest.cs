using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Header("Chest Properties")]
    public int levelChestIndex = 1;
    public Transform chestLid; // Rotate this to open
    public float openAngle = -80f;

    [Header("Visual Effects")]
    public ParticleSystem goldenGlowParticles;
    public ParticleSystem openExplosionParticles; // Coins, gems, fireworks
    public AudioSource victoryAudio;

    private bool isOpened = false;
    private bool playerInRange = false;
    private bool materialsConverted = false;

    private void Start()
    {
        if (goldenGlowParticles != null)
        {
            goldenGlowParticles.Play();
        }
        ConvertMaterialsToURP();
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
            ConvertMaterialsToURP();
            materialsConverted = true;
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

        // Hide prompt
        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.HideInteractionPrompt();
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

        // Scoring
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(100); // +100 for treasure discovery
            GameManager.Instance.CompleteLevel();
        }
    }

    private void ConvertMaterialsToURP()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader != null)
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                Material[] mats = r.materials;
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null)
                    {
                        if (mats[m].shader.name != "Universal Render Pipeline/Lit")
                        {
                            Material oldMat = mats[m];
                            Material newMat = new Material(urpLitShader);
                            newMat.name = oldMat.name + "_URP";

                            // Use mainTexture and color properties which are shader-independent
                            Color mainColor = new Color(0.45f, 0.28f, 0.15f); // default wood brown
                            try
                            {
                                mainColor = oldMat.color;
                            }
                            catch (System.Exception) {}

                            newMat.SetColor("_BaseColor", mainColor);

                            try
                            {
                                if (oldMat.mainTexture != null)
                                {
                                    newMat.SetTexture("_BaseMap", oldMat.mainTexture);
                                }
                            }
                            catch (System.Exception) {}

                            mats[m] = newMat;
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    r.materials = mats;
                }
            }
            materialsConverted = true;
        }
    }
}
