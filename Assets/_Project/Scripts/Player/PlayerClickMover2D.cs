using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClickMover2D : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 4f;
    public float stopDistance = 0.05f;

    [Header("Layers")]
    public LayerMask groundMask;

    private Rigidbody2D rb;
    private Vector2 targetPos;
    private bool hasTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        targetPos = rb.position;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // click sinistro
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 clickPoint = new Vector2(world.x, world.y);

            // Raycast sul piano di camminamento
            RaycastHit2D hit = Physics2D.Raycast(clickPoint, Vector2.zero, 0.1f, groundMask);
            if (hit.collider != null)
            {
                targetPos = hit.point;
                hasTarget = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        Vector2 current = rb.position;
        Vector2 dir = (targetPos - current);
        float dist = dir.magnitude;

        if (dist <= stopDistance)
        {
            rb.MovePosition(targetPos);
            hasTarget = false;
            return;
        }

        Vector2 step = dir.normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(current + step);
    }

    // opzionale: gizmo del target
    void OnDrawGizmosSelected()
    {
        if (hasTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPos, 0.1f);
        }
    }
}
