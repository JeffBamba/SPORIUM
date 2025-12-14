using _Project.Sporae.Core;
using _Project;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sporae.Dome.PotSystem;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.UI;
using Sporae.DevTools;

/// <summary>
/// Sistema di HUD minimale sempre visibili per tutti i pot.
/// Crea e gestisce fino a 4 HUD minimale posizionate in basso a destra dello schermo.
/// </summary>
public class AlwaysVisiblePotHUD : MonoBehaviour
{
    [Header("HUD Settings")]
    [Tooltip("Posizione iniziale in basso a destra (offset da bottom-right corner)")]
    [SerializeField] private Vector2 bottomRightOffset = new Vector2(-20, 20);
    [Tooltip("Dimensione di ogni HUD minimale")]
    [SerializeField] private Vector2 minimalHUDSize = new Vector2(200, 80);
    [Tooltip("Spaziatura tra le HUD")]
    [SerializeField] private Vector2 hudSpacing = new Vector2(10, 10);
    [Tooltip("Layout: Vertical (colonna) o Horizontal (riga)")]
    [SerializeField] private bool verticalLayout = true;
    [Tooltip("Colore di background delle HUD")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [Tooltip("Colore del testo")]
    [SerializeField] private Color textColor = Color.white;
    
    [Header("References")]
    [Tooltip("Riferimento al pannello dettagliato (opzionale, cercato automaticamente se non assegnato)")]
    [SerializeField] private PotDetailsWidget potDetailsWidget;
    
    [Header("Prefab (Optional)")]
    [Tooltip("Prefab della HUD minimale (se assegnato, viene istanziato invece di creare dinamicamente)")]
    [SerializeField] private GameObject minimalHUDPrefab;
    
    [Header("Growth Tooltip")]
    [Tooltip("Prefab del tooltip Growth (opzionale, creato dinamicamente se non assegnato)")]
    [SerializeField] private GameObject growthTooltipPrefab;
    
    private Canvas _parentCanvas;
    private bool _isInitialized;
    
    // Sistema HUD sempre visibili per tutti i pot
    private class MinimalHUDInstance
    {
        public PotSlot pot;
        public GameObject container;
        public Image plantPreviewImage;
        public TextMeshProUGUI potIdText;  // Nome del Pot (es. POT-001)
        public TextMeshProUGUI plantNameAndLevelText;
        public TextMeshProUGUI conditionText;
        public TextMeshProUGUI growthStateText;
        public TextMeshProUGUI phDriftText;  // pH drift della pianta
        public Image waterCircle;
        public Image ledCircle;
        
        // Tooltip Growth
        public GameObject growthTooltipPanel;
        public TextMeshProUGUI growthTooltipText;
    }
    private MinimalHUDInstance[] _alwaysVisibleHUDs = new MinimalHUDInstance[4];
    
    private DayCycleSystem _dayCycleSystem;
    private PhSystem _phSystem;
    private PotSystemConfig _potSystemConfig;
    private PlantGrowthConfig _growthConfig;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        _phSystem = ServiceContainer.Instance.Get<PhSystem>(suppressWarning: true);
        _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
        _growthConfig = Resources.Load<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
        
        _parentCanvas = FindParentCanvas();
        
        if (_parentCanvas == null)
        {
            SporiumLogger.LogError(LogCategory.UI, "Impossibile trovare un Canvas. HUD sempre visibili disabilitate.");
            enabled = false;
            return;
        }
        
        // Cerca PotDetailsWidget se non assegnato
        if (potDetailsWidget == null)
        {
            potDetailsWidget = FindObjectOfType<PotDetailsWidget>();
        }
        
        // Crea le 4 HUD minimale sempre visibili
        CreateAlwaysVisibleMinimalHUDs();
        
        // Sottoscrivi agli eventi per aggiornare le HUD
        PotEvents.OnPotStateChanged += OnPotStateChanged;
        PotEvents.OnPlantGrew += OnPlantGrew;
        PotEvents.OnPlantStageChanged += OnPlantStageChanged;
        
        if (_dayCycleSystem != null)
        {
            _dayCycleSystem.OnDayChanged += HandleDayChanged;
        }
        
        _isInitialized = true;
        SporiumLogger.LogInfo(LogCategory.UI, "AlwaysVisiblePotHUD inizializzato correttamente.");
    }
    
    private void OnDestroy()
    {
        PotEvents.OnPotStateChanged -= OnPotStateChanged;
        PotEvents.OnPlantGrew -= OnPlantGrew;
        PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
        
        if (_dayCycleSystem != null)
        {
            _dayCycleSystem.OnDayChanged -= HandleDayChanged;
        }
    }
    
    private void Update()
    {
        // Aggiorna le HUD sempre visibili periodicamente (ogni 0.5 secondi invece di ogni frame per performance)
        if (_isInitialized)
        {
            // Usa un timer per non aggiornare ogni frame
            if (Time.time % 0.5f < Time.deltaTime)
            {
                UpdateAllAlwaysVisibleHUDs();
            }
        }
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
                SporiumLogger.LogDebug(LogCategory.UI, "Trovato Canvas dell'HUD esistente.");
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
                SporiumLogger.LogDebug(LogCategory.UI, $"Trovato Canvas: {canvas.name}");
                return canvas;
            }
        }
        
        SporiumLogger.LogWarning(LogCategory.UI, "Nessun Canvas trovato nella scena.");
        return null;
    }
    
    /// <summary>
    /// Crea 4 HUD minimale sempre visibili in basso a destra per tutti i pot
    /// </summary>
    private void CreateAlwaysVisibleMinimalHUDs()
    {
        // Trova tutti i pot nella scena
        PotSlot[] allPots = FindObjectsOfType<PotSlot>();
        
        if (allPots == null || allPots.Length == 0)
        {
            SporiumLogger.LogWarning(LogCategory.UI, "Nessun pot trovato nella scena. HUD minimale non create.");
            return;
        }
        
        // Ordina i pot per PotId (POT-001, POT-002, etc.)
        System.Array.Sort(allPots, (a, b) => string.Compare(a.PotId, b.PotId));
        
        // Crea fino a 4 HUD (una per pot)
        int hudCount = Mathf.Min(allPots.Length, 4);
        
        for (int i = 0; i < hudCount; i++)
        {
            if (allPots[i] == null) continue;
            
            _alwaysVisibleHUDs[i] = new MinimalHUDInstance
            {
                pot = allPots[i]
            };
            
            if (minimalHUDPrefab != null)
            {
                CreateHUDFromPrefab(_alwaysVisibleHUDs[i], i, hudCount);
            }
            else
            {
                CreateSingleMinimalHUD(_alwaysVisibleHUDs[i], i, hudCount);
            }
        }
        
        SporiumLogger.LogInfo(LogCategory.UI, $"Create {hudCount} always visible minimal HUDs in bottom-right corner.");
    }
    
    /// <summary>
    /// Crea una HUD da prefab se assegnato
    /// </summary>
    private void CreateHUDFromPrefab(MinimalHUDInstance hudInstance, int index, int totalCount)
    {
        if (_parentCanvas == null || minimalHUDPrefab == null) return;
        
        // Istantzia il prefab
        GameObject container = Instantiate(minimalHUDPrefab, _parentCanvas.transform);
        container.name = $"MinimalHUD_{hudInstance.pot.PotId}";
        hudInstance.container = container;
        
        // Posiziona in basso a destra
        RectTransform containerRect = container.GetComponent<RectTransform>();
        
        // IMPORTANTE: Leggi le dimensioni REALI del prefab PRIMA di modificare anchors/pivot
        // Usa rect.size che restituisce le dimensioni effettive indipendentemente da come sono definite
        Vector2 actualSize = containerRect.rect.size;
        
        // Se le dimensioni sono zero o molto piccole, usa quelle di default
        if (actualSize.x <= 1 || actualSize.y <= 1)
        {
            actualSize = minimalHUDSize;
            containerRect.sizeDelta = actualSize;
        }
        
        // Imposta anchors e pivot per posizionamento in basso a destra
        containerRect.anchorMin = new Vector2(1f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f);
        
        // Calcola posizione in base all'indice e al layout usando le dimensioni REALI del prefab
        float xOffset = bottomRightOffset.x;
        float yOffset = bottomRightOffset.y;
        
        if (verticalLayout)
        {
            // Layout verticale: usa l'altezza reale del prefab
            yOffset += index * (actualSize.y + hudSpacing.y);
        }
        else
        {
            // Layout orizzontale: usa la larghezza reale del prefab
            xOffset -= index * (actualSize.x + hudSpacing.x);
        }
        
        containerRect.anchoredPosition = new Vector2(xOffset, yOffset);
        
        // Cerca gli elementi UI nel prefab
        hudInstance.plantPreviewImage = container.transform.Find("PlantPreviewImage")?.GetComponent<Image>();
        hudInstance.potIdText = container.transform.Find("PotIdText")?.GetComponent<TextMeshProUGUI>();
        hudInstance.plantNameAndLevelText = container.transform.Find("PlantNameAndLevelText")?.GetComponent<TextMeshProUGUI>();
        hudInstance.conditionText = container.transform.Find("ConditionText")?.GetComponent<TextMeshProUGUI>();
        hudInstance.growthStateText = container.transform.Find("GrowthStateText")?.GetComponent<TextMeshProUGUI>();
        hudInstance.phDriftText = container.transform.Find("PhDriftText")?.GetComponent<TextMeshProUGUI>();
        hudInstance.waterCircle = container.transform.Find("WaterCircle")?.GetComponent<Image>();
        hudInstance.ledCircle = container.transform.Find("LedCircle")?.GetComponent<Image>();
        
        // Setup tooltip Growth
        SetupGrowthTooltip(hudInstance);
        
        // Aggiorna immediatamente
        UpdateSingleMinimalHUD(hudInstance);
    }
    
    /// <summary>
    /// Crea una singola HUD minimale sempre visibile dinamicamente
    /// </summary>
    private void CreateSingleMinimalHUD(MinimalHUDInstance hudInstance, int index, int totalCount)
    {
        if (_parentCanvas == null) return;
        
        // Crea container
        GameObject container = new GameObject($"MinimalHUD_{hudInstance.pot.PotId}");
        container.transform.SetParent(_parentCanvas.transform, false);
        hudInstance.container = container;
        
        // Aggiungi RectTransform
        RectTransform containerRect = container.AddComponent<RectTransform>();
        
        // Posiziona in basso a destra
        containerRect.anchorMin = new Vector2(1f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f);
        
        // Calcola posizione in base all'indice e al layout
        float xOffset = bottomRightOffset.x;
        float yOffset = bottomRightOffset.y;
        
        if (verticalLayout)
        {
            // Layout verticale: una sopra l'altra
            yOffset += index * (minimalHUDSize.y + hudSpacing.y);
        }
        else
        {
            // Layout orizzontale: una accanto all'altra
            xOffset -= index * (minimalHUDSize.x + hudSpacing.x);
        }
        
        containerRect.anchoredPosition = new Vector2(xOffset, yOffset);
        containerRect.sizeDelta = minimalHUDSize;
        
        // Aggiungi background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(container.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = backgroundColor;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // 1. Anteprima immagine pianta (50x50, in alto a sinistra)
        GameObject previewGO = new GameObject("PlantPreviewImage");
        previewGO.transform.SetParent(container.transform, false);
        hudInstance.plantPreviewImage = previewGO.AddComponent<Image>();
        hudInstance.plantPreviewImage.color = Color.white;
        RectTransform previewRect = previewGO.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0f, 0.5f);
        previewRect.anchorMax = new Vector2(0f, 0.5f);
        previewRect.pivot = new Vector2(0f, 0.5f);
        previewRect.anchoredPosition = new Vector2(5, 0);
        previewRect.sizeDelta = new Vector2(50, 50);
        
        // 2. PotId (nome del Pot, in alto)
        GameObject potIdGO = new GameObject("PotIdText");
        potIdGO.transform.SetParent(container.transform, false);
        hudInstance.potIdText = potIdGO.AddComponent<TextMeshProUGUI>();
        hudInstance.potIdText.color = textColor;
        hudInstance.potIdText.fontSize = 12;
        hudInstance.potIdText.fontStyle = FontStyles.Bold;
        hudInstance.potIdText.alignment = TextAlignmentOptions.Left;
        hudInstance.potIdText.text = hudInstance.pot.PotId;
        RectTransform potIdRect = potIdGO.GetComponent<RectTransform>();
        potIdRect.anchorMin = new Vector2(0f, 0.85f);
        potIdRect.anchorMax = new Vector2(1f, 1f);
        potIdRect.pivot = new Vector2(0f, 0.5f);
        potIdRect.anchoredPosition = new Vector2(60, 0);
        potIdRect.sizeDelta = new Vector2(-65, 15);
        
        // 3. Nome Pianta + Livello (sotto PotId)
        GameObject nameGO = new GameObject("PlantNameAndLevelText");
        nameGO.transform.SetParent(container.transform, false);
        hudInstance.plantNameAndLevelText = nameGO.AddComponent<TextMeshProUGUI>();
        hudInstance.plantNameAndLevelText.color = textColor;
        hudInstance.plantNameAndLevelText.fontSize = 14;
        hudInstance.plantNameAndLevelText.fontStyle = FontStyles.Bold;
        hudInstance.plantNameAndLevelText.alignment = TextAlignmentOptions.Left;
        hudInstance.plantNameAndLevelText.text = "Vuoto";
        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.7f);
        nameRect.anchorMax = new Vector2(1f, 0.85f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.anchoredPosition = new Vector2(60, 0);
        nameRect.sizeDelta = new Vector2(-65, 15);
        
        // 4. Condizione (sotto il nome)
        GameObject conditionGO = new GameObject("ConditionText");
        conditionGO.transform.SetParent(container.transform, false);
        hudInstance.conditionText = conditionGO.AddComponent<TextMeshProUGUI>();
        hudInstance.conditionText.color = textColor;
        hudInstance.conditionText.fontSize = 11;
        hudInstance.conditionText.alignment = TextAlignmentOptions.Left;
        hudInstance.conditionText.text = "-";
        RectTransform conditionRect = conditionGO.GetComponent<RectTransform>();
        conditionRect.anchorMin = new Vector2(0f, 0.55f);
        conditionRect.anchorMax = new Vector2(1f, 0.7f);
        conditionRect.pivot = new Vector2(0f, 0.5f);
        conditionRect.anchoredPosition = new Vector2(60, 0);
        conditionRect.sizeDelta = new Vector2(-65, 15);
        
        // 5. Stato Crescita (sotto la condizione) - con tooltip
        GameObject growthGO = new GameObject("GrowthStateText");
        growthGO.transform.SetParent(container.transform, false);
        hudInstance.growthStateText = growthGO.AddComponent<TextMeshProUGUI>();
        hudInstance.growthStateText.color = textColor;
        hudInstance.growthStateText.fontSize = 11;
        hudInstance.growthStateText.alignment = TextAlignmentOptions.Left;
        hudInstance.growthStateText.text = "-";
        RectTransform growthRect = growthGO.GetComponent<RectTransform>();
        growthRect.anchorMin = new Vector2(0f, 0.4f);
        growthRect.anchorMax = new Vector2(1f, 0.55f);
        growthRect.pivot = new Vector2(0f, 0.5f);
        growthRect.anchoredPosition = new Vector2(60, 0);
        growthRect.sizeDelta = new Vector2(-65, 15);
        
        // 6. pH Drift (sotto lo stato crescita)
        GameObject phDriftGO = new GameObject("PhDriftText");
        phDriftGO.transform.SetParent(container.transform, false);
        hudInstance.phDriftText = phDriftGO.AddComponent<TextMeshProUGUI>();
        hudInstance.phDriftText.color = textColor;
        hudInstance.phDriftText.fontSize = 10;
        hudInstance.phDriftText.richText = true;
        hudInstance.phDriftText.alignment = TextAlignmentOptions.Left;
        hudInstance.phDriftText.text = "pH: -";
        RectTransform phDriftRect = phDriftGO.GetComponent<RectTransform>();
        phDriftRect.anchorMin = new Vector2(0f, 0.25f);
        phDriftRect.anchorMax = new Vector2(1f, 0.4f);
        phDriftRect.pivot = new Vector2(0f, 0.5f);
        phDriftRect.anchoredPosition = new Vector2(60, 0);
        phDriftRect.sizeDelta = new Vector2(-65, 15);
        
        // 7. Cerchio Water (in basso a sinistra, accanto all'immagine)
        GameObject waterGO = new GameObject("WaterCircle");
        waterGO.transform.SetParent(container.transform, false);
        hudInstance.waterCircle = waterGO.AddComponent<Image>();
        hudInstance.waterCircle.color = Color.gray;
        RectTransform waterRect = waterGO.GetComponent<RectTransform>();
        waterRect.anchorMin = new Vector2(0f, 0f);
        waterRect.anchorMax = new Vector2(0f, 0f);
        waterRect.pivot = new Vector2(0.5f, 0.5f);
        waterRect.anchoredPosition = new Vector2(20, 15);
        waterRect.sizeDelta = new Vector2(16, 16);
        
        // 8. Cerchio LED (accanto al cerchio Water)
        GameObject ledGO = new GameObject("LedCircle");
        ledGO.transform.SetParent(container.transform, false);
        hudInstance.ledCircle = ledGO.AddComponent<Image>();
        hudInstance.ledCircle.color = Color.gray;
        RectTransform ledRect = ledGO.GetComponent<RectTransform>();
        ledRect.anchorMin = new Vector2(0f, 0f);
        ledRect.anchorMax = new Vector2(0f, 0f);
        ledRect.pivot = new Vector2(0.5f, 0.5f);
        ledRect.anchoredPosition = new Vector2(40, 15);
        ledRect.sizeDelta = new Vector2(16, 16);
        
        // Setup tooltip Growth
        SetupGrowthTooltip(hudInstance);
        
        // Aggiorna immediatamente
        UpdateSingleMinimalHUD(hudInstance);
    }
    
    /// <summary>
    /// Aggiorna una singola HUD minimale sempre visibile
    /// </summary>
    private void UpdateSingleMinimalHUD(MinimalHUDInstance hudInstance)
    {
        if (hudInstance == null || hudInstance.pot == null || hudInstance.pot.PotActions == null)
        {
            // Nascondi se il pot non esiste più
            if (hudInstance != null && hudInstance.container != null)
            {
                hudInstance.container.SetActive(false);
            }
            return;
        }
        
        PotStateModel state = hudInstance.pot.PotActions.GetCurrentState();
        if (state == null)
        {
            if (hudInstance.container != null)
                hudInstance.container.SetActive(false);
            return;
        }
        
        // Assicurati che il container sia visibile
        if (hudInstance.container != null)
            hudInstance.container.SetActive(true);
        
        // Gestione speciale per vasi vuoti: resetta tutti i testi
        if (state.IsEmpty || state.Stage == (int)PlantStage.Empty)
        {
            // 1. Anteprima immagine pianta (grigia per Empty)
            if (hudInstance.plantPreviewImage != null)
            {
                hudInstance.plantPreviewImage.color = GetStageColor((int)PlantStage.Empty);
                if (hudInstance.pot.Sprite != null)
                {
                    hudInstance.plantPreviewImage.sprite = hudInstance.pot.Sprite;
                }
            }
            
            // 1. PotId (sempre visibile)
            if (hudInstance.potIdText != null)
            {
                hudInstance.potIdText.text = hudInstance.pot.PotId;
            }
            
            // 2. Nome Pianta + Livello
            if (hudInstance.plantNameAndLevelText != null)
            {
                hudInstance.plantNameAndLevelText.text = "Vuoto";
            }
            
            // 3. Condizione (vuota quando il vaso è vuoto)
            if (hudInstance.conditionText != null)
            {
                hudInstance.conditionText.text = "-";
            }
            
            // 4. Stato Crescita (vuoto quando il vaso è vuoto)
            if (hudInstance.growthStateText != null)
            {
                hudInstance.growthStateText.text = "-";
            }
            
            // 5. pH Drift (vuoto quando il vaso è vuoto)
            if (hudInstance.phDriftText != null)
            {
                hudInstance.phDriftText.text = "pH: -";
            }
            
            // 6. Cerchio Water (grigio quando vuoto)
            if (hudInstance.waterCircle != null)
            {
                hudInstance.waterCircle.color = Color.gray;
            }
            
            // 7. Cerchio LED (grigio quando vuoto)
            if (hudInstance.ledCircle != null)
            {
                hudInstance.ledCircle.color = Color.gray;
            }
            
            return; // Esci subito, non processare ulteriormente
        }
        
        // Vaso con pianta: aggiorna normalmente
        // 1. PotId (sempre visibile)
        if (hudInstance.potIdText != null)
        {
            hudInstance.potIdText.text = hudInstance.pot.PotId;
        }
        
        // 2. Anteprima immagine pianta
        if (hudInstance.plantPreviewImage != null)
        {
            hudInstance.plantPreviewImage.color = GetStageColor(state.Stage);
            if (hudInstance.pot.Sprite != null)
            {
                hudInstance.plantPreviewImage.sprite = hudInstance.pot.Sprite;
            }
        }
        
        // 3. Nome Pianta + Livello
        if (hudInstance.plantNameAndLevelText != null)
        {
            string plantName = GetPlantDisplayName(state.PlantCode);
            if (string.IsNullOrEmpty(plantName))
            {
                plantName = "Pianta Sconosciuta";
            }
            string levelText = $" Lv.{state.PlantLevel}";
            hudInstance.plantNameAndLevelText.text = $"{plantName}{levelText}";
        }
        
        // 4. Condizione
        if (hudInstance.conditionText != null)
        {
            PlantData plantData = state?.GetPlantData();
            ConditionResult result;
            
            if (plantData != null && _phSystem != null && _potSystemConfig != null)
            {
                result = PlantConditionSystem.CalculateCondition(
                    state,
                    plantData,
                    _phSystem,
                    _potSystemConfig,
                    _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 0,
                    state.PreviousDayConditionScore >= 0 ? state.PreviousDayConditionScore : state.ConditionScore);
                
                int maxHydration = _potSystemConfig?.MaxHydration ?? 5;
                bool isOverwatering = PlantConditionSystem.IsOverwatering(state, maxHydration);
                string conditionName = PlantConditionSystem.GetConditionName(result.Condition, isOverwatering);
                hudInstance.conditionText.text = conditionName;
            }
            else
            {
                PlantCondition condition = (PlantCondition)state.ConditionLabel;
                int maxHydration = _potSystemConfig?.MaxHydration ?? 5;
                bool isOverwatering = PlantConditionSystem.IsOverwatering(state, maxHydration);
                string conditionName = PlantConditionSystem.GetConditionName(condition, isOverwatering);
                hudInstance.conditionText.text = conditionName;
            }
        }
        
        // 5. Stato Crescita
        if (hudInstance.growthStateText != null)
        {
            string growthStatus = GetGrowthStatus(state);
            hudInstance.growthStateText.text = $"Growth: {growthStatus}";
        }
        
        // 6. pH Drift
        if (hudInstance.phDriftText != null)
        {
            if (!string.IsNullOrEmpty(state.PlantCode))
            {
                var plantDatabase = PlantDatabase.Instance;
                if (plantDatabase != null)
                {
                    var plantData = plantDatabase.GetPlantDataByCode(state.PlantCode);
                    if (plantData != null)
                    {
                        float phDrift = plantData.GetDailyPhDrift();
                        string colorTag;
                        if (phDrift > 0)
                            colorTag = "<color=#4DCC4D>"; // Verde per drift positivo (Pure)
                        else if (phDrift < 0)
                            colorTag = "<color=#CC4D4D>"; // Rosso per drift negativo (Evil)
                        else
                            colorTag = "<color=#999999>"; // Grigio per drift zero (Standard)
                        
                        hudInstance.phDriftText.text = $"<color=#CCCCCC>pH:</color> {colorTag}{phDrift:+#;-#;0}/giorno</color>";
                    }
                    else
                    {
                        hudInstance.phDriftText.text = "pH: -";
                    }
                }
                else
                {
                    hudInstance.phDriftText.text = "pH: -";
                }
            }
            else
            {
                hudInstance.phDriftText.text = "pH: -";
            }
        }
        
        // 5. Cerchio Water
        if (hudInstance.waterCircle != null)
        {
            hudInstance.waterCircle.color = state.WateringSystemOn ? new Color(0.2f, 0.6f, 1f) : Color.gray;
        }
        
        // 6. Cerchio LED
        if (hudInstance.ledCircle != null)
        {
            bool ledOn = state.LedSystemState != LedSystemState.Off;
            hudInstance.ledCircle.color = ledOn ? new Color(1f, 0.8f, 0.2f) : Color.gray;
        }
    }
    
    /// <summary>
    /// Aggiorna tutte le HUD minimale sempre visibili
    /// </summary>
    private void UpdateAllAlwaysVisibleHUDs()
    {
        for (int i = 0; i < _alwaysVisibleHUDs.Length; i++)
        {
            if (_alwaysVisibleHUDs[i] != null)
            {
                UpdateSingleMinimalHUD(_alwaysVisibleHUDs[i]);
            }
        }
    }
    
    private void HandleDayChanged(int currentDay)
    {
        // Aggiorna le HUD sempre visibili quando cambia il giorno
        UpdateAllAlwaysVisibleHUDs();
    }
    
    private void OnPotStateChanged(PotSlot pot)
    {
        if (!_isInitialized) return;
        
        // Aggiorna le HUD sempre visibili quando cambia lo stato di un pot
        UpdateAllAlwaysVisibleHUDs();
    }
    
    private void OnPlantGrew(string potId, PlantStage stage, int oldPoints, int newPoints)
    {
        if (!_isInitialized) return;
        
        // Aggiorna le HUD sempre visibili quando una pianta cresce
        UpdateAllAlwaysVisibleHUDs();
    }
    
    private void OnPlantStageChanged(string potId, PlantStage stage)
    {
        if (!_isInitialized) return;
        
        // Aggiorna le HUD sempre visibili quando cambia lo stadio
        UpdateAllAlwaysVisibleHUDs();
    }
    
    /// <summary>
    /// Ottiene il nome visualizzabile della pianta dal PlantCode
    /// </summary>
    private string GetPlantDisplayName(string plantCode)
    {
        if (string.IsNullOrEmpty(plantCode))
            return null;
        
        // Prova a ottenere il nome dal PlantDatabase
        var plantDatabase = PlantDatabase.Instance;
        if (plantDatabase != null)
        {
            var plantData = plantDatabase.GetPlantDataByCode(plantCode);
            if (plantData != null)
            {
                // Usa il nome del PlantData (rimuovi prefisso PLT- e sostituisci - con spazi)
                return plantData.name.Replace("PLT-", "").Replace("-", " ");
            }
        }
        
        // Fallback: mappa manuale per piante conosciute
        switch (plantCode)
        {
            case "PLT-STD-001":
                return "Ferric Fern";
            case "PLT-PURE-001":
                return "Arctic Hask";
            case "PLT-EVIL-001":
                return "Glasscap Fungus";
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Restituisce il colore per uno stadio
    /// </summary>
    private Color GetStageColor(int stage)
    {
        switch (stage)
        {
            case (int)PlantStage.Empty:
                return Color.gray;
            case (int)PlantStage.Seed:
                return new Color(0.6f, 0.4f, 0.2f); // Brown color
            case (int)PlantStage.Sprout:
                return Color.green;
            case (int)PlantStage.HarvestReady:
                return Color.yellow;
            case (int)PlantStage.Resting:
                return new Color(0.6f, 0.6f, 0.8f); // Grigio-blu
            default:
                return Color.white;
        }
    }
    
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
            case 3: return "HarvestReady";
            case 4: return "Flowering";
            case 5: return "HarvestReady";
            case 6: return "Resting";
            default: return $"Stadio {stage}";
        }
    }
    
    /// <summary>
    /// Determina lo stato di crescita della pianta (IN CRESCITA, Stabile, Difficoltà)
    /// </summary>
    private string GetGrowthStatus(PotStateModel state)
    {
        if (state == null || state.IsEmpty || !state.HasPlant)
            return "Stabile";
        
        // Ottieni PlantData e StageRequirements
        PlantData plantData = state.GetPlantData();
        if (plantData == null || _potSystemConfig == null)
            return "Stabile";
        
        PlantStage currentStage = (PlantStage)state.Stage;
        StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
        if (stageReq == null)
            return "Stabile";
        
        // Calcola percentuale idratazione
        int maxHydration = _potSystemConfig.MaxHydration;
        int hydrationPercent = maxHydration > 0 ? 
            Mathf.RoundToInt((float)state.Hydration / maxHydration * 100f) : 0;
        
        // Verifica range per ogni parametro
        bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
        
        // Light OK basato su stress percentage (0% = OK) invece di LightExposure
        int consecutiveDays = state.GetConsecutiveLedDays();
        const int maxDaysForFullStress = 4;
        float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
        bool ledRequirementMet = stageReq.IsLedRequirementMet(state.LedSystemState);
        bool lightOk = stressPercentage == 0f && ledRequirementMet;
        
        bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
        
        // Determina stato in base ai parametri
        int paramsOk = (waterOk ? 1 : 0) + (lightOk ? 1 : 0) + (fertilizerOk ? 1 : 0);
        
        if (paramsOk == 3)
            return "IN CRESCITA";
        else if (paramsOk == 2 || paramsOk == 1)
            return "Stabile";
        else // paramsOk == 0
            return "Difficoltà";
    }
    
    /// <summary>
    /// Setup tooltip Growth per una HUD minimale
    /// </summary>
    private void SetupGrowthTooltip(MinimalHUDInstance hudInstance)
    {
        if (hudInstance == null || hudInstance.growthStateText == null)
            return;
        
        // Crea tooltip panel se non esiste
        if (hudInstance.growthTooltipPanel == null)
        {
            // Crea panel tooltip
            GameObject tooltipPanel = new GameObject("GrowthTooltipPanel");
            tooltipPanel.transform.SetParent(hudInstance.container.transform, false);
            hudInstance.growthTooltipPanel = tooltipPanel;
            
            // Aggiungi Image per background
            Image bgImage = tooltipPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.9f);
            
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0f, 1f);
            tooltipRect.anchoredPosition = new Vector2(10, -10);
            tooltipRect.sizeDelta = new Vector2(300, 200);
            
            // Aggiungi testo tooltip
            GameObject tooltipTextGO = new GameObject("TooltipText");
            tooltipTextGO.transform.SetParent(tooltipPanel.transform, false);
            hudInstance.growthTooltipText = tooltipTextGO.AddComponent<TextMeshProUGUI>();
            hudInstance.growthTooltipText.color = Color.white;
            hudInstance.growthTooltipText.fontSize = 12;
            hudInstance.growthTooltipText.richText = true;
            hudInstance.growthTooltipText.alignment = TextAlignmentOptions.Left;
            hudInstance.growthTooltipText.text = "Tooltip Growth";
            
            RectTransform textRect = tooltipTextGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 8);
            textRect.offsetMax = new Vector2(-8, -8);
            
            tooltipPanel.SetActive(false);
        }
        
        // Aggiungi EventTrigger alla label Growth
        EventTrigger trigger = hudInstance.growthStateText.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = hudInstance.growthStateText.gameObject.AddComponent<EventTrigger>();
        }
        
        trigger.triggers.Clear();
        
        // PointerEnter - mostra tooltip
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            if (hudInstance.pot != null && hudInstance.pot.PotActions != null)
            {
                PotStateModel state = hudInstance.pot.PotActions.GetCurrentState();
                if (state != null)
                {
                    UpdateGrowthTooltip(hudInstance, state);
                    if (hudInstance.growthTooltipPanel != null)
                    {
                        hudInstance.growthTooltipPanel.SetActive(true);
                    }
                }
            }
        });
        trigger.triggers.Add(enterEntry);
        
        // PointerExit - nascondi tooltip
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            if (hudInstance.growthTooltipPanel != null)
            {
                hudInstance.growthTooltipPanel.SetActive(false);
            }
        });
        trigger.triggers.Add(exitEntry);
    }
    
    /// <summary>
    /// Aggiorna tooltip Growth con dati attuali
    /// </summary>
    private void UpdateGrowthTooltip(MinimalHUDInstance hudInstance, PotStateModel state)
    {
        if (hudInstance.growthTooltipText == null || state == null)
            return;
        
        hudInstance.growthTooltipText.text = BuildGrowthTooltip(state);
    }
    
    /// <summary>
    /// Costruisce il tooltip di crescita (simile a PotDetailsWidget)
    /// </summary>
    private string BuildGrowthTooltip(PotStateModel state)
    {
        var sb = new System.Text.StringBuilder();
        
        if (_growthConfig == null || state == null || state.IsEmpty || !state.HasPlant)
        {
            sb.AppendLine("<b>Crescita: Informazioni non disponibili</b>");
            return sb.ToString();
        }
        
        // Ottieni PlantData e StageRequirements
        PlantData plantData = state.GetPlantData();
        if (plantData == null || _potSystemConfig == null)
        {
            sb.AppendLine("<b>Crescita: Dati pianta non disponibili</b>");
            return sb.ToString();
        }
        
        PlantStage currentStage = (PlantStage)state.Stage;
        StageRequirements stageReq = plantData.GetStageRequirements(currentStage);
        if (stageReq == null)
        {
            sb.AppendLine("<b>Crescita: Requisiti stadio non disponibili</b>");
            return sb.ToString();
        }
        
        // Calcola percentuale idratazione
        int maxHydration = _potSystemConfig.MaxHydration;
        int hydrationPercent = maxHydration > 0 ? 
            Mathf.RoundToInt((float)state.Hydration / maxHydration * 100f) : 0;
        
        // Verifica range per ogni parametro
        bool waterOk = stageReq.IsHydrationInRange(hydrationPercent);
        
        // Light OK basato su stress percentage (0% = OK)
        int consecutiveDays = state.GetConsecutiveLedDays();
        const int maxDaysForFullStress = 4;
        float stressPercentage = Mathf.Clamp01((float)consecutiveDays / maxDaysForFullStress) * 100f;
        bool ledRequirementMet = stageReq.IsLedRequirementMet(state.LedSystemState);
        bool lightOk = stressPercentage == 0f && ledRequirementMet;
        
        bool fertilizerOk = stageReq.IsFertilizerInRange(state.FertilizerLevel);
        
        // Determina stato
        string growthStatus = GetGrowthStatus(state);
        sb.AppendLine($"<b>Crescita: {growthStatus}</b>");
        sb.AppendLine();
        
        // Spiegazione semplice per il player
        sb.AppendLine("La pianta cresce quando si trova nel <color=#00FF00>range giusto</color> di:");
        sb.AppendLine();
        
        // Water
        string waterStatus = waterOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
        sb.AppendLine($"• <color=#3F6FFF>Acqua (Water)</color>: {waterStatus}");
        if (!waterOk)
        {
            sb.AppendLine($"  Range ideale: {stageReq.hydrationMin}% - {stageReq.hydrationMax}%");
            sb.AppendLine($"  Attuale: {hydrationPercent}%");
        }
        sb.AppendLine();
        
        // Light
        string lightStatus = lightOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
        sb.AppendLine($"• <color=#FFD700>Luce</color>: {lightStatus}");
        sb.AppendLine($"  Range ideale: <color=#00FF00>{stageReq.lightMin}%-{stageReq.lightMed}%-{stageReq.lightMax}%</color>");
        if (!lightOk)
        {
            sb.AppendLine($"  Attuale: {stressPercentage:F0}%");
            string ledRequired = stageReq.GetRequiredLed()?.ToString() ?? "Nessuno";
            if (ledRequired != "Nessuno" && !ledRequirementMet)
            {
                sb.AppendLine($"  LED richiesto: {ledRequired} (<color=#FF0000>NON OK</color>)");
            }
        }
        else
        {
            sb.AppendLine($"  Attuale: <color=#00FF00>{stressPercentage:F0}%</color>");
        }
        sb.AppendLine();
        
        // Fertilizer
        bool isFertilizerOptional = (currentStage == PlantStage.Seed || currentStage == PlantStage.Sprout);
        string fertilizerStatus = fertilizerOk ? "<color=#00FF00>OK</color>" : "<color=#FF0000>NON OK</color>";
        string fertilizerLabel = isFertilizerOptional ? 
            $"• <color=#90EE90>Fertilizzante</color> (opzionale): {fertilizerStatus}" :
            $"• <color=#90EE90>Fertilizzante</color>: {fertilizerStatus}";
        sb.AppendLine(fertilizerLabel);
        if (!fertilizerOk)
        {
            sb.AppendLine($"  Range ideale: {stageReq.fertilizerMin}% - {stageReq.fertilizerMax}%");
            sb.AppendLine($"  Attuale: {state.FertilizerLevel}%");
        }
        if (isFertilizerOptional)
        {
            sb.AppendLine($"  <color=#FFFF00>Nota: Negli stadi Seed e Sprout, il fertilizzante è opzionale per avanzare.</color>");
        }
        sb.AppendLine();
        
        // Giorni mancanti per avanzare
        int daysInStage = state.DaysInCurrentStage;
        int requiredDays = stageReq.durationDays;
        int daysRemaining = Mathf.Max(0, requiredDays - daysInStage);
        
        if (daysRemaining > 0)
        {
            sb.AppendLine($"<color=#FFFF00>Giorni mancanti per avanzare:</color> <color=#FFFFFF>{daysRemaining}</color>");
            sb.AppendLine($"  (Giorni nello stadio: {daysInStage} / {requiredDays})");
        }
        else
        {
            sb.AppendLine("<color=#00FF00>✓ Giorni minimi raggiunti!</color>");
            if (waterOk && lightOk && fertilizerOk)
            {
                sb.AppendLine("<color=#00FF00>✓ Tutti i parametri sono nel range ideale!</color>");
                sb.AppendLine("<color=#00FF00>La pianta può avanzare al prossimo stadio.</color>");
            }
            else
            {
                sb.AppendLine("<color=#FFFF00>⚠️ Metti tutti i parametri nel range ideale per avanzare.</color>");
            }
        }
        
        return sb.ToString();
    }
}

