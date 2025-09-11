using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

/// <summary>
/// Bootstrap per garantire la presenza di due vasi interattivi nella stanza Dome.
/// Da attaccare a ROOM_Dome o a un Empty figlio.
/// Crea automaticamente i vasi se mancanti o se il prefab non esiste.
/// </summary>
public class RoomDomePotsBootstrap : MonoBehaviour
{
    [Header("Pot Configuration")]
    [SerializeField] private string pot1Id = "POT-001";
    [SerializeField] private string pot2Id = "POT-002";

    [Header("Pot Positions")]
    [SerializeField] private Vector2 pot1Offset = new Vector2(-1.5f, 0f);
    [SerializeField] private Vector2 pot2Offset = new Vector2(1.5f, 0f);

    [Header("Pot Settings")]
    [SerializeField] private float potScale = 1f;
    [SerializeField] private LayerMask potLayer = 1; // Default layer

    [Header("Prefab Reference")]
    [SerializeField] private GameObject potPrefab;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool drawGizmos = true;

    private GameObject potsAnchor;
    private PotSlot pot1;
    private PotSlot pot2;

    void Start()
    {
        InitializeDomePots();
    }

    private void InitializeDomePots()
    {
        if (showDebugLogs)
        {
            Debug.Log("[RoomDomePotsBootstrap] Inizializzazione vasi Dome...");
        }

        // Crea o trova l'anchor per i vasi
        CreatePotsAnchor();

        // Crea i due vasi
        CreatePot(pot1Id, pot1Offset, 1);
        CreatePot(pot2Id, pot2Offset, 2);

        if (showDebugLogs)
        {
            Debug.Log("[RoomDomePotsBootstrap] Inizializzazione vasi Dome completata.");
        }

        // BLK-01.03A: I vasi verranno registrati automaticamente da PotActions
        // NON registrare qui per evitare duplicazione
        if (showDebugLogs)
        {
            Debug.Log("[RoomDomePotsBootstrap] Vasi creati, registrazione gestita da PotActions");
        }
    }

    private void CreatePotsAnchor()
    {
        // Cerca se esiste gi� un anchor
        potsAnchor = transform.Find("Dome_PotsAnchor")?.gameObject;

        if (potsAnchor == null)
        {
            // Crea un nuovo anchor
            potsAnchor = new GameObject("Dome_PotsAnchor");
            potsAnchor.transform.SetParent(transform);
            potsAnchor.transform.localPosition = Vector3.zero;

            if (showDebugLogs)
            {
                Debug.Log("[RoomDomePotsBootstrap] Creato nuovo anchor Dome_PotsAnchor");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("[RoomDomePotsBootstrap] Trovato anchor esistente Dome_PotsAnchor");
            }
        }
    }

    private void CreatePot(string potId, Vector2 offset, int potNumber)
    {
        // Cerca se il vaso esiste gi�
        Transform existingPot = potsAnchor.transform.Find($"Pot_{potId}");
        if (existingPot != null)
        {
            PotSlot existingSlot = existingPot.GetComponent<PotSlot>();
            if (existingSlot != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[RoomDomePotsBootstrap] Vaso {potId} gi� esistente, saltato.");
                }

                // Assegna il riferimento
                if (potNumber == 1) pot1 = existingSlot;
                else pot2 = existingSlot;

                return;
            }
        }

        GameObject potGO;

        // Prova a usare il prefab se assegnato
        if (potPrefab != null)
        {
            potGO = Instantiate(potPrefab, potsAnchor.transform);
            if (showDebugLogs)
            {
                Debug.Log($"[RoomDomePotsBootstrap] Istanzato vaso {potId} da prefab.");
            }
        }
        else
        {
            // Crea il vaso a runtime se non c'� prefab
            potGO = CreatePotRuntime(potId);
            if (showDebugLogs)
            {
                Debug.Log($"[RoomDomePotsBootstrap] Creato vaso {potId} a runtime.");
            }
        }

        // Configura il vaso
        ConfigurePot(potGO, potId, offset, potNumber);
    }

    private GameObject CreatePotRuntime(string potId)
    {
        // Crea un GameObject vuoto
        GameObject potGO = new GameObject($"Pot_{potId}");
        potGO.transform.SetParent(potsAnchor.transform);

        // Aggiungi SpriteRenderer con sprite di default
        SpriteRenderer sr = potGO.AddComponent<SpriteRenderer>();

        // Prova a trovare uno sprite di default
        Sprite defaultSprite = FindDefaultSprite();
        if (defaultSprite != null)
        {
            sr.sprite = defaultSprite;
        }
        else
        {
            // Crea un sprite quadrato di default se non ne trova nessuno
            sr.sprite = CreateDefaultSprite();
        }

        // Aggiungi BoxCollider2D per interazioni
        BoxCollider2D collider = potGO.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one; // Dimensione 1x1

        // Aggiungi il componente PotSlot
        PotSlot potSlot = potGO.AddComponent<PotSlot>();
        
        // Aggiungi il componente PotActions per BLK-01.02
        PotActions potActions = potGO.AddComponent<PotActions>();
        
        // Aggiungi il componente PotGrowthController per BLK-01.03A
        PotGrowthController potGrowthController = potGO.AddComponent<PotGrowthController>();

        // Imposta il layer
        potGO.layer = (int)Mathf.Log(potLayer.value, 2);

        return potGO;
    }

    private Sprite FindDefaultSprite()
    {
        // Cerca sprite esistenti nel progetto
        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

        // Priorit�: cerca sprite che potrebbero essere vasi
        foreach (Sprite sprite in allSprites)
        {
            if (sprite.name.ToLower().Contains("pot") ||
                sprite.name.ToLower().Contains("vase") ||
                sprite.name.ToLower().Contains("plant"))
            {
                return sprite;
            }
        }

        // Fallback: primo sprite disponibile
        if (allSprites.Length > 0)
        {
            return allSprites[0];
        }

        return null;
    }

    private Sprite CreateDefaultSprite()
    {
        // Crea una texture quadrata di default
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];

        // Colore marrone per il vaso
        Color potColor = new Color(0.6f, 0.4f, 0.2f);

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = potColor;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Crea sprite dalla texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        sprite.name = "DefaultPotSprite";

        return sprite;
    }

    private void ConfigurePot(GameObject potGO, string potId, Vector2 offset, int potNumber)
    {
        // Posiziona il vaso
        potGO.transform.localPosition = new Vector3(offset.x, offset.y, 0);

        // Scala il vaso
        potGO.transform.localScale = Vector3.one * potScale;

        // Configura il PotSlot
        PotSlot potSlot = potGO.GetComponent<PotSlot>();
        if (potSlot != null)
        {
            potSlot.SetPotId(potId);
        }
        
        // Configura il PotGrowthController
        PotGrowthController potGrowthController = potGO.GetComponent<PotGrowthController>();
        if (potGrowthController != null)
        {
            // Crea e assegna il PotStateModel (Stage 0 = Empty per vasi vuoti)
            Debug.LogError($"new pot state model with id: {potId}");
            PotStateModel potState = new PotStateModel(potId);
            potGrowthController.SetPotState(potState);
            
            if (showDebugLogs)
            {
                Debug.Log($"[RoomDomePotsBootstrap] PotGrowthController configurato per {potId}");
            }
        }

        // Assegna il riferimento
        if (potNumber == 1) pot1 = potSlot;
        else pot2 = potSlot;

        if (showDebugLogs)
        {
            Debug.Log($"[RoomDomePotsBootstrap] Vaso {potId} configurato in posizione {offset}");
        }
    }

    /// <summary>
    /// Restituisce il primo vaso (POT-001)
    /// </summary>
    public PotSlot GetPot1()
    {
        return pot1;
    }

    /// <summary>
    /// Restituisce il secondo vaso (POT-002)
    /// </summary>
    public PotSlot GetPot2()
    {
        return pot2;
    }

    /// <summary>
    /// Restituisce tutti i vasi nella Dome
    /// </summary>
    public PotSlot[] GetAllPots()
    {
        return new PotSlot[] { pot1, pot2 };
    }


    /// <summary>
    /// Ricrea i vasi (utile per debugging)
    /// </summary>
    [ContextMenu("Recreate Pots")]
    public void RecreatePots()
    {
        // Distruggi i vasi esistenti
        if (pot1 != null) DestroyImmediate(pot1.gameObject);
        if (pot2 != null) DestroyImmediate(pot2.gameObject);

        pot1 = null;
        pot2 = null;

        // Ricrea
        InitializeDomePots();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Disegna le posizioni dei vasi
        Vector3 anchorPos = transform.position;

        // Vaso 1
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(anchorPos + new Vector3(pot1Offset.x, pot1Offset.y, 0), 0.3f);
        UnityEditor.Handles.Label(anchorPos + new Vector3(pot1Offset.x, pot1Offset.y, 0.5f), pot1Id);

        // Vaso 2
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(anchorPos + new Vector3(pot2Offset.x, pot2Offset.y, 0), 0.3f);
        UnityEditor.Handles.Label(anchorPos + new Vector3(pot2Offset.x, pot2Offset.y, 0.5f), pot2Id);

        // Anchor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(anchorPos, Vector3.one * 0.2f);
        UnityEditor.Handles.Label(anchorPos + Vector3.up * 0.3f, "Dome_PotsAnchor");
    }
#endif

    /// <summary>
    /// BLK-01.03A: Registra i vasi nel DayCycleController
    /// </summary>
    private void RegisterPotsWithGrowthSystem()
    {
        var dayCycleController = FindObjectOfType<SPOR_BLK_01_03A_DayCycleController>();
        if (dayCycleController != null)
        {
            int registeredCount = 0;
            foreach (var pot in FindObjectsOfType<PotGrowthController>())
            {
                var potState = pot.GetPotState();
                if (potState != null)
                {
                    dayCycleController.RegisterPot(potState);
                    registeredCount++;
                }
            }
            Debug.Log($"[BLK-01.03A] Registered {registeredCount} pots in DayCycleController");
        }
        else
        {
            Debug.LogWarning("[BLK-01.03A] DayCycleController non trovato!");
        }
    }
}
