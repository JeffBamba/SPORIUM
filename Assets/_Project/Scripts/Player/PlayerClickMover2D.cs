using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClickMover2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDistance = 0.05f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Pathfinding")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float pathfindingRadius = 0.5f;

    [Header("Smoothing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool smoothRotation = true;

    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    private bool hasTarget;
    private bool isMoving;

    // Pathfinding
    private Vector2[] pathPoints;
    private int currentPathIndex;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        targetPosition = rb.position;
        
        // Configura Rigidbody2D per movimento smooth
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        if (hasTarget)
        {
            MoveTowardsTarget();
        }
        else if (isMoving)
        {
            // Decelerazione quando non c'è target
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            rb.velocity = currentVelocity;
            
            if (currentVelocity.magnitude < 0.01f)
            {
                isMoving = false;
                currentVelocity = Vector2.zero;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = GetMouseWorldPosition();
            
            if (IsValidTargetPosition(clickPosition))
            {
                SetTarget(clickPosition);
            }
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    private bool IsValidTargetPosition(Vector2 position)
    {
        // Controlla se la posizione è raggiungibile
        RaycastHit2D hit = Physics2D.CircleCast(position, pathfindingRadius, Vector2.zero, 0.1f, groundMask);
        return hit.collider != null;
    }

    private void SetTarget(Vector2 newTarget)
    {
        targetPosition = newTarget;
        hasTarget = true;
        currentPathIndex = 0;
        
        // Calcola path semplice (per ora lineare, può essere esteso con A*)
        CalculateSimplePath();
    }

    private void CalculateSimplePath()
    {
        Vector2 startPos = rb.position;
        Vector2 direction = (targetPosition - startPos).normalized;
        float distance = Vector2.Distance(startPos, targetPosition);
        
        // Path semplice con punti intermedi per evitare ostacoli
        int pathSegments = Mathf.Max(1, Mathf.RoundToInt(distance / 2f));
        pathPoints = new Vector2[pathSegments + 1];
        
        for (int i = 0; i <= pathSegments; i++)
        {
            float t = (float)i / pathSegments;
            pathPoints[i] = Vector2.Lerp(startPos, targetPosition, t);
        }
        
        currentPathIndex = 0;
    }

    private void MoveTowardsTarget()
    {
        if (currentPathIndex >= pathPoints.Length)
        {
            // Raggiunto il target
            hasTarget = false;
            isMoving = false;
            return;
        }

        Vector2 currentTarget = pathPoints[currentPathIndex];
        Vector2 direction = (currentTarget - rb.position).normalized;
        float distanceToTarget = Vector2.Distance(rb.position, currentTarget);

        if (distanceToTarget <= stopDistance)
        {
            // Raggiunto il punto del path, passa al prossimo
            currentPathIndex++;
            return;
        }

        // Movimento con accelerazione
        Vector2 targetVelocity = direction * moveSpeed;
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        
        rb.velocity = currentVelocity;
        isMoving = true;

        // Rotazione smooth verso la direzione del movimento
        if (smoothRotation && currentVelocity.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
            float currentAngle = rb.rotation;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.rotation = newAngle;
        }
    }

    public void StopMovement()
    {
        hasTarget = false;
        isMoving = false;
        currentVelocity = Vector2.zero;
        rb.velocity = Vector2.zero;
    }

    public bool IsMoving => isMoving || hasTarget;

    // Gizmos per debug
    void OnDrawGizmosSelected()
    {
        if (hasTarget)
        {
            // Target
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
            
            // Path
            if (pathPoints != null)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < pathPoints.Length - 1; i++)
                {
                    Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
                    Gizmos.DrawWireSphere(pathPoints[i], 0.1f);
                }
                if (pathPoints.Length > 0)
                {
                    Gizmos.DrawWireSphere(pathPoints[pathPoints.Length - 1], 0.1f);
                }
            }
        }
        
        // Area di pathfinding
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pathfindingRadius);
    }
}
