using UnityEngine;

/*
 * PreyAI.cs
 * 
 * Purpose: Controls AI behavior for prey entities that flee from the player.
 * Implements intelligent escape mechanics with obstacle avoidance and different
 * movement patterns. Now includes grab/release functionality.
 * 
 * Features:
 * - Configurable detection and escape ranges
 * - Multiple movement types (expandable)
 * - Smart obstacle avoidance system
 * - Smooth escape behavior with physics-based movement
 * - Grab/Release functionality
 * - Visual debugging tools in editor
 * 
 * Usage:
 * 1. Attach to prey GameObject with Rigidbody
 * 2. Add "Holdable" tag to GameObject
 * 3. Configure detection range and escape parameters
 * 4. Set appropriate obstacle layers for avoidance
 */

[RequireComponent(typeof(Rigidbody))]
public class PreyAI : MonoBehaviour, IGrabbable
{
    public enum MovementType
    {
        Linear,
        // Add more movement types here later
        // Curved,
        // Zigzag,
        // etc.
    }

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float escapeDistance = 5f;
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Movement Type")]
    [SerializeField] private MovementType movementType = MovementType.Linear;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int maxAttempts = 8;
    [SerializeField] private float rotationStep = 45f; // Degrees to rotate when finding alternative path

    private Transform player;
    private Rigidbody rb;
    private bool isEscaping = false;
    private Vector3 escapePosition;   
    private bool isGrabbed = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isGrabbed) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within detection range OR if we're still escaping
        if (distanceToPlayer <= detectionRange || isEscaping)
        {
            // Calculate new escape position if:
            // 1. We're not currently escaping, or
            // 2. We're too close to the player, or
            // 3. We've reached our current escape position
            if (!isEscaping || 
                distanceToPlayer < escapeDistance * 0.8f ||
                Vector3.Distance(transform.position, escapePosition) < 0.5f)
            {
                // Only calculate new escape position if player is in range
                if (distanceToPlayer <= detectionRange)
                {
                    CalculateEscapePosition();
                }
            }

            // Continue moving if we're escaping
            if (isEscaping)
            {
                MoveToEscapePosition();
                
                // Only stop escaping if we've reached our destination
                if (Vector3.Distance(transform.position, escapePosition) < 0.5f)
                {
                    isEscaping = false;
                    rb.velocity = Vector3.zero;
                }
            }
        }
    }

    private void CalculateEscapePosition()
    {
        // Calculate direction away from player
        Vector3 directionFromPlayer = (transform.position - player.position).normalized;
        
        // Set escape position further than escape distance to ensure movement
        Vector3 validEscapePos = FindValidEscapePosition(directionFromPlayer);
        
        // If no valid position found, stay in place
        if (validEscapePos == transform.position)
        {
            isEscaping = false;
            return;
        }

        escapePosition = validEscapePos;
        isEscaping = true;
    }

    private Vector3 FindValidEscapePosition(Vector3 initialDirection)
    {
        // Try initial direction first
        if (IsPathClear(initialDirection))
        {
            return transform.position + initialDirection * escapeDistance;
        }

        // Try alternative directions
        float currentAngle = 0f;
        for (int i = 1; i <= maxAttempts; i++)
        {
            // Alternate between positive and negative angles with increasing magnitude
            currentAngle = (i % 2 == 0 ? 1 : -1) * (i * rotationStep);
            
            Vector3 rotatedDirection = Quaternion.Euler(0, currentAngle, 0) * initialDirection;
            if (IsPathClear(rotatedDirection))
            {
                Vector3 newPosition = transform.position + rotatedDirection * escapeDistance;
                newPosition.y = transform.position.y; // Keep same Y position
                return newPosition;
            }
        }

        // If no valid position found, return current position
        return transform.position;
    }

    private bool IsPathClear(Vector3 direction)
    {
        RaycastHit hit;
        return !Physics.Raycast(transform.position, direction, out hit, escapeDistance, obstacleLayer);
    }

    private void MoveToEscapePosition()
    {
        switch (movementType)
        {
            case MovementType.Linear:
                Vector3 moveDirection = (escapePosition - transform.position).normalized;
                rb.velocity = moveDirection * moveSpeed;
                break;
        }
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (isEscaping)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, escapePosition);
        }

        // Visualize attempted escape directions in edit mode
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 initialDirection = transform.forward;
            for (int i = 1; i <= maxAttempts; i++)
            {
                float angle = (i % 2 == 0 ? 1 : -1) * (i * rotationStep);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * initialDirection;
                Gizmos.DrawRay(transform.position, direction * escapeDistance);
            }
        }
    }
    
    public void OnGrab()
    {
        isGrabbed = true;
        rb.velocity = Vector3.zero;
        isEscaping = false;
    }

    public void OnRelease()
    {
        isGrabbed = false;

    }
} 