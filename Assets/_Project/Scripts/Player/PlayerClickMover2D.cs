using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClickMover2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDistance = 0.1f; // Aumentato per fermata più precisa
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
                rb.velocity = Vector2.zero; // Forza la velocità a zero
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
        isMoving = true;
        
        // Reset della velocità quando si imposta un nuovo target
        currentVelocity = Vector2.zero;
        rb.velocity = Vector2.zero;
    }

    private void MoveTowardsTarget()
    {
        Vector2 direction = (targetPosition - rb.position).normalized;
        float distanceToTarget = Vector2.Distance(rb.position, targetPosition);

        // Controlla se siamo abbastanza vicini al target per fermarci
        if (distanceToTarget <= stopDistance)
        {
            // Raggiunto il target - ferma il movimento
            hasTarget = false;
            isMoving = false;
            currentVelocity = Vector2.zero;
            rb.velocity = Vector2.zero;
            
            // Posiziona il player esattamente sul target per evitare deriva
            rb.position = targetPosition;
            return;
        }

        // Movimento con accelerazione
        Vector2 targetVelocity = direction * moveSpeed;
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        
        rb.velocity = currentVelocity;

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
            
            // Linea diretta al target
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // Area di stop
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, stopDistance);
        }
        
        // Area di pathfinding
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pathfindingRadius);
    }
}
