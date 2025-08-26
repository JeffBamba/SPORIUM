using UnityEngine;

public class ElevatorSystem : MonoBehaviour
{
    public Transform[] levels;          // target Y di ogni livello
    public GameObject uiPanel;          // il pannello UI con i bottoni
    public int cryCost = 5;             // costo per utilizzo

    private bool playerInside = false;
    private Transform player;

    void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    void Update()
    {
        // Fallback scale manuali (W/S) se CRY = 0
        if (playerInside && GameManager.I != null && GameManager.I.CurrentCRY <= 0)
        {
            float vertical = Input.GetAxisRaw("Vertical"); // W/S o ↑/↓
            if (vertical != 0 && player != null)
            {
                player.Translate(Vector2.up * vertical * Time.deltaTime * 2f);
                Debug.Log("[Elevator DEBUG] Uso scale manuali con W/S (CRY = 0).");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[Elevator DEBUG] OnTriggerEnter2D chiamato con: " + other.name);

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            player = other.transform;
            if (uiPanel != null) uiPanel.SetActive(true);
            Debug.Log("[Elevator DEBUG] Player riconosciuto, pannello attivo.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("[Elevator DEBUG] OnTriggerExit2D chiamato con: " + other.name);

        if (other.CompareTag("Player"))
        {
            playerInside = false;
            player = null;
            if (uiPanel != null) uiPanel.SetActive(false);
            Debug.Log("[Elevator DEBUG] Player uscito, pannello disattivato.");
        }
    }

    public void GoToLevel(int index)
    {
        if (GameManager.I == null || player == null) return;

        if (GameManager.I.CurrentCRY < cryCost)
        {
            Debug.LogWarning("[Elevator DEBUG] Non hai CRY a sufficienza! Usa le scale (W/S).");
            return;
        }

        if (GameManager.I.TrySpendAction(cryCost))
        {
            Vector3 targetPos = new Vector3(player.position.x, levels[index].position.y, player.position.z);
            player.position = targetPos;
            Debug.Log($"[Elevator DEBUG] Spostato al livello {index}. CRY rimasti: {GameManager.I.CurrentCRY}, Azioni: {GameManager.I.ActionsLeft}");
        }
    }
}
