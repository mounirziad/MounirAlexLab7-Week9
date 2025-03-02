using System.Collections; // Import the System.Collections namespace for coroutines
using System.Collections.Generic; // Import the System.Collections.Generic namespace for generic collections
using UnityEngine; // Import the Unity engine namespace
using UnityEngine.AI; // Import the Unity AI namespace for NavMeshAgent

public class AIScript : MonoBehaviour // Define the AIScript class that inherits from MonoBehaviour
{
    public NavMeshAgent navMeshAgent; // Reference to the NavMeshAgent component for AI navigation
    public float startWaitTime = 4; // Time to wait at each waypoint during patrol
    public float speedWalk = 6; // Walking speed during patrol
    public float speedRun = 9; // Running speed during chase

    public float viewRadius = 15; // Radius within which the AI can detect the player
    public float viewAngle = 90; // Field of view angle for player detection
    public LayerMask playerMask; // Layer mask to filter player objects
    public LayerMask obstacleMask; // Layer mask to filter obstacles

    public Transform[] waypoints; // Array of waypoints for patrol behavior

    // Public material for chase state
    public Material chaseMaterial; // Material to apply when chasing the player (assign in Inspector)
    private Material originalMaterial; // Stores the original material of the AI

    private int m_CurrentWaypointIndex; // Index of the current waypoint in the patrol route
    private Vector3 playerLastPosition = Vector3.zero; // Last known position of the player
    private Vector3 m_PlayerPosition; // Current position of the player

    private float m_WaitTime; // Timer for waiting at waypoints
    private bool m_PlayerInRange; // Flag to check if the player is within detection range
    private bool m_PlayerNear; // Flag to check if the player is near
    private bool m_IsPatrol; // Flag to check if the AI is in patrol mode
    private bool m_CaughtPlayer; // Flag to check if the player has been caught
    private bool isColliding = false; // Flag to check if the AI is currently colliding

    // Add a Renderer reference
    private Renderer aiRenderer; // Reference to the AI's renderer component

    void Start() // Called when the script is initialized
    {
        m_PlayerPosition = Vector3.zero; // Initialize player position to zero
        m_IsPatrol = true; // Start in patrol mode
        m_CaughtPlayer = false; // Player has not been caught initially
        m_PlayerInRange = false; // Player is not in range initially
        m_WaitTime = startWaitTime; // Initialize wait time

        m_CurrentWaypointIndex = 0; // Start at the first waypoint
        navMeshAgent = GetComponent<NavMeshAgent>(); // Get the NavMeshAgent component
        navMeshAgent.isStopped = false; // Ensure the agent is not stopped
        navMeshAgent.speed = speedWalk; // Set initial speed to walking speed
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Set initial destination
        navMeshAgent.baseOffset = 0.5f; // Adjust the base offset of the agent
        navMeshAgent.updateRotation = false; // Disable automatic rotation
        navMeshAgent.stoppingDistance = 0.5f; // Set stopping distance for the agent

        // Disable physics-based movement
        Rigidbody rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
        if (rb != null) // Check if Rigidbody exists
        {
            rb.isKinematic = true; // Set Rigidbody to kinematic to disable physics
        }

        // Adjust NavMeshAgent settings
        navMeshAgent.stoppingDistance = 0.5f; // Set stopping distance
        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // Set obstacle avoidance quality
        navMeshAgent.avoidancePriority = 50; // Set avoidance priority
        navMeshAgent.angularSpeed = 120f; // Set rotation speed
        navMeshAgent.acceleration = 8f; // Set acceleration

        aiRenderer = GetComponentInChildren<Renderer>(); // Get the Renderer component
        if (aiRenderer != null) // Check if Renderer exists
        {
            originalMaterial = aiRenderer.material; // Store the original material
        }
    }

    void Update() // Called once per frame
    {
        EnvironmentView(); // Check for player detection

        if (!m_IsPatrol) // If not in patrol mode, chase the player
        {
            Chasing();
        }
        else // Otherwise, patrol
        {
            Patroling();
        }

        RotateTowardsMovementDirection(); // Ensure proper rotation
    }

    private void Chasing() // Handles chasing behavior
    {
        if (!m_CaughtPlayer) // If the player hasn't been caught
        {
            Move(speedRun); // Move at running speed

            if (aiRenderer != null && chaseMaterial != null) // If renderer and chase material exist
            {
                aiRenderer.material = chaseMaterial; // Apply chase material
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Find the player object
            if (player != null) // If player exists
            {
                Debug.Log("Chasing player: " + player.transform.position); // Log player position
                navMeshAgent.SetDestination(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z)); // Set destination to player position

                // Check player distance continuously
                if (Vector3.Distance(transform.position, player.transform.position) >= 6f) // If player is out of range
                {
                    Debug.Log("Player out of range, returning to patrol"); // Log return to patrol
                    m_IsPatrol = true; // Switch to patrol mode
                    m_PlayerNear = false; // Player is no longer near
                    Move(speedWalk); // Move at walking speed
                    m_WaitTime = startWaitTime; // Reset wait time
                    navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Set destination to current waypoint

                    // Revert to the original material
                    if (aiRenderer != null && originalMaterial != null) // If renderer and original material exist
                    {
                        aiRenderer.material = originalMaterial; // Revert to original material
                    }
                }
            }
        }

        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) // If destination is reached
        {
            if (m_WaitTime <= 0 && !m_CaughtPlayer) // If wait time is over and player isn't caught
            {
                Stop(); // Stop moving
                m_WaitTime -= Time.deltaTime; // Decrease wait time
            }
        }
    }

    private void Patroling() // Handles patrol behavior
    {
        if (m_PlayerNear) // If player is near
        {
            Move(speedWalk); // Move at walking speed
            LookingPlayer(playerLastPosition); // Look towards the player's last position
        }
        else // If player is not near
        {
            m_PlayerNear = false; // Player is not near
            playerLastPosition = Vector3.zero; // Reset player's last position
            navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Set destination to current waypoint

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) // If destination is reached
            {
                if (m_WaitTime <= 0) // If wait time is over
                {
                    NextPoint(); // Move to the next waypoint
                    Move(speedWalk); // Move at walking speed
                    m_WaitTime = startWaitTime; // Reset wait time
                }
                else // If wait time is not over
                {
                    Stop(); // Stop moving
                    m_WaitTime -= Time.deltaTime; // Decrease wait time
                }
            }
        }
    }

    void Move(float speed) // Handles movement
    {
        navMeshAgent.isStopped = false; // Ensure the agent is not stopped
        navMeshAgent.speed = speed; // Set movement speed
    }

    void Stop() // Handles stopping
    {
        navMeshAgent.isStopped = true; // Stop the agent
        navMeshAgent.speed = 0; // Set speed to zero
    }

    public void NextPoint() // Moves to the next waypoint
    {
        m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length; // Increment waypoint index
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Set destination to next waypoint
    }

    void CaughtPlayer() // Handles catching the player
    {
        m_CaughtPlayer = true; // Set caught player flag
    }

    void LookingPlayer(Vector3 player) // Handles looking towards the player
    {
        navMeshAgent.SetDestination(new Vector3(player.x, transform.position.y, player.z)); // Set destination to player position

        if (Vector3.Distance(transform.position, player) <= 0.3) // If close to the player
        {
            if (m_WaitTime <= 0) // If wait time is over
            {
                m_PlayerNear = false; // Player is no longer near
                Move(speedWalk); // Move at walking speed
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Set destination to current waypoint
                m_WaitTime = startWaitTime; // Reset wait time
            }
            else // If wait time is not over
            {
                Stop(); // Stop moving
                m_WaitTime -= Time.deltaTime; // Decrease wait time
            }
        }
    }

    void EnvironmentView() // Handles player detection
    {
        Collider[] targetsInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask | LayerMask.GetMask("canPickUp")); // Detect objects within view radius

        Transform closestTarget = null; // Initialize closest target
        float closestDistance = Mathf.Infinity; // Initialize closest distance

        foreach (Collider target in targetsInRange) // Iterate through detected objects
        {
            Transform targetTransform = target.transform; // Get target's transform
            Vector3 dirToTarget = (targetTransform.position - transform.position).normalized; // Calculate direction to target
            float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position); // Calculate distance to target

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2) // Check if target is within view angle
            {
                if (!Physics.Raycast(transform.position, dirToTarget, distanceToTarget, obstacleMask)) // Check if target is not blocked by obstacles
                {
                    if (distanceToTarget < closestDistance) // Check if target is the closest
                    {
                        closestDistance = distanceToTarget; // Update closest distance
                        closestTarget = targetTransform; // Update closest target
                    }
                }
            }
        }

        if (closestTarget != null) // If a target is detected
        {
            Debug.Log("Player detected: " + closestTarget.name); // Log detected player
            m_IsPatrol = false; // Switch to chase mode
            m_PlayerInRange = true; // Player is in range
            m_PlayerPosition = closestTarget.position; // Update player position
        }
        else // If no target is detected
        {
            Debug.Log("No player detected"); // Log no player detected
            m_PlayerInRange = false; // Player is not in range
            m_IsPatrol = true; // Switch to patrol mode
        }
    }

    void RotateTowardsMovementDirection() // Handles rotation towards movement direction
    {
        if (!m_IsPatrol && m_PlayerInRange) // If chasing and player is in range
        {
            float distanceToPlayer = Vector3.Distance(transform.position, m_PlayerPosition); // Calculate distance to player

            if (distanceToPlayer > 0.5f) // If not too close to the player
            {
                Vector3 directionToPlayer = (m_PlayerPosition - transform.position).normalized; // Calculate direction to player
                directionToPlayer.y = 0; // Ensure no vertical tilt
                if (directionToPlayer != Vector3.zero) // Avoid zero direction errors
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer); // Calculate target rotation
                    float rotationOffset = -270f; // Apply rotation offset
                    targetRotation *= Quaternion.Euler(0, rotationOffset, 0); // Adjust rotation
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // Smoothly rotate towards target
                }
            }
        }
        else if (m_IsPatrol && navMeshAgent.velocity.sqrMagnitude > 0.01f) // If patrolling and moving
        {
            float direction = navMeshAgent.velocity.x; // Get movement direction
            if (direction > 0) // If moving right
            {
                transform.rotation = Quaternion.Euler(0, 180, 0); // Face right
            }
            else // If moving left
            {
                transform.rotation = Quaternion.Euler(0, 0, 0); // Face left
            }
        }
    }

    void OnCollisionEnter(Collision collision) // Called when a collision starts
    {
        if ((collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("canPickUp")) && !isColliding) // If colliding with player or pick-up object
        {
            navMeshAgent.isStopped = true; // Stop the agent
            isColliding = true; // Set colliding flag
        }
    }

    void OnCollisionExit(Collision collision) // Called when a collision ends
    {
        if ((collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("canPickUp")) && isColliding) // If collision ends with player or pick-up object
        {
            StartCoroutine(ResumeAfterCollision()); // Resume movement after delay
        }
    }

    IEnumerator ResumeAfterCollision() // Handles resuming movement after collision
    {
        yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds
        navMeshAgent.isStopped = false; // Resume movement
        isColliding = false; // Reset colliding flag
    }
}