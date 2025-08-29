using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Widget UI minimale che mostra informazioni sul vaso selezionato.
/// Si integra con l'HUD esistente o crea un fallback se necessario.
/// </summary>
public class PotHUDWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI potInfoText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Action Buttons (BLK-01.02)")]
    [SerializeField] private Button btnPlant;
    [SerializeField] private Button btnWater;
    [SerializeField] private Button btnLight;
    [SerializeField] private TextMeshProUGUI txtCosts;
    
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
        // Sottoscrivi agli eventi del sistema dei vasi
        PotSlot.OnPotSelected += OnPotSelected;
        PotEvents.OnPotStateChanged += OnPotStateChanged;
        PotEvents.OnPotActionFailed += OnPotActionFailed;
    }
    
    void OnDisable()
    {
        // Annulla sottoscrizioni
        PotSlot.OnPotSelected -= OnPotSelected;
        PotEvents.OnPotStateChanged -= OnPotStateChanged;
        PotEvents.OnPotActionFailed -= OnPotActionFailed;
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
        
        // IMPORTANTE: Assicurati che il Canvas abbia GraphicRaycaster per i click UI
        if (parentCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            Debug.LogWarning("[PotHUDWidget] Aggiungendo GraphicRaycaster al Canvas per i click UI");
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        
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
        
        // Crea pulsanti di azione se non assegnati
        CreateActionButtons();
    }
    
    private void OnPotSelected(PotSlot pot)
    {
        if (!isInitialized) return;
        
        Debug.Log($"[PotHUDWidget] Vaso {pot.PotId} selezionato. Aggiornamento UI...");
        Debug.Log($"[PotHUDWidget] PotActions presente: {pot.PotActions != null}");
        Debug.Log($"[PotHUDWidget] Player in range: {pot.InRange}");
        
        // Aggiorna le informazioni del vaso usando il nuovo sistema
        string potInfo = $"Selected: {pot.PotId} — {GetPotStatusText(pot)}";
        UpdatePotInfo(potInfo);
        
        // Aggiorna i pulsanti di azione
        UpdateActionButtons(pot);
        
        // Mostra il widget
        SetWidgetVisible(true);
        
        Debug.Log($"[PotHUDWidget] UI aggiornata per vaso {pot.PotId}");
    }
    
    private void UpdatePotInfo(string info)
    {
        if (potInfoText != null)
        {
            potInfoText.text = info;
        }
    }
    
    private string GetPotStatusText(PotSlot pot)
    {
        if (pot.PotActions == null) return "Errore: PotActions mancante";
        
        PotStateModel state = pot.PotActions.GetCurrentState();
        if (state == null) return "Errore: Stato vaso mancante";
        
        if (state.IsEmpty)
        {
            return "Vuoto - Pronto per piantare";
        }
        else
        {
            string stageName = GetStageName(state.Stage);
            string threshold = GetStageThreshold(state.Stage);
            return $"Pianta ({stageName}) - H:{state.Hydration}/3 L:{state.LightExposure}/3 - Progresso: {state.GrowthPoints}/{threshold}";
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
    /// Mostra/nasconde il widget
    /// </summary>
    public void SetWidgetVisible(bool visible)
    {
        if (widgetContainer != null)
        {
            widgetContainer.SetActive(visible);
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
    
    #region Action Buttons (BLK-01.02)
    
    /// <summary>
    /// Crea i pulsanti di azione per il vaso
    /// </summary>
    private void CreateActionButtons()
    {
        if (btnPlant == null)
        {
            btnPlant = CreateActionButton("Plant", "Piantare", PotEvents.PotActionType.Plant);
        }
        
        if (btnWater == null)
        {
            btnWater = CreateActionButton("Water", "Annaffiare", PotEvents.PotActionType.Water);
        }
        
        if (btnLight == null)
        {
            btnLight = CreateActionButton("Light", "Illuminare", PotEvents.PotActionType.Light);
        }
        
        // Crea il testo dei costi
        if (txtCosts == null)
        {
            GameObject costsGO = new GameObject("CostsText");
            costsGO.transform.SetParent(widgetContainer.transform, false);
            
            txtCosts = costsGO.AddComponent<TextMeshProUGUI>();
            txtCosts.color = textColor;
            txtCosts.fontSize = 12;
            txtCosts.alignment = TextAlignmentOptions.Center;
            txtCosts.text = "Costo: -1 Azione / -1 CRY";
            
            RectTransform costsRect = costsGO.GetComponent<RectTransform>();
            costsRect.anchorMin = new Vector2(0, 0);
            costsRect.anchorMax = new Vector2(1, 0);
            costsRect.pivot = new Vector2(0.5f, 0);
            costsRect.anchoredPosition = new Vector2(0, 5);
            costsRect.sizeDelta = new Vector2(0, 20);
        }
        
        // Nascondi tutti i pulsanti inizialmente
        SetActionButtonsVisible(false);
    }
    
    /// <summary>
    /// Crea un singolo pulsante di azione
    /// </summary>
    private Button CreateActionButton(string buttonName, string buttonText, PotEvents.PotActionType actionType)
    {
        GameObject buttonGO = new GameObject($"Btn_{buttonName}");
        buttonGO.transform.SetParent(widgetContainer.transform, false);
        
        // Aggiungi Button
        Button button = buttonGO.AddComponent<Button>();
        
        // Aggiungi Image per il background
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Aggiungi testo
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI buttonTextComponent = textGO.AddComponent<TextMeshProUGUI>();
        buttonTextComponent.text = buttonText;
        buttonTextComponent.color = Color.white;
        buttonTextComponent.fontSize = 14;
        buttonTextComponent.alignment = TextAlignmentOptions.Center;
        
        // Posiziona il testo
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Posiziona il pulsante
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(0, 0);
        buttonRect.pivot = new Vector2(0, 0);
        buttonRect.sizeDelta = new Vector2(80, 30);
        
        // Posiziona in base al tipo di azione (Y aumentato per evitare sovrapposizioni)
        switch (actionType)
        {
            case PotEvents.PotActionType.Plant:
                buttonRect.anchoredPosition = new Vector2(10, 60);
                break;
            case PotEvents.PotActionType.Water:
                buttonRect.anchoredPosition = new Vector2(100, 60);
                break;
            case PotEvents.PotActionType.Light:
                buttonRect.anchoredPosition = new Vector2(190, 60);
                break;
        }
        
        // Aggiungi listener per l'azione
        button.onClick.AddListener(() => OnActionButtonClicked(actionType));
        
        // IMPORTANTE: Configura il pulsante per intercettare correttamente i click UI
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation() { mode = Navigation.Mode.None };
        
        // Aggiungi Image con raycast target per intercettare meglio i click
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
        }
        
        // Aggiungi EventTrigger per intercettare tutti gli eventi e prevenire movimento player
        EventTrigger eventTrigger = buttonGO.AddComponent<EventTrigger>();
        
        // PointerDown - blocca movimento player
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { 
            Debug.Log($"[PotHUDWidget] Evento PointerDown bloccato per {actionType}");
            // Blocca la propagazione dell'evento
        });
        eventTrigger.triggers.Add(pointerDownEntry);
        
        // BeginDrag - previene drag accidentali
        EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
        beginDragEntry.eventID = EventTriggerType.BeginDrag;
        beginDragEntry.callback.AddListener((data) => { 
            Debug.Log($"[PotHUDWidget] Evento BeginDrag bloccato per {actionType}");
        });
        eventTrigger.triggers.Add(beginDragEntry);
        
        return button;
    }
    
    /// <summary>
    /// Gestisce il click su un pulsante di azione
    /// </summary>
    private void OnActionButtonClicked(PotEvents.PotActionType actionType)
    {
        Debug.Log($"[PotHUDWidget] Click su pulsante {actionType} intercettato!");
        
        // Trova il vaso selezionato
        PotSlot selectedPot = FindSelectedPot();
        if (selectedPot == null || selectedPot.PotActions == null)
        {
            Debug.LogWarning("[PotHUDWidget] Nessun vaso selezionato o PotActions mancante");
            return;
        }
        
        Debug.Log($"[PotHUDWidget] Eseguendo azione {actionType} su vaso {selectedPot.PotId}");
        
        // Esegui l'azione appropriata
        bool success = false;
        switch (actionType)
        {
            case PotEvents.PotActionType.Plant:
                success = selectedPot.PotActions.DoPlant();
                break;
            case PotEvents.PotActionType.Water:
                success = selectedPot.PotActions.DoWater();
                break;
            case PotEvents.PotActionType.Light:
                success = selectedPot.PotActions.DoLight();
                break;
        }
        
        if (success)
        {
            Debug.Log($"[PotHUDWidget] Azione {actionType} eseguita con successo!");
            // Aggiorna l'UI
            UpdateActionButtons(selectedPot);
        }
        else
        {
            Debug.LogWarning($"[PotHUDWidget] Azione {actionType} fallita!");
        }
    }
    
    /// <summary>
    /// Trova il vaso attualmente selezionato
    /// </summary>
    private PotSlot FindSelectedPot()
    {
        // Trova il vaso che ha emesso l'evento OnPotSelected
        // Usa il sistema di eventi per tracciare la selezione
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        foreach (PotSlot pot in allPots)
        {
            if (pot.PotActions != null && pot.IsSelected)
            {
                Debug.Log($"[PotHUDWidget] Trovato vaso selezionato: {pot.PotId}");
                return pot;
            }
        }
        
        // Fallback: cerca il primo vaso con PotActions
        foreach (PotSlot pot in allPots)
        {
            if (pot.PotActions != null)
            {
                Debug.LogWarning($"[PotHUDWidget] Fallback: usando primo vaso disponibile {pot.PotId}");
                return pot;
            }
        }
        
        Debug.LogError("[PotHUDWidget] Nessun vaso trovato!");
        return null;
    }
    
    /// <summary>
    /// Aggiorna i pulsanti di azione in base al vaso selezionato
    /// </summary>
    private void UpdateActionButtons(PotSlot pot)
    {
        if (pot == null || pot.PotActions == null)
        {
            SetActionButtonsVisible(false);
            return;
        }
        
        // Mostra i pulsanti solo se il player è in range
        bool inRange = pot.InRange;
        SetActionButtonsVisible(inRange);
        
        if (inRange)
        {
            // Aggiorna lo stato di ogni pulsante
            UpdateButtonState(btnPlant, pot.PotActions.CanPlant(), "Piantare");
            UpdateButtonState(btnWater, pot.PotActions.CanWater(), "Annaffiare");
            UpdateButtonState(btnLight, pot.PotActions.CanLight(), "Illuminare");
        }
    }
    
    /// <summary>
    /// Aggiorna lo stato di un singolo pulsante
    /// </summary>
    private void UpdateButtonState(Button button, bool canExecute, string actionName)
    {
        if (button == null) return;
        
        button.interactable = canExecute;
        
        // Aggiorna il colore e il tooltip
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = canExecute ? 
                new Color(0.2f, 0.8f, 0.2f, 0.9f) : // Verde se abilitato
                new Color(0.5f, 0.5f, 0.5f, 0.9f);   // Grigio se disabilitato
        }
        
        // Aggiorna il testo
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = canExecute ? actionName : $"{actionName} (N/A)";
        }
    }
    
    /// <summary>
    /// Mostra/nasconde i pulsanti di azione
    /// </summary>
    private void SetActionButtonsVisible(bool visible)
    {
        if (btnPlant != null) btnPlant.gameObject.SetActive(visible);
        if (btnWater != null) btnWater.gameObject.SetActive(visible);
        if (btnLight != null) btnLight.gameObject.SetActive(visible);
        if (txtCosts != null) txtCosts.gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Gestisce il cambio di stato di un vaso
    /// </summary>
    private void OnPotStateChanged(PotSlot pot)
    {
        if (!isInitialized) return;
        
        // Aggiorna i pulsanti se questo è il vaso selezionato
        UpdateActionButtons(pot);
    }
    
    /// <summary>
    /// Gestisce il fallimento di un'azione
    /// </summary>
    private void OnPotActionFailed(PotEvents.PotActionType actionType, PotSlot pot, string reason)
    {
        if (!isInitialized) return;
        
        // Mostra il motivo del fallimento
        string failureMessage = $"Azione {PotEvents.GetActionName(actionType)} fallita: {reason}";
        UpdatePotInfo(failureMessage);
        
        Debug.LogWarning($"[PotHUDWidget] {failureMessage}");
    }
    
    #endregion

    /// <summary>
    /// Restituisce il nome localizzato per uno stadio
    /// </summary>
    private string GetStageName(int stage)
    {
        switch (stage)
        {
            case 0: return "Empty";
            case 1: return "Seed";
            case 2: return "Sprout";
            case 3: return "Mature";
            default: return $"Stadio {stage}";
        }
    }

    /// <summary>
    /// Restituisce la soglia di punti per lo stadio corrente
    /// </summary>
    private string GetStageThreshold(int stage)
    {
        switch (stage)
        {
            case 0: return "0"; // Empty (nessun avanzamento)
            case 1: return "2"; // Seed to Sprout
            case 2: return "3"; // Sprout to Mature
            case 3: return "∞"; // Mature (nessun avanzamento)
            default: return "?";
        }
    }
}
