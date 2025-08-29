using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Widget UI minimale che mostra informazioni sul vaso selezionato.
/// Si integra con l'HUD esistente o crea un fallback se necessario.
/// </summary>
public class PotHUDWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI potInfoText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Widget Settings")]
    [SerializeField] private Vector2 widgetPosition = new Vector2(12, 12);
    [SerializeField] private Vector2 widgetSize = new Vector2(300, 80);
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    
    [Header("Fallback Settings")]
    [SerializeField] private bool createFallbackCanvas = true;
    [SerializeField] private string fallbackCanvasName = "PotHUD_Fallback";
    
    private GameObject widgetContainer;
    private Canvas parentCanvas;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeWidget();
    }
    
    void OnEnable()
    {
        // Sottoscrivi all'evento di selezione vaso
        PotSlot.OnPotSelected += OnPotSelected;
    }
    
    void OnDisable()
    {
        // Annulla sottoscrizione
        PotSlot.OnPotSelected -= OnPotSelected;
    }
    
    private void InitializeWidget()
    {
        // Cerca un Canvas esistente (preferibilmente quello dell'HUD)
        parentCanvas = FindParentCanvas();
        
        if (parentCanvas == null && createFallbackCanvas)
        {
            // Crea un Canvas di fallback se non ne trova nessuno
            CreateFallbackCanvas();
        }
        
        if (parentCanvas == null)
        {
            Debug.LogError("[PotHUDWidget] Impossibile trovare o creare un Canvas. Widget disabilitato.");
            enabled = false;
            return;
        }
        
        // Crea il widget UI
        CreateWidgetUI();
        
        // Imposta testo iniziale
        UpdatePotInfo("Nessun vaso selezionato");
        
        isInitialized = true;
        Debug.Log("[PotHUDWidget] Widget inizializzato correttamente.");
    }
    
    private Canvas FindParentCanvas()
    {
        // Cerca prima nell'HUD esistente
        HUDController hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            Canvas hudCanvas = hudController.GetComponentInParent<Canvas>();
            if (hudCanvas != null)
            {
                Debug.Log("[PotHUDWidget] Trovato Canvas dell'HUD esistente.");
                return hudCanvas;
            }
        }
        
        // Cerca qualsiasi Canvas nella scena
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || 
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                Debug.Log($"[PotHUDWidget] Trovato Canvas: {canvas.name}");
                return canvas;
            }
        }
        
        Debug.LogWarning("[PotHUDWidget] Nessun Canvas trovato nella scena.");
        return null;
    }
    
    private void CreateFallbackCanvas()
    {
        // Crea un GameObject per il Canvas
        GameObject canvasGO = new GameObject(fallbackCanvasName);
        parentCanvas = canvasGO.AddComponent<Canvas>();
        parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        parentCanvas.sortingOrder = 100; // Alto z-order per essere sopra tutto
        
        // Aggiungi CanvasScaler per responsive design
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Aggiungi GraphicRaycaster per interazioni UI
        canvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log("[PotHUDWidget] Creato Canvas di fallback.");
    }
    
    private void CreateWidgetUI()
    {
        // Crea il container del widget
        widgetContainer = new GameObject("UI_PotInfo");
        widgetContainer.transform.SetParent(parentCanvas.transform, false);
        
        // Aggiungi RectTransform
        RectTransform rectTransform = widgetContainer.AddComponent<RectTransform>();
        
        // Posiziona in basso-sinistra
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = widgetPosition;
        rectTransform.sizeDelta = widgetSize;
        
        // Crea background (opzionale)
        if (backgroundImage == null)
        {
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(widgetContainer.transform, false);
            
            backgroundImage = bgGO.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }
        
        // Crea testo se non assegnato
        if (potInfoText == null)
        {
            GameObject textGO = new GameObject("PotInfoText");
            textGO.transform.SetParent(widgetContainer.transform, false);
            
            potInfoText = textGO.AddComponent<TextMeshProUGUI>();
            potInfoText.color = textColor;
            potInfoText.fontSize = 16;
            potInfoText.alignment = TextAlignmentOptions.Left;
            potInfoText.text = "Nessun vaso selezionato";
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
        }
    }
    
    private void OnPotSelected(PotSlot pot)
    {
        if (!isInitialized) return;
        
        // Aggiorna le informazioni del vaso
        string potInfo = $"Selected: {pot.PotId} — Stato: {GetStateText(pot.State)}";
        UpdatePotInfo(potInfo);
        
        Debug.Log($"[PotHUDWidget] Aggiornato widget per vaso: {pot.PotId}");
    }
    
    private void UpdatePotInfo(string info)
    {
        if (potInfoText != null)
        {
            potInfoText.text = info;
        }
    }
    
    private string GetStateText(PotState state)
    {
        switch (state)
        {
            case PotState.Empty:
                return "Vuoto";
            case PotState.Occupied:
                return "Occupato";
            case PotState.Growing:
                return "In crescita";
            case PotState.Mature:
                return "Maturo";
            default:
                return "Sconosciuto";
        }
    }
    
    /// <summary>
    /// Forza l'aggiornamento del widget con un messaggio personalizzato
    /// </summary>
    public void SetCustomMessage(string message)
    {
        UpdatePotInfo(message);
    }
    
    /// <summary>
    /// Nasconde il widget
    /// </summary>
    public void HideWidget()
    {
        if (widgetContainer != null)
        {
            widgetContainer.SetActive(false);
        }
    }
    
    /// <summary>
    /// Mostra il widget
    /// </summary>
    public void ShowWidget()
    {
        if (widgetContainer != null)
        {
            widgetContainer.SetActive(true);
        }
    }
    
    /// <summary>
    /// Cambia la posizione del widget
    /// </summary>
    public void SetWidgetPosition(Vector2 newPosition)
    {
        widgetPosition = newPosition;
        if (widgetContainer != null)
        {
            RectTransform rectTransform = widgetContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = widgetPosition;
            }
        }
    }
}
