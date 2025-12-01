using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using _Project;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Placeholder HUD funzionale per visualizzare il pH della Dome
    /// Mostra valore pH, banda pH e colore indicativo
    /// </summary>
    public class HUDPhDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI phValueText;
    [SerializeField] private TextMeshProUGUI phBandText;
    [SerializeField] private Image phIndicatorImage; // Barra/indicatore visivo opzionale
    
    [Header("Settings")]
    [SerializeField] private bool updateOnStart = true;
    [SerializeField] private float updateInterval = 0.1f; // Aggiorna ogni 0.1 secondi per catturare oscillazioni
    [SerializeField] private bool autoCreateUI = true; // Crea UI automaticamente se mancante
    [SerializeField] private Vector2 uiPosition = new Vector2(0f, -30f); // Posizione alto-centro
    
    [Header("Hover Tooltip")]
    [SerializeField] private bool enableHoverTooltip = true;
    [SerializeField] private GameObject tooltipPanel; // Riquadro tooltip (creato automaticamente se null)
    [SerializeField] private TextMeshProUGUI tooltipText; // Testo del tooltip
    
    private PhSystem _phSystem;
    private float _lastUpdateTime;
    private Canvas _targetCanvas;
    private UINotification _uiNotification;
    private bool _isHovering = false;
    private GameObject _tooltipInstance;
    
    private void Awake()
    {
        // Auto-setup: crea UI se mancante
        if (autoCreateUI)
        {
            AutoSetupUI();
        }
    }
    
    private void Start()
    {
        // Cerca PhSystem nel ServiceContainer o crea uno temporaneo
        TryGetPhSystem();
        
        // Cerca UINotification per i toast
        _uiNotification = FindObjectOfType<UINotification>();
        
        // Setup hover detection e tooltip
        if (enableHoverTooltip)
        {
            SetupHoverDetection();
            CreateTooltipPanel();
        }
        
        if (updateOnStart)
        {
            UpdatePhDisplay();
        }
    }
    
    /// <summary>
    /// Configura hover detection sugli elementi UI del pH
    /// </summary>
    private void SetupHoverDetection()
    {
        // Aggiungi EventTrigger a phValueText
        if (phValueText != null)
        {
            AddHoverEvents(phValueText.gameObject);
        }
        
        // Aggiungi EventTrigger a phBandText
        if (phBandText != null)
        {
            AddHoverEvents(phBandText.gameObject);
        }
        
        // Aggiungi EventTrigger a phIndicatorImage se presente
        if (phIndicatorImage != null)
        {
            AddHoverEvents(phIndicatorImage.gameObject);
        }
    }
    
    /// <summary>
    /// Aggiunge eventi hover a un GameObject UI
    /// </summary>
    private void AddHoverEvents(GameObject target)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }
        
        // Evento PointerEnter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnPhHUDHoverEnter(); });
        trigger.triggers.Add(enterEntry);
        
        // Evento PointerExit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnPhHUDHoverExit(); });
        trigger.triggers.Add(exitEntry);
    }
    
    /// <summary>
    /// Crea il pannello tooltip se non esiste
    /// </summary>
    private void CreateTooltipPanel()
    {
        if (_targetCanvas == null)
        {
            // Cerca un Canvas Screen Space Overlay esistente
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    _targetCanvas = canvas;
                    break;
                }
            }
            if (_targetCanvas == null) return;
        }
        
        // Crea tooltip panel se non assegnato manualmente
        if (tooltipPanel == null)
        {
            _tooltipInstance = new GameObject("pH_TooltipPanel");
            _tooltipInstance.transform.SetParent(_targetCanvas.transform, false);
            
            // Aggiungi Image per background
            Image bgImage = _tooltipInstance.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.9f); // Nero semi-trasparente
            
            // Crea testo
            GameObject textGO = new GameObject("pH_TooltipText");
            textGO.transform.SetParent(_tooltipInstance.transform, false);
            tooltipText = textGO.AddComponent<TextMeshProUGUI>();
            tooltipText.text = "";
            tooltipText.fontSize = 14;
            tooltipText.color = Color.white;
            tooltipText.alignment = TextAlignmentOptions.TopLeft;
            tooltipText.raycastTarget = false;
            tooltipText.richText = true; // Abilita rich text per interpretare i tag <color>, <b>, ecc.
            
            // Configura RectTransform del testo
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10f, 10f);
            textRect.offsetMax = new Vector2(-10f, -10f);
            
            // Configura RectTransform del panel
            RectTransform panelRect = _tooltipInstance.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = uiPosition + new Vector2(0f, -80f); // Sotto l'HUD pH
            panelRect.sizeDelta = new Vector2(300f, 150f);
            
            tooltipPanel = _tooltipInstance;
        }
        
        // Assicurati che il rich text sia abilitato (anche se tooltipText è assegnato manualmente)
        if (tooltipText != null)
        {
            tooltipText.richText = true;
        }
        
        // Nascondi inizialmente
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Chiamato quando il mouse entra nell'HUD pH
    /// </summary>
    private void OnPhHUDHoverEnter()
    {
        _isHovering = true;
        
        if (_phSystem == null)
        {
            TryGetPhSystem();
        }
        
        if (_phSystem != null && tooltipPanel != null && tooltipText != null)
        {
            // Mostra il tooltip
            tooltipPanel.SetActive(true);
            UpdateTooltipContent();
        }
    }
    
    /// <summary>
    /// Chiamato quando il mouse esce dall'HUD pH
    /// </summary>
    private void OnPhHUDHoverExit()
    {
        _isHovering = false;
        
        // Nascondi il tooltip
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Aggiorna il contenuto del tooltip con il calcolo corrente
    /// </summary>
    private void UpdateTooltipContent()
    {
        if (_phSystem == null || tooltipText == null) return;
        
        // Assicurati che il rich text sia sempre abilitato
        tooltipText.richText = true;
        
        string calculation = _phSystem.GetCalculationBreakdown();
        Color tooltipColor = _phSystem.GetBandColor();
        
        // Applica colore al testo (leggermente più chiaro per leggibilità)
        tooltipText.color = new Color(tooltipColor.r * 0.8f + 0.2f, tooltipColor.g * 0.8f + 0.2f, tooltipColor.b * 0.8f + 0.2f, 1f);
        tooltipText.text = calculation;
    }
    
    /// <summary>
    /// Crea automaticamente gli elementi UI se mancanti
    /// </summary>
    private void AutoSetupUI()
    {
        // Trova o crea Canvas - IMPORTANTE: deve essere Screen Space Overlay per non seguire il player
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        _targetCanvas = null;
        
        // Cerca un Canvas Screen Space Overlay esistente
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _targetCanvas = canvas;
                Debug.Log($"[HUDPhDisplay] Trovato Canvas Screen Space Overlay: {canvas.name}");
                break;
            }
        }
        
        // Se non trovato, crea un nuovo Canvas Screen Space Overlay
        if (_targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas_HUDpH");
            _targetCanvas = canvasGO.AddComponent<Canvas>();
            _targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _targetCanvas.sortingOrder = 100; // Alto z-order per essere sopra tutto
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Debug.Log("[HUDPhDisplay] Creato nuovo Canvas Screen Space Overlay per pH HUD");
            
            // Crea EventSystem se mancante
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        
        // Crea phValueText se mancante
        if (phValueText == null)
        {
            GameObject valueTextGO = new GameObject("pH_ValueText");
            valueTextGO.transform.SetParent(_targetCanvas.transform, false);
            phValueText = valueTextGO.AddComponent<TextMeshProUGUI>();
            phValueText.text = "pH: --";
            phValueText.fontSize = 24;
            phValueText.color = Color.white;
            phValueText.alignment = TextAlignmentOptions.Center;
            phValueText.raycastTarget = enableHoverTooltip; // Abilita hover se tooltip attivo
            
            // Posiziona in alto al centro
            RectTransform valueRect = phValueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 1f);
            valueRect.anchorMax = new Vector2(0.5f, 1f);
            valueRect.pivot = new Vector2(0.5f, 1f);
            valueRect.anchoredPosition = uiPosition;
            valueRect.sizeDelta = new Vector2(200f, 30f);
        }
        
        // Crea phBandText se mancante
        if (phBandText == null)
        {
            GameObject bandTextGO = new GameObject("pH_BandText");
            bandTextGO.transform.SetParent(_targetCanvas.transform, false);
            phBandText = bandTextGO.AddComponent<TextMeshProUGUI>();
            phBandText.text = "Banda: --";
            phBandText.fontSize = 18;
            phBandText.color = Color.white;
            phBandText.alignment = TextAlignmentOptions.Center;
            phBandText.raycastTarget = enableHoverTooltip; // Abilita hover se tooltip attivo
            
            // Posiziona sotto il valore pH (centrato)
            RectTransform bandRect = phBandText.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0.5f, 1f);
            bandRect.anchorMax = new Vector2(0.5f, 1f);
            bandRect.pivot = new Vector2(0.5f, 1f);
            bandRect.anchoredPosition = uiPosition + new Vector2(0f, -35f);
            bandRect.sizeDelta = new Vector2(200f, 25f);
        }
        
        // phIndicatorImage è opzionale, non lo creiamo automaticamente
    }
    
    private void Update()
    {
        // Aggiorna periodicamente per catturare cambiamenti
        if (Time.time - _lastUpdateTime >= updateInterval)
        {
            UpdatePhDisplay();
            _lastUpdateTime = Time.time;
        }
        
        // Aggiorna tooltip se visibile e hover attivo
        if (_isHovering && tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdateTooltipContent();
        }
    }
    
    private void TryGetPhSystem()
    {
        try
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null && serviceContainer.Contains(typeof(PhSystem)))
            {
                _phSystem = serviceContainer.Get<PhSystem>();
                
                // Sottoscrivi agli eventi se disponibili
                if (_phSystem != null)
                {
                    _phSystem.OnPhChanged += OnPhChanged;
                }
            }
            else
            {
                // Fallback: cerca PhSystemDebugConsole che potrebbe avere il sistema
                var debugConsole = FindObjectOfType<PhSystemDebugConsole>();
                if (debugConsole != null)
                {
                    // Il debug console ha accesso al sistema pH
                    // Per ora creiamo un sistema temporaneo
                    _phSystem = new PhSystem(0f);
                    _phSystem.Reset(); // Reset esplicito per assicurarsi che sia a 0.0
                }
                else
                {
                    // Crea sistema temporaneo
                    _phSystem = new PhSystem(0f);
                    _phSystem.Reset(); // Reset esplicito per assicurarsi che sia a 0.0
                }
            }
        }
        catch
        {
            // Fallback: sistema temporaneo
            _phSystem = new PhSystem(0f);
            _phSystem.Reset(); // Reset esplicito per assicurarsi che sia a 0.0
        }
    }
    
    private void OnPhChanged(float newPh, float delta)
    {
        UpdatePhDisplay();
    }
    
    public void UpdatePhDisplay()
    {
        if (_phSystem == null)
        {
            TryGetPhSystem();
        }
        
        if (_phSystem == null)
        {
            // Fallback: mostra valori di default
            if (phValueText != null)
                phValueText.text = "pH: --";
            if (phBandText != null)
                phBandText.text = "Banda: --";
            return;
        }
        
        float currentPh = _phSystem.CurrentPh;
        string bandName = _phSystem.GetBandName();
        Color bandColor = _phSystem.GetBandColor();
        
        // Aggiorna testo valore pH
        if (phValueText != null)
        {
            phValueText.text = $"pH: {currentPh:F1}";
            phValueText.color = bandColor;
        }
        
        // Aggiorna testo banda pH
        if (phBandText != null)
        {
            phBandText.text = $"Banda: {bandName}";
            phBandText.color = bandColor;
        }
        
        // Aggiorna indicatore visivo (se presente)
        if (phIndicatorImage != null)
        {
            phIndicatorImage.color = bandColor;
            
            // Opzionale: scala la barra in base al valore pH (-100 a +100)
            float normalizedPh = (currentPh + 100f) / 200f; // Normalizza da 0 a 1
            phIndicatorImage.fillAmount = normalizedPh;
        }
    }
    
    /// <summary>
    /// Imposta manualmente il riferimento al PhSystem (per integrazione futura)
    /// </summary>
    public void SetPhSystem(PhSystem phSystem)
    {
        if (_phSystem != null)
        {
            _phSystem.OnPhChanged -= OnPhChanged;
        }
        
        _phSystem = phSystem;
        
        if (_phSystem != null)
        {
            _phSystem.OnPhChanged += OnPhChanged;
            UpdatePhDisplay();
        }
    }
    
    private void OnDestroy()
    {
        if (_phSystem != null)
        {
            _phSystem.OnPhChanged -= OnPhChanged;
        }
    }
    }
}
