using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target Configuration")]
    [SerializeField] private Transform target;
    [SerializeField] private bool findPlayerOnStart = true;
    [SerializeField] private string playerTag = "Player";
    
    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = Vector3.zero;
    [SerializeField] private bool useOffset = false;
    
    [Header("Bounds")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 10f);
    
    [Header("Advanced Settings")]
    [SerializeField] private bool lookAhead = false;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private bool smoothRotation = false;
    [SerializeField] private float rotationSpeed = 3f;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private bool isInitialized = false;

    void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("[CameraFollow2D] Configurazione non valida! CameraFollow2D disabilitato.");
                enabled = false;
                return;
            }
        }
        
        InitializeCamera();
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;
        
        if (smoothSpeed <= 0)
        {
            Debug.LogWarning("[CameraFollow2D] smoothSpeed deve essere maggiore di 0. Impostato a 1.");
            smoothSpeed = 1f;
        }
        
        if (rotationSpeed <= 0)
        {
            Debug.LogWarning("[CameraFollow2D] rotationSpeed deve essere maggiore di 0. Impostato a 1.");
            rotationSpeed = 1f;
        }
        
        if (lookAheadDistance < 0)
        {
            Debug.LogWarning("[CameraFollow2D] lookAheadDistance non può essere negativo. Impostato a 0.");
            lookAheadDistance = 0f;
        }
        
        return isValid;
    }

    private void InitializeCamera()
    {
        // Trova il target se non assegnato
        if (target == null && findPlayerOnStart)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                target = player.transform;
                if (showDebugLogs)
                {
                    Debug.Log($"[CameraFollow2D] Target trovato automaticamente: {target.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[CameraFollow2D] Nessun oggetto con tag '{playerTag}' trovato!");
            }
        }
        
        if (target != null)
        {
            // Imposta posizione iniziale
            targetPosition = CalculateTargetPosition();
            transform.position = targetPosition;
            
            isInitialized = true;
            
            if (showDebugLogs)
            {
                Debug.Log("[CameraFollow2D] Camera inizializzata correttamente.");
            }
        }
        else
        {
            Debug.LogWarning("[CameraFollow2D] Nessun target assegnato! Camera non seguirà nulla.");
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || target == null) return;
        
        // Calcola posizione target
        targetPosition = CalculateTargetPosition();
        
        // Applica bounds se abilitati
        if (useBounds)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
        
        // Movimento smooth
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            1f / smoothSpeed
        );
        
        transform.position = smoothedPosition;
        
        // Rotazione smooth se abilitata
        if (smoothRotation && target != null)
        {
            HandleSmoothRotation();
        }
    }

    private Vector3 CalculateTargetPosition()
    {
        if (target == null) return transform.position;
        
        Vector3 basePosition = target.position;
        
        // Aggiungi offset se abilitato
        if (useOffset)
        {
            basePosition += offset;
        }
        
        // Mantieni la Z originale della camera
        basePosition.z = transform.position.z;
        
        // Look ahead se abilitato
        if (lookAhead && target != null)
        {
            // Calcola direzione del movimento del target
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null && targetRb.velocity.magnitude > 0.1f)
            {
                Vector2 lookAheadOffset = targetRb.velocity.normalized * lookAheadDistance;
                basePosition += new Vector3(lookAheadOffset.x, lookAheadOffset.y, 0);
            }
        }
        
        return basePosition;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        float clampedX = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
        
        return new Vector3(clampedX, clampedY, position.z);
    }

    private void HandleSmoothRotation()
    {
        if (target == null) return;
        
        // Calcola direzione verso il target
        Vector2 direction = (target.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Rotazione smooth
        float currentAngle = transform.rotation.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    // Metodi pubblici per configurazione runtime
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isInitialized = target != null;
        
        if (showDebugLogs)
        {
            Debug.Log($"[CameraFollow2D] Nuovo target assegnato: {(target != null ? target.name : "null")}");
        }
    }

    public void SetSmoothSpeed(float newSpeed)
    {
        smoothSpeed = Mathf.Max(0.1f, newSpeed);
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        useOffset = true;
    }

    public void SetUseOffset(bool use)
    {
        useOffset = use;
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    public void SetUseBounds(bool use)
    {
        useBounds = use;
    }

    public void SetLookAhead(bool use, float distance = 2f)
    {
        lookAhead = use;
        lookAheadDistance = Mathf.Max(0, distance);
    }

    public void SetSmoothRotation(bool use, float speed = 3f)
    {
        smoothRotation = use;
        rotationSpeed = Mathf.Max(0.1f, speed);
    }

    // Metodi per ottenere informazioni
    public Transform GetTarget() => target;
    public float GetSmoothSpeed() => smoothSpeed;
    public Vector3 GetOffset() => offset;
    public bool IsUsingOffset() => useOffset;
    public bool IsUsingBounds() => useBounds;
    public bool IsLookAheadEnabled() => lookAhead;
    public bool IsSmoothRotationEnabled() => smoothRotation;
    public bool IsInitialized => isInitialized;

    // Metodo per resettare la camera alla posizione del target
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = CalculateTargetPosition();
            if (useBounds)
            {
                transform.position = ClampToBounds(transform.position);
            }
        }
    }

    // Metodo per ottenere informazioni dettagliate
    public string GetCameraInfo()
    {
        string info = $"Camera Info:\n";
        info += $"Target: {(target != null ? target.name : "null")}\n";
        info += $"Inizializzata: {isInitialized}\n";
        info += $"Smooth Speed: {smoothSpeed}\n";
        info += $"Offset: {offset} (Usato: {useOffset})\n";
        info += $"Bounds: {minBounds} -> {maxBounds} (Usato: {useBounds})\n";
        info += $"Look Ahead: {lookAhead} (Distanza: {lookAheadDistance})\n";
        info += $"Smooth Rotation: {smoothRotation} (Velocità: {rotationSpeed})";
        
        return info;
    }

    // Gizmos per debug
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        
        // Disegna bounds
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) * 0.5f,
            (minBounds.y + maxBounds.y) * 0.5f,
            transform.position.z
        );
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        // Disegna offset se abilitato
        if (useOffset && target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position + offset, 0.3f);
            Gizmos.DrawLine(target.position, target.position + offset);
        }
        
        // Disegna look ahead se abilitato
        if (lookAhead && target != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 lookAheadPos = CalculateTargetPosition();
            Gizmos.DrawWireSphere(lookAheadPos, 0.2f);
            Gizmos.DrawLine(transform.position, lookAheadPos);
        }
    }
}
