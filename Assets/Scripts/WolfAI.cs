using UnityEngine;

public class WolfAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float detectionRange = 15f;
    public float attackRange = 2f;

    [Header("Patrol Area")]
    public Transform[] patrolWaypoints;
    
    private int currentWaypointIndex = 0;
    private Transform player;
    private Vector3 spawnPosition;
    private bool isChasing = false;

    private void Start()
    {
        spawnPosition = transform.position;
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
        else
        {
            player = GameObject.FindWithTag("Player")?.transform;
        }

        // Generate automatic patrol waypoints around spawn if none set
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
            GameObject wp1 = new GameObject(name + "_WP1");
            wp1.transform.position = spawnPosition + new Vector3(10f, 0f, 0f);
            GameObject wp2 = new GameObject(name + "_WP2");
            wp2.transform.position = spawnPosition + new Vector3(-10f, 0f, 0f);
            patrolWaypoints = new Transform[] { wp1.transform, wp2.transform };
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isChasing)
        {
            // Chase logic
            ChasePlayer();

            // If player gets far enough or resets, stop chasing
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                isChasing = false;
            }

            // Catch player
            if (distanceToPlayer <= attackRange)
            {
                CatchPlayer();
            }
        }
        else
        {
            // Patrol logic
            Patrol();

            // Detect player
            if (distanceToPlayer <= detectionRange)
            {
                // Wolf chases player if they are in range
                // Educational check: if player is sprinting at 6 m/s, they will outrun the wolves.
                // If they are walking slowly (e.g. 2 m/s), the wolves will detect and chase them!
                if (PlayerController.Instance.currentSpeedMPS < 5.0f)
                {
                    isChasing = true;
                }
            }
        }
    }

    private void Patrol()
    {
        if (patrolWaypoints.Length == 0) return;

        Transform targetWP = patrolWaypoints[currentWaypointIndex];
        Vector3 direction = (targetWP.position - transform.position);
        direction.y = 0;
        
        if (direction.magnitude < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
        }
        else
        {
            transform.Translate(direction.normalized * patrolSpeed * Time.deltaTime, Space.World);
            transform.LookAt(new Vector3(targetWP.position.x, transform.position.y, targetWP.position.z));
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position);
        direction.y = 0;

        transform.Translate(direction.normalized * chaseSpeed * Time.deltaTime, Space.World);
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    private void CatchPlayer()
    {
        isChasing = false;
        transform.position = spawnPosition; // Reset wolf pos
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleWrongChoice("Caught by wolves! Speed was insufficient.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
