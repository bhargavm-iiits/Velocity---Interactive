using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    public float baseWalkSpeedMPS = 4f;
    public float sprintSpeedMPS = 10f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    [Header("Compass Model")]
    public GameObject compassPrefab;

    [Header("VR Settings")]
    public Transform vrCameraTransform; // If in VR, camera determines forward direction

    [Header("State Variables")]
    public float currentSpeedMPS;
    public string currentDirectionString = "North";
    [HideInInspector] public bool movementFrozen = false;
    [HideInInspector] public bool isMovingInWrongDirection = false;
    
    // 3D Direction Scope Arrow container
    private Transform arrowContainer;
    private Transform compassTransform;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 0.15f;
    private float xRotation = 0f;
    
    // Wind force applied externally (e.g. Level 5 storm)
    [HideInInspector] public Vector3 windForce = Vector3.zero;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;

    // Input actions (fallback to keyboard/gamepad if no custom input action setup is bound)
    private Vector2 moveInput;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.stepOffset = 0.5f; // Ensure player can step up temple stairs smoothly
        }

        // Remove duplicate CapsuleCollider to prevent character controller physics conflicts
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            Destroy(cap);
            Debug.Log("PlayerController: Destroyed duplicate CapsuleCollider component to prevent physics conflicts.");
        }

        // Fix all SignBoard colliders in the scene to compensate for scale 100
        BoxCollider[] allBoxColliders = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
        foreach (var bc in allBoxColliders)
        {
            if (bc.gameObject.name.Contains("SignBoard") && bc.transform.localScale.x > 50f)
            {
                Vector3 scale = bc.transform.localScale;
                if (bc.size.x > 0.5f)
                {
                    bc.center = new Vector3(bc.center.x / scale.x, bc.center.y / scale.y, bc.center.z / scale.z);
                    bc.size = new Vector3(bc.size.x / scale.x, bc.size.y / scale.y, bc.size.z / scale.z);
                    Debug.Log($"PlayerController: Fixed scale of signboard collider: {bc.gameObject.name} to center={bc.center}, size={bc.size}");
                }
            }
        }
    }

    private void Start()
    {
        if (Cursor.lockState != CursorLockMode.Locked && (GameUIController.Instance == null || !GameUIController.Instance.selectionPanel.activeSelf))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Spawn the floating 3D navigation arrow in front of the camera
        CreateDirectionScopeArrow();
    }

    private void Update()
    {
        // Stuck Diagnostics
        if (Time.frameCount % 30 == 0)
        {
            try
            {
                string report = $"Frame {Time.frameCount} stuck check:\n";
                report += $"Player Pos: {transform.position}, speed: {currentSpeedMPS:F2}\n";
                Vector3 p1 = transform.position + Vector3.up * 0.5f;
                Vector3 p2 = transform.position + Vector3.up * 1.5f;
                Collider[] colls = Physics.OverlapCapsule(p1, p2, 0.45f);
                report += $"Overlapped Colliders ({colls.Length}):\n";
                foreach (var c in colls)
                {
                    report += $"- {c.name} (Object: {c.gameObject.name}, Tag: {c.tag}, Layer: {c.gameObject.layer}, isTrigger: {c.isTrigger})\n";
                }
                System.IO.File.WriteAllText("player_overlap_debug.txt", report);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Stuck check failed: {ex}");
            }
        }

        // Grounded check
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // Mouse Look
        HandleMouseLook();

        // Update direction arrow orientation in world space
        UpdateDirectionScopeRotation();

        // Read Inputs
        ReadInput();

        // Calculate move direction relative to camera (Desktop & VR compatible)
        Transform referenceTransform = vrCameraTransform != null ? vrCameraTransform : Camera.main.transform;
        Vector3 forward = referenceTransform.forward;
        Vector3 right = referenceTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // Determine Speed in m/s
        float targetSpeedMPS = isSprinting ? sprintSpeedMPS : baseWalkSpeedMPS;

        // Check if moving in correct direction
        bool correctDirection = true;
        if (GameManager.Instance != null)
        {
            string targetDir = GameManager.Instance.GetCurrentCorrectDirection(transform.position);
            
            if (moveDirection.magnitude > 0.05f && !string.IsNullOrEmpty(targetDir))
            {
                // Classify moveDirection to cardinal
                float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                string inputDirStr = "North";
                if (angle >= 315f || angle < 45f) inputDirStr = "North";
                else if (angle >= 45f && angle < 135f) inputDirStr = "East";
                else if (angle >= 135f && angle < 225f) inputDirStr = "South";
                else inputDirStr = "West";

                if (!inputDirStr.Equals(targetDir, System.StringComparison.OrdinalIgnoreCase))
                {
                    correctDirection = false;
                }
            }
        }

        isMovingInWrongDirection = !correctDirection && moveDirection.magnitude > 0.05f;

        float speedMPS = targetSpeedMPS;
        if (!correctDirection)
        {
            speedMPS = targetSpeedMPS * 0.2f; // Slow down to 20% speed as a penalty
        }

        // Apply movement
        Vector3 movement = moveDirection * speedMPS;

        // Apply wind force (Level 5 Storm effect)
        movement += windForce;

        controller.Move(movement * Time.deltaTime);

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // Track Current Speed and Direction for Educational Feedback
        UpdateVelocityState(movement);
    }

    private void ReadInput()
    {
        // 1. New Input System keyboard/gamepad reads
        moveInput = Vector2.zero;
        isSprinting = false;

        if (movementFrozen) return;

        // Keyboard WASD / Arrow keys
        if (Keyboard.current != null)
        {
            float x = 0;
            float y = 0;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1;
            moveInput = new Vector2(x, y);

            if (Keyboard.current.shiftKey.isPressed)
            {
                isSprinting = true;
            }

            // Reset level to checkpoint
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.HandleWrongChoice("Manual Reset");
                }
            }

            // Toggle compass
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (CompassSystem.Instance != null)
                {
                    CompassSystem.Instance.ToggleCompass();
                }
            }

            // Toggle 3D compass
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                Toggle3DCompass();
            }

            // Toggle cursor lock with Escape
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        // VR Controller or Gamepad fallback joysticks
        if (Gamepad.current != null && moveInput == Vector2.zero)
        {
            moveInput = Gamepad.current.leftStick.ReadValue();
            if (Gamepad.current.rightTrigger.isPressed || Gamepad.current.buttonEast.isPressed)
            {
                isSprinting = true;
            }
        }

        // Handle Jump
        if (isGrounded)
        {
            bool jumpPressed = false;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressed = true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;

            if (jumpPressed)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }

    private void UpdateVelocityState(Vector3 movement)
    {
        // Current speed is the magnitude of the horizontal movement in km/h
        Vector3 horizontalVel = new Vector3(movement.x, 0, movement.z);
        currentSpeedMPS = horizontalVel.magnitude;

        if (horizontalVel.magnitude > 0.05f)
        {
            // Determine Direction: North = (0,0,1), East = (1,0,0), etc.
            float angle = Mathf.Atan2(horizontalVel.x, horizontalVel.z) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 315f || angle < 45f) currentDirectionString = "North";
            else if (angle >= 45f && angle < 135f) currentDirectionString = "East";
            else if (angle >= 135f && angle < 225f) currentDirectionString = "South";
            else currentDirectionString = "West";
        }
        else
        {
            // When stationary, current speed is 0
            currentSpeedMPS = 0f;
        }
    }

    public void ResetToPosition(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        playerVelocity = Vector3.zero;
        controller.enabled = true;
        Debug.Log($"Player reset to position: {position}");
    }

    private void HandleMouseLook()
    {
        // Click to lock cursor back during gameplay (ignore clicks over UI elements)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (!isOverUI && Cursor.lockState != CursorLockMode.Locked && 
                (GameUIController.Instance == null || (!GameUIController.Instance.selectionPanel.activeSelf && !GameUIController.Instance.IsQuizActive)))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.sqrMagnitude > 0.0001f)
            {
                // Rotate player body horizontally (Y-axis)
                transform.Rotate(Vector3.up * (mouseDelta.x * mouseSensitivity));

                // Rotate camera vertically (X-axis)
                xRotation -= mouseDelta.y * mouseSensitivity;
                xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Limit vertical look

                Transform camTransform = vrCameraTransform != null ? vrCameraTransform : Camera.main.transform;
                if (camTransform != null)
                {
                    camTransform.localEulerAngles = new Vector3(xRotation, 0f, 0f);
                }
            }
        }
    }

    private void CreateDirectionScopeArrow()
    {
        Transform parentCam = vrCameraTransform != null ? vrCameraTransform : Camera.main.transform;
        if (parentCam == null) return;

        // Container object
        GameObject containerObj = new GameObject("DirectionScopeArrow");
        arrowContainer = containerObj.transform;
        arrowContainer.SetParent(parentCam, false);
        arrowContainer.localPosition = new Vector3(0f, 0f, 0.45f); // Center of the screen

        if (compassPrefab != null)
        {
            GameObject compassInstance = Instantiate(compassPrefab, arrowContainer);
            
            // Destroy all colliders on the compass to prevent player collision conflicts
            Collider[] colls = compassInstance.GetComponentsInChildren<Collider>(true);
            foreach (var c in colls)
            {
                Destroy(c);
            }

            compassTransform = compassInstance.transform;
            float scale = 0.006f; // Scale it down from 17.93m to ~10.8cm
            compassInstance.transform.localScale = new Vector3(scale, scale, scale);
            // Offset by the mesh bounds center (0.09, 1.27, 1.20) to center it visually
            compassInstance.transform.localPosition = new Vector3(-0.09f, -1.27f, -1.20f) * scale;
            // Tilt slightly (45 degrees) towards camera relative to the default imported rotation (270, 0, 0)
            // 270 - 45 = 225 degrees around X. Rotate 180 around Z to orient North (red tip) to the top.
            compassInstance.transform.localRotation = Quaternion.Euler(225f, 0f, 180f);
        }
        else
        {
            // Shaft (Cylinder)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(arrowContainer, false);
            shaft.transform.localPosition = Vector3.zero;
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point forward along Z
            shaft.transform.localScale = new Vector3(0.02f, 0.12f, 0.02f);

            Renderer shaftRenderer = shaft.GetComponent<Renderer>();
            if (shaftRenderer != null)
            {
                shaftRenderer.material = new Material(Shader.Find("Sprites/Default"));
                shaftRenderer.material.color = Color.cyan;
            }

            // Head (Cube)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(arrowContainer, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.14f); // Tip of shaft
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

            Renderer headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null)
            {
                headRenderer.material = new Material(Shader.Find("Sprites/Default"));
                headRenderer.material.color = Color.yellow;
            }
        }

        // Hide by default
        arrowContainer.gameObject.SetActive(false);
    }

    private void UpdateDirectionScopeRotation()
    {
        if (arrowContainer == null) return;

        // Keep arrowContainer locked to the camera (so it doesn't tilt or move vertically relative to the screen)
        arrowContainer.localRotation = Quaternion.identity;

        if (compassTransform != null)
        {
            // Get player's horizontal heading angle relative to World North
            Transform camTransform = vrCameraTransform != null ? vrCameraTransform : Camera.main.transform;
            if (camTransform != null)
            {
                Vector3 camForward = camTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                float headingAngle = Vector3.SignedAngle(Vector3.forward, camForward, Vector3.up);

                // Apply rotation around the compass's local Z-axis (normal of the dial)
                // and then tilt it 45 degrees towards the camera (225 degrees around X)
                // Offset by 180 degrees to align North (red needle) to the top of the compass.
                compassTransform.localRotation = Quaternion.Euler(225f, 0f, 0f) * Quaternion.Euler(0f, 0f, headingAngle + 180f);
            }
        }
    }

    public void Toggle3DCompass()
    {
        if (arrowContainer != null)
        {
            bool nextState = !arrowContainer.gameObject.activeSelf;
            arrowContainer.gameObject.SetActive(nextState);
            Debug.Log($"3D Compass visibility toggled to: {nextState}");
        }
    }
}
