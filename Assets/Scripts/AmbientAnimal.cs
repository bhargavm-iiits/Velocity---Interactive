using System.Collections;
using UnityEngine;

public class AmbientAnimal : MonoBehaviour
{
    [Header("Wander Settings")]
    public float walkSpeed = 5f;
    public float wanderRadius = 25f;

    private Animator animator;
    private Vector3 spawnPosition;
    private Vector3 currentTarget;
    private Terrain terrain;

    private void Start()
    {
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        terrain = Terrain.activeTerrain;
        if (terrain == null) terrain = FindFirstObjectByType<Terrain>();

        // Ensure Rigidbody exists for physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = 50f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = true; // Kinematic allows smooth programmatic movement while interacting with the physics system

        StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // Choose a target position (or run away if player is near)
            Vector3 targetPos;
            float currentSpeed = walkSpeed;

            if (PlayerController.Instance != null && Vector3.Distance(transform.position, PlayerController.Instance.transform.position) < 10f)
            {
                // Run away randomly 100m away in a direction away from the player
                Vector3 fleeDir = (transform.position - PlayerController.Instance.transform.position);
                fleeDir.y = 0f;
                if (fleeDir.magnitude < 0.1f) fleeDir = transform.forward;
                fleeDir.Normalize();

                // Add random variance to the escape path (+/- 45 degrees)
                float angleOffset = Random.Range(-45f, 45f);
                fleeDir = Quaternion.Euler(0f, angleOffset, 0f) * fleeDir;

                targetPos = transform.position + fleeDir * 100f;
                currentSpeed = 8f; // Fleeing speed
                
                Debug.Log($"{gameObject.name} is fleeing from player to {targetPos}!");
            }
            else
            {
                // Normal wander
                Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
                targetPos = spawnPosition + new Vector3(randCircle.x, 0f, randCircle.y);
                currentSpeed = walkSpeed;
            }

            // Project target onto terrain height
            if (terrain != null)
            {
                float height = terrain.SampleHeight(targetPos);
                targetPos.y = height + terrain.transform.position.y;
            }
            else
            {
                targetPos.y = spawnPosition.y;
            }

            currentTarget = targetPos;

            if (animator != null)
            {
                animator.Play("Walk"); // Continuously play Walk animation
            }

            // Move to target
            float elapsed = 0f;
            float timeout = 25f; // prevent getting stuck indefinitely (longer timeout for 100m runaway)
            while (Vector3.Distance(transform.position, currentTarget) > 1.0f && elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                // Check if player comes near while walking, to trigger another runaway immediately!
                if (currentSpeed == walkSpeed && PlayerController.Instance != null && Vector3.Distance(transform.position, PlayerController.Instance.transform.position) < 10f)
                {
                    break; // Break the current wander movement loop to recalculate target as runaway!
                }

                Vector3 dir = (currentTarget - transform.position);
                dir.y = 0f;
                if (dir.magnitude > 0.1f)
                {
                    transform.Translate(dir.normalized * currentSpeed * Time.deltaTime, Space.World);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 5f);
                }

                // Keep strictly on ground
                if (terrain != null)
                {
                    Vector3 pos = transform.position;
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    transform.position = pos;
                }

                yield return null;
            }

            // Continuously animated and rigged without stopping (no idle pauses!)
            yield return null;
        }
    }
}
