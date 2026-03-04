using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using Sporae.UI.UIToolkit.HUD;
using Sporae.DevTools;
using _Project.Sporae.Core;
using _Project;
using Sporae.Core;
using Sporae.UI.UIToolkit.HUD.Components;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.Icons;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Controller per la TopBar HUD con tutte le metriche di gioco.
    /// </summary>
    public class TopBarController : MonoBehaviour
    {
        [Header("UI Toolkit References")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Game Metrics")]
        [SerializeField] private int _actionsLeft = 3;
        [SerializeField] private int _maxActions = 4;
        [SerializeField] private float _phLevel = 0f;
        [SerializeField] private float _condensation = 78f;
        [SerializeField] private float _mutationIndex = 0.42f;
        [SerializeField] private int _cryBalance = 1245;
        [SerializeField] private int _grateValue = 12;
        
        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogs = false;

        [Header("pH Tooltip Font (opzionale)")]
        [Tooltip("Assegna qui un override font per il titolo del tooltip pH. Se vuoto, eredita il font globale del tema UI.")]
        [SerializeField] private Font _phTooltipTitleFont;

        [Header("UI Glow Frame")]
        [SerializeField] private Material _glowFrameMaterial;
        [SerializeField] private bool _glowFrameLiveUpdate = true;
        
        // Suppress warning for unused field (used in Inspector)
        #pragma warning disable 0414
        
        // UI Elements
        private VisualElement _root;
        private VisualElement _iconActions;
        private VisualElement _iconActionsGlyph;
        private Label _actionsLabel;
        private SegmentedBarUI _actionsBar;
        private Label _actionsValueLabel;
        private VisualElement _phDisplay;
        private Label _phDriftLabel;
        private Label _phBandLabel;
        private VisualElement _phMarker;
        private VisualElement _phGradient;
        private VisualElement _phNeutralZone;
        private VisualElement _phSlider;
        private Label _condensationValueLabel;
        private VisualElement _condensationDisplay; // FASE 10: Per tooltip futuro
        private Label _mutationValueLabel;
        private Label _cryValueLabel;
        private Label _grateValueLabel;

        private VisualElement _glowFrame;
        private UiGlowFrameGenerator _glowFrameGenerator;
        private Material _glowFrameMaterialRuntime;
        private const string GlowShaderName = "Sporae/UI/GlowFrame";
        
        // Tooltip
        private VisualElement _phTooltip;
        private Label _phTooltipTitle;
        private VisualElement _phTooltipTitleIcon;
        private Label _phTooltipTitleStatus;
        private Label _phTooltipValueCurrent;
        private VisualElement _phTooltipModifiersList;
        private Label _phTooltipValueTotal;
        private Label _phTooltipValueEffects;
        private Label _phTooltipValueTip;
        private VisualElement[] _phTooltipCrtBars;
        private float[] _phCrtBarTop;
        private float[] _phCrtBarSpeed;
        private float _phCrtNextRandomizeTime;
        private int _phCrtVisibleCount;
        
        // FASE 8: Tooltip Condensation
        private VisualElement _condensationTooltip;
        private Label _condensationTooltipText;
        private Button _condensationCollectButton;
        
        // pH Gradient Texture (creata runtime)
        private Texture2D _phGradientTexture;
        
        // pH Color constants (0-14 scale)
        private static readonly Color PH_RED = new Color(1f, 0.165f, 0.165f, 1f); // #FF2A2A
        private static readonly Color PH_ORANGE = new Color(1f, 0.667f, 0.2f, 1f); // #FFAA33
        private static readonly Color PH_WHITE = new Color(0.961f, 0.969f, 0.980f, 1f); // #F5F7FA
        private static readonly Color PH_BLUE = new Color(0.2f, 0.722f, 1f, 1f); // #33B8FF
        private static readonly Color PH_PURPLE = new Color(0.357f, 0.310f, 1f, 1f); // #5B4FFF
        private static readonly Color PH_GLOW_RED = new Color(1f, 0.165f, 0.165f, 1f); // #FF2A2A
        private static readonly Color PH_GLOW_WHITE = new Color(0.847f, 1f, 0.898f, 1f); // #D8FFE5
        private static readonly Color PH_GLOW_BLUE = new Color(0.2f, 0.722f, 1f, 1f); // #33B8FF
        private static readonly Color PH_NEUTRAL_ZONE = new Color(0.847f, 1f, 0.898f, 0.35f); // #D8FFE5 35%
        
        // Game Systems
        private GameManager _gameManager;
        private ActionSystem _actionSystem;
        private EconomySystem _economySystem;
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;
        private DayCycleController _dayCycleController;
        
        // Animation coroutines
        private Coroutine _condensationIdleCoroutine;
        private Coroutine _phPulseCoroutine;
        
        // Condensation threshold tracking
        private float _previousCondensation = -1f; // -1 indica valore iniziale non ancora impostato
        
        // Colors
        private readonly Color _greenStable = new Color(0.498f, 1f, 0.478f, 1f); // #7FFF7A
        private readonly Color _yellowWarning = new Color(0.902f, 0.788f, 0.435f, 1f); // #E6C96F
        private readonly Color _redCritical = new Color(0.827f, 0.373f, 0.373f, 1f); // #D35F5F
        private readonly Color _blueInfo = new Color(0.365f, 0.714f, 0.890f, 1f); // #5DB6E3
        private Color _actionsBaseColor = new Color(0.902f, 0.788f, 0.435f, 1f); // default giallo
        private const float ActionsPulseSeed = 17.73f;
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder per HUD base (sotto PlantCard, sopra background)
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 50;
        }
        
        private void Start()
        {
            InitializeUI();
            InitializeComponents();
            InitializeGameSystems();
            UpdateAllMetrics();
            StartIdleAnimations();
        }
        
        private void InitializeGameSystems()
        {
            // Cerca GameManager nella scena
            _gameManager = FindObjectOfType<GameManager>();
            
            if (_gameManager != null)
            {
                // Collega ActionSystem
                if (_gameManager.ActionSystem != null)
                {
                    _actionSystem = _gameManager.ActionSystem;
                    
                    // Sottoscrivi agli eventi
                    _actionSystem.OnActionsChanged += OnActionsChanged;
                    
                    // Aggiorna valori iniziali
                    _actionsLeft = _actionSystem.ActionsLeft;
                    _maxActions = _actionSystem.MaxActions;
                    UpdateActions(_actionsLeft, _maxActions);
                    
                    if (_enableDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: ActionSystem collegato - Actions: {_actionsLeft}/{_maxActions}");
                    }
                }
                
                // Collega EconomySystem
                if (_gameManager.EconomySystem != null)
                {
                    _economySystem = _gameManager.EconomySystem;
                    
                    // Sottoscrivi agli eventi
                    _economySystem.OnCRYChanged += OnCRYChanged;
                    
                    // Aggiorna valore iniziale
                    _cryBalance = _economySystem.CurrentCRY;
                    UpdateCryBalance(_cryBalance);
                    
                    if (_enableDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: EconomySystem collegato - CRY: {_cryBalance}");
                    }
                }
                
                // FASE 10: Collega CondensationSystem
                if (_gameManager.CondensationSystem != null)
                {
                    // Sottoscrivi agli eventi
                    _gameManager.OnCondensationChanged += OnCondensationChanged;
                    
                    // Aggiorna valore iniziale (imposta anche previous per evitare toast al primo caricamento)
                    _condensation = _gameManager.CondensationSystem.CurrentAccumulation;
                    _previousCondensation = _condensation; // Imposta come previous per evitare toast iniziale
                    UpdateCondensation(_condensation);
                    
                    if (_enableDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: CondensationSystem collegato - Condensation: {_condensation}%");
                    }
                }
                
                // Collega PhSystem
                TryConnectPhSystem();
                
                // Day cycle per tooltip pH (giorno corrente e drift previsto)
                _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
                _dayCycleController = FindObjectOfType<DayCycleController>();
                
                // Sottoscrivi all'evento OnServiceRegistered per collegarsi quando PhSystem viene registrato
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: GameManager non trovato. Usando valori mock.");
            }
        }
        
        private void TryConnectPhSystem()
        {
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            
            if (_phSystem != null)
            {
                // Sottoscrivi agli eventi
                _phSystem.OnPhChanged += OnPhChanged;
                
                // Aggiorna valore iniziale
                _phLevel = _phSystem.CurrentPh;
                UpdatePh(_phLevel);
                
                
                if (_enableDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.UI, $"TopBarController: PhSystem collegato - pH: {_phLevel}");
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: PhSystem non trovato nel ServiceContainer. Riproverà quando verrà registrato.");
            }
        }
        
        private void OnServiceRegistered(object service)
        {
            
            // Se PhSystem viene registrato e non siamo ancora collegati, connettiamoci
            if (service is PhSystem && _phSystem == null)
            {
                TryConnectPhSystem();
            }
        }
        
        private void OnActionsChanged(int actionsLeft)
        {
            if (_actionSystem != null)
            {
                _actionsLeft = actionsLeft;
                _maxActions = _actionSystem.MaxActions;
                UpdateActions(_actionsLeft, _maxActions);
            }
        }
        
        private void OnCRYChanged(int cryAmount)
        {
            if (_economySystem != null)
            {
                _cryBalance = cryAmount;
                UpdateCryBalance(_cryBalance);
            }
        }
        
        private void OnPhChanged(float newPh, float delta)
        {
            
            if (_phSystem != null)
            {
                _phLevel = newPh;
                UpdatePh(_phLevel);
            }
        }
        
        /// <summary>
        /// FASE 10: Handler per cambio condensazione (riceve percentuale 0-100%).
        /// </summary>
        private void OnCondensationChanged(float percentage)
        {
            float previousValue = _previousCondensation;
            _condensation = percentage;
            
            // Rileva attraversamento soglie (solo se non è il primo valore)
            if (previousValue >= 0f)
            {
                CheckCondensationThresholds(previousValue, percentage);
            }
            
            _previousCondensation = percentage;
            UpdateCondensation(_condensation);
            
            // FASE 8: Aggiorna tooltip se visibile
            if (_condensationTooltip != null && _condensationTooltip.style.display == DisplayStyle.Flex)
            {
                UpdateCondensationTooltipContent();
            }
        }
        
        /// <summary>
        /// Verifica se la condensazione ha attraversato le soglie critiche (50%, 80%, 100%)
        /// ed emette toast notifications appropriate.
        /// </summary>
        private void CheckCondensationThresholds(float previousValue, float currentValue)
        {
            // Soglia 50%: Tank mezzo pieno (warning giallo)
            if (previousValue < 50f && currentValue >= 50f)
            {
                EmitCondensationToast("COND-005");
            }
            
            // Soglia 80%: Umidità impatta muffe (warning)
            if (previousValue < 80f && currentValue >= 80f)
            {
                EmitCondensationToast("COND-006");
            }
            
            // Soglia 100%: Tank pieno, umidità alta (danger)
            if (previousValue < 100f && currentValue >= 100f)
            {
                EmitCondensationToast("COND-008");
            }
        }
        
        /// <summary>
        /// Emette una toast notification per la condensazione.
        /// </summary>
        private void EmitCondensationToast(string toastCode)
        {
            var foundation = Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.PostToast(toastCode, new Sporae.UI.UIToolkit.NotificationsFoundation.NotificationPayload());
            }
            else
            {
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    // Fallback con messaggi hardcoded se foundation non disponibile
                    string message = toastCode switch
                    {
                        "COND-005" => "⚠️ Tank mezzo pieno",
                        "COND-006" => "⚠️ Umidità impatta muffe",
                        "COND-008" => "🚨 Tank pieno, umidità alta",
                        _ => $"Condensation threshold: {toastCode}"
                    };
                    toastManager.ShowToast(ToastNotificationType.Warning, message, toastCode);
                }
            }
        }
        
        private void SetupPhTooltip()
        {
            if (_phDisplay == null)
                return;

            _phTooltip = _root.Q<VisualElement>("ph-tooltip");
            _phTooltipTitle = _phTooltip?.Q<Label>("ph-tooltip-title");
            _phTooltipTitleIcon = _phTooltip?.Q<VisualElement>("ph-tooltip-title-icon");
            _phTooltipTitleStatus = _phTooltip?.Q<Label>("ph-tooltip-title-status");
            _phTooltipValueCurrent = _phTooltip?.Q<Label>("ph-tooltip-value-current");
            _phTooltipModifiersList = _phTooltip?.Q<VisualElement>("ph-tooltip-modifiers-list");
            _phTooltipValueTotal = _phTooltip?.Q<Label>("ph-tooltip-value-total");
            _phTooltipValueEffects = _phTooltip?.Q<Label>("ph-tooltip-value-effects");
            _phTooltipValueTip = _phTooltip?.Q<Label>("ph-tooltip-value-tip");
            _phTooltipCrtBars = new[]
            {
                _phTooltip?.Q<VisualElement>("ph-tooltip-crt-refresh"),
                _phTooltip?.Q<VisualElement>("ph-tooltip-crt-refresh-2"),
                _phTooltip?.Q<VisualElement>("ph-tooltip-crt-refresh-3")
            };
            _phCrtBarTop = new float[3];
            _phCrtBarSpeed = new float[3];
            _phCrtNextRandomizeTime = 0f;
            _phCrtVisibleCount = 1;
            if (_phTooltip != null)
                _phTooltip.pickingMode = PickingMode.Ignore;

            ApplyPhTooltipTitleFont();

            if (_phTooltip == null)
                return;

            _phDisplay.RegisterCallback<MouseEnterEvent>(OnPhHoverEnter);
            _phDisplay.RegisterCallback<MouseLeaveEvent>(OnPhHoverExit);
            _phDisplay.RegisterCallback<MouseMoveEvent>(OnPhHoverMove);
        }

        private void ApplyPhTooltipTitleFont()
        {
            if (_phTooltipTitle == null) return;
            // Keep global theme font unless a specific override is explicitly assigned in Inspector.
            Font font = _phTooltipTitleFont;
            if (font != null)
            {
                _phTooltipTitle.style.unityFont = new StyleFont(font);
                _phTooltipTitle.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
        }

        private void OnPhHoverEnter(MouseEnterEvent evt)
        {
            if (_phSystem != null && _phTooltip != null)
            {
                ApplyPhTooltipTitleFont();
                UpdatePhTooltipContent();
                _phTooltip.style.display = DisplayStyle.Flex;
                _phTooltip.BringToFront();
            }
        }
        
        private void OnPhHoverExit(MouseLeaveEvent evt)
        {
            if (_phTooltip != null)
            {
                _phTooltip.style.display = DisplayStyle.None;
            }
        }
        
        private void OnPhHoverMove(MouseMoveEvent evt)
        {
            if (_phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex && _phDisplay != null)
            {
                // Posiziona tooltip vicino al ph-display (sotto e a destra)
                var phDisplayBounds = _phDisplay.worldBound;
                var rootBounds = _root.worldBound;
                
                // Calcola posizione relativa al root
                float tooltipX = phDisplayBounds.xMax + 10f;
                float tooltipY = phDisplayBounds.yMax - 20f;
                
                // Assicurati che il tooltip non esca dallo schermo
                float tooltipWidth = 480f;
                float tooltipHeight = _phTooltip.resolvedStyle.height;
                
                if (tooltipX + tooltipWidth > rootBounds.width)
                {
                    tooltipX = phDisplayBounds.xMin - tooltipWidth - 10f;
                }
                
                if (tooltipY + tooltipHeight > rootBounds.height)
                {
                    tooltipY = phDisplayBounds.yMin - tooltipHeight - 10f;
                }
                
                _phTooltip.style.left = tooltipX;
                _phTooltip.style.top = tooltipY;
            }
        }
        
        private void UpdatePhTooltipContent()
        {
            if (_phSystem == null || _phTooltipValueCurrent == null)
                return;

            var culture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
            string bandName = _phSystem.GetBandName();
            float currentPh = _phSystem.CurrentPh;
            int currentDay = _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 1;

            // CURRENT VALUE: testo + colore dalla banda pH (acido→neutro→basico)
            _phTooltipValueCurrent.text = $"pH {currentPh.ToString("F1", culture)} — {bandName}";
            _phTooltipValueCurrent.style.color = new StyleColor(GetPhColorFromDrift(currentPh));

            // ACTIVE MODIFIERS: righe con [icon box] [nome quasi bianco] [valore colorato]
            // Total daily drift = somma di tutti i valori in Active Modifiers (piante + azioni + eventi)
            var plantMods = _phSystem.GetDailyPlantModifiersForDay(currentDay);
            var actionMods = _phSystem.GetDailyActionModifiers();
            var eventMods = _phSystem.GetDailyEventModifiers();
            float totalFromModifiers = 0f;
            if (plantMods != null)
                foreach (var p in plantMods)
                    totalFromModifiers += p.DailyDrift;
            if (actionMods != null)
                foreach (var a in actionMods)
                    totalFromModifiers += a.Delta;
            if (eventMods != null)
                foreach (var e in eventMods)
                    totalFromModifiers += e.Delta;

            bool hasPlantsInPots = plantMods != null && plantMods.Count > 0;
            bool hasAnyModifiers = hasPlantsInPots || (actionMods != null && actionMods.Count > 0) || (eventMods != null && eventMods.Count > 0);
            if (_phTooltipTitleIcon != null)
            {
                Color iconColor = hasAnyModifiers ? new Color(0.498f, 1f, 0.478f, 1f) : new Color(0.784f, 0.235f, 0.235f, 1f);
                _phTooltipTitleIcon.style.backgroundColor = new StyleColor(iconColor);
            }
            if (_phTooltipTitleStatus != null)
            {
                _phTooltipTitleStatus.text = hasAnyModifiers ? "Online" : "Offline";
                _phTooltipTitleStatus.style.color = new StyleColor(hasAnyModifiers ? new Color(0.498f, 1f, 0.478f, 1f) : new Color(0.784f, 0.235f, 0.235f, 1f));
            }

            if (_phTooltipModifiersList != null)
            {
                _phTooltipModifiersList.Clear();
                if (!hasAnyModifiers)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                    var iconBox = new VisualElement { name = "ph-modifier-icon", style = { width = 20, height = 20, minWidth = 20, minHeight = 20, marginRight = 8, backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.2f), borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1, borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.5f), borderRightColor = new Color(0.5f, 0.5f, 0.5f, 0.5f), borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.5f), borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) } };
                    var nameLabel = new Label { text = "Nessun modificatore attivo", enableRichText = true, style = { color = new Color(0.52f, 0.52f, 0.52f), fontSize = 10 } };
                    row.Add(iconBox);
                    row.Add(nameLabel);
                    _phTooltipModifiersList.Add(row);
                }
                else
                {
                    if (plantMods != null)
                    {
                        foreach (var p in plantMods)
                        {
                            string plantName = GetPlantDisplayName(p.PlantCode);
                            string driftStr = p.DailyDrift.ToString("+#0.0;-#0.0;0", culture);
                            Color valueColor = p.DailyDrift >= 0 ? new Color(1f, 0.27f, 0.27f) : new Color(0.365f, 0.714f, 0.89f);
                            var icon = GlobalIconResolver.GetPlantIcon(p.PlantCode);
                            AddPhModifierRow(_phTooltipModifiersList, plantName, driftStr, valueColor, icon);
                        }
                    }
                    if (actionMods != null)
                    {
                        foreach (var a in actionMods)
                        {
                            string driftStr = a.Delta.ToString("+#0.0;-#0.0;0", culture);
                            Color valueColor = a.Delta >= 0 ? new Color(1f, 0.27f, 0.27f) : new Color(0.365f, 0.714f, 0.89f);
                            var icon = GlobalIconResolver.GetActionIcon(a.ActionDisplayName);
                            AddPhModifierRow(_phTooltipModifiersList, a.ActionDisplayName, driftStr, valueColor, icon);
                        }
                    }
                    if (eventMods != null)
                    {
                        foreach (var e in eventMods)
                        {
                            string driftStr = e.Delta.ToString("+#0.0;-#0.0;0", culture);
                            Color valueColor = e.Delta >= 0 ? new Color(1f, 0.27f, 0.27f) : new Color(0.365f, 0.714f, 0.89f);
                            var icon = GlobalIconResolver.GetActionIcon(e.ActionDisplayName);
                            AddPhModifierRow(_phTooltipModifiersList, e.ActionDisplayName, driftStr, valueColor, icon);
                        }
                    }
                }
            }

            // Total daily drift = somma degli elementi in Active Modifiers (coerente con la lista)
            float totalDaily = totalFromModifiers;
            string totalStr = totalDaily.ToString("+#0.0;-#0.0;0", culture);
            string stableStr = Mathf.Abs(totalDaily) < 0.2f ? " (Stable)" : "";
            if (_phTooltipValueTotal != null)
                _phTooltipValueTotal.text = $"<color=#7FFF7A>{totalStr}{stableStr}</color>";

            // #region agent log
            float systemTotalDrift = _phSystem.GetTotalDailyDrift();
            int plantCount = plantMods != null ? plantMods.Count : 0;
            int actionCount = actionMods != null ? actionMods.Count : 0;
            PhRunDebugLogger.Log("H4", "TopBarController.cs:UpdatePhTooltipContent", "Tooltip pH values", "{\"currentPh\":" + currentPh.ToString("R") + ",\"currentDay\":" + currentDay + ",\"totalFromModifiers\":" + totalFromModifiers.ToString("R") + ",\"getTotalDailyDrift\":" + systemTotalDrift.ToString("R") + ",\"plantModsCount\":" + plantCount + ",\"actionModsCount\":" + actionCount + "}");
            // #endregion

            // POTENTIAL EFFECTS (solo valore; titolo è fisso in UXML)
            PhSystem.PhBand phBand = _phSystem.EvaluateState();
            if (_phTooltipValueEffects != null)
            {
                if (phBand == PhSystem.PhBand.Neutral)
                    _phTooltipValueEffects.text = "  <color=#7FFF7A>✔ Optimal range: Normal growth conditions</color>\n  <color=#888888>— Placeholder: rimuovere quando esistono bonus/malus pH in game —</color>";
                else
                    _phTooltipValueEffects.text = $"  <color=#E6C96F>Banda attuale: {bandName}</color>\n  <color=#888888>— Placeholder: rimuovere quando esistono bonus/malus pH in game —</color>";
            }

            // TIP (solo valore; titolo è fisso in UXML). Se nessuna pianta: invito a piantare dal Terminale POT.
            if (_phTooltipValueTip != null)
            {
                string tipBase = hasPlantsInPots ? "  " + GetPhTooltipTipForDay(currentDay) : "  Piantare piante dal Terminale POT per attivare il sistema di monitoraggio del pH.";
                if (hasAnyModifiers)
                    tipBase += "\n  Il valore \"CURRENT VALUE\" è il pH attuale della Dome; il drift indicato viene applicato al cambio giornata.";
                _phTooltipValueTip.text = tipBase;
            }
        }

        /// <summary>Restituisce un consiglio generico rotante per il tooltip pH (basato sul giorno).</summary>
        private static string GetPhTooltipTipForDay(int day)
        {
            string[] tips = new[]
            {
                "Aggiungi piante Pure o LED Blu per stabilizzare il baseline del pH.",
                "Il pH della Dome influenza crescita e resa: monitora la banda (Acido/Neutro/Basico).",
                "Overwatering e LED Rosso spostano il pH; usa le azioni con consapevolezza.",
                "Condensazione e fertilizzanti contribuiscono al drift giornaliero.",
                "Ogni pianta nei Pot ha un impatto giornaliero sul pH: meno vasi = drift più prevedibile.",
                "Il forecast del giorno dopo (End of Day) include tutti gli effetti: piante, LED, azioni.",
                "Usa il Laboratorio e l'Extractor per spore e semi; il pH non influenza l'estrazione.",
            };
            int index = (day - 1) % tips.Length;
            return tips[index];
        }

        private static string GetPlantDisplayName(string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode) || plantCode == "Unknown")
                return "Pianta";
            var plantData = PlantDatabase.Instance?.GetPlantDataByCode(plantCode);
            return plantData != null ? (plantData.name ?? plantCode) : plantCode;
        }
        
        /// <summary>
        /// Aggiunge una riga alla lista Active Modifiers: [icon box] [nome quasi bianco] [valore colorato].
        /// </summary>
        private static void AddPhModifierRow(VisualElement list, string nameText, string valueText, Color valueColor, Sprite icon = null)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var iconBox = new VisualElement();
            iconBox.AddToClassList("ph-tooltip-modifier-icon");
            if (icon != null)
            {
                iconBox.style.backgroundImage = new StyleBackground(icon);
                iconBox.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
            var nameLabel = new Label { text = nameText, enableRichText = false };
            nameLabel.style.color = new Color(0.94f, 0.95f, 0.96f); // quasi bianco come da reference
            nameLabel.style.fontSize = 11;
            nameLabel.style.marginRight = 8;
            nameLabel.style.flexGrow = 1;
            var valueLabel = new Label { text = valueText, enableRichText = false };
            valueLabel.style.color = valueColor;
            valueLabel.style.fontSize = 11;
            row.Add(iconBox);
            row.Add(nameLabel);
            row.Add(valueLabel);
            list.Add(row);
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UIDocument non trovato su TopBarController!");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "Root VisualElement non trovato!");
                return;
            }

            _glowFrame = _root.Q<VisualElement>("glow-frame");
            SetupGlowFrame();
            
            // Query UI elements
            _iconActions = _root.Q<VisualElement>("icon-actions");
            _iconActionsGlyph = _root.Q<VisualElement>("icon-actions-glyph");
            _actionsLabel = _root.Q<Label>("actions-label");
            var actionsBarContainer = _root.Q<VisualElement>("actions-bar");
            _actionsValueLabel = _root.Q<Label>("actions-value");
            _phDisplay = _root.Q<VisualElement>("ph-display");
            _phDriftLabel = _root.Q<Label>("ph-drift-label");
            _phBandLabel = _root.Q<Label>("ph-band");
            _phSlider = _root.Q<VisualElement>("ph-slider");
            _phMarker = _root.Q<VisualElement>("ph-marker");
            _phGradient = _root.Q<VisualElement>("ph-gradient");
            _phNeutralZone = _root.Q<VisualElement>("ph-neutral-zone");
            
            _condensationValueLabel = _root.Q<Label>("condensation-value");
            _condensationDisplay = _root.Q<VisualElement>("condensation-display"); // FASE 10: Query per tooltip futuro
            _mutationValueLabel = _root.Q<Label>("mutation-value");
            _cryValueLabel = _root.Q<Label>("cry-value");
            _grateValueLabel = _root.Q<Label>("grate-value");
            
            // Setup tooltip per pH
            SetupPhTooltip();
            
            // FASE 8: Setup tooltip per condensazione
            SetupCondensationTooltip();
            
            // Crea texture gradiente pH (0-14 scale)
            CreatePhGradientTexture();
            
            // Registra callback per quando il layout è calcolato (per aggiornare posizione marker)
            if (_phSlider != null)
            {
                _phSlider.RegisterCallback<GeometryChangedEvent>(OnPhSliderGeometryChanged);
            }
            
            // Initialize Actions Bar
            if (actionsBarContainer != null)
            {
                _actionsBar = new SegmentedBarUI(
                    actionsBarContainer,
                    4,
                    _yellowWarning,
                    new Color(0.118f, 0.157f, 0.165f, 1f),
                    _yellowWarning
                );
            }
        }

        private void SetupGlowFrame()
        {
            if (_glowFrame == null) return;

            if (_glowFrameMaterial == null)
            {
                var shader = Shader.Find(GlowShaderName);
                if (shader != null)
                    _glowFrameMaterial = new Material(shader);
            }

            if (_glowFrameMaterial != null)
            {
                ApplyGlowDefaults(_glowFrameMaterial);
                _glowFrameMaterialRuntime = new Material(_glowFrameMaterial);
                _glowFrameGenerator = new UiGlowFrameGenerator(_glowFrame, _glowFrameMaterialRuntime);
            }

            _glowFrameLiveUpdate = true;

        }

        private static void ApplyGlowDefaults(Material mat)
        {
            // Glow frame should be transparent; bar background is handled by USS.
            var bg = new Color(0f, 0f, 0f, 0f);
            // Border: #7FFF7A @ 60%
            var border = new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.60f);
            // Glow: same green, stronger alpha for bloom
            var glow = new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.90f);

            mat.SetColor("_GradTop", bg);
            mat.SetColor("_GradBottom", bg);
            mat.SetFloat("_GradStrength", 0.0f);
            mat.SetColor("_BorderColor", border);
            mat.SetColor("_GlowColor", glow);
            mat.SetFloat("_BorderThickness", 1.5f); // Ridotto per border più sottile, mantenendo glow
            mat.SetFloat("_BorderSoftness", 0.5f); // Più netto per border più visibile come sottile
            mat.SetFloat("_GlowSize", 14.0f);
            mat.SetFloat("_GlowIntensity", 1.0f);
            mat.SetFloat("_GlowFalloff", 1.25f);
        }

        private void Update()
        {
            if (_glowFrameLiveUpdate && _glowFrameGenerator != null)
            {
                if (_glowFrameMaterialRuntime != null && _glowFrameMaterial != null)
                    _glowFrameMaterialRuntime.CopyPropertiesFromMaterial(_glowFrameMaterial);
                if (_glowFrameMaterialRuntime != null)
                {
                    _glowFrameMaterialRuntime.SetFloat("_EdgeMode", 2.0f); // bottom edge only
                    _glowFrameMaterialRuntime.SetFloat("_BorderThickness", 1.5f); // Border più sottile
                    _glowFrameMaterialRuntime.SetFloat("_BorderSoftness", 0.5f); // Più netto
                }
                _glowFrameGenerator.Render();
            }
            
            // Pulsazione leggera randomica su icona e label ACTIONS (Perlin: da scurito ad acceso)
            if (_iconActionsGlyph != null && _actionsLabel != null)
            {
                float t = Mathf.PerlinNoise(Time.time * 0.38f, ActionsPulseSeed);
                // Curva che tiene il valore alto più a lungo (più tempo “acceso” che “scurito”)
                float tRemap = 1f - Mathf.Pow(1f - t, 1.7f);
                float pulse = Mathf.Lerp(0.38f, 1.22f, tRemap);
                float r = Mathf.Clamp01(_actionsBaseColor.r * pulse);
                float g = Mathf.Clamp01(_actionsBaseColor.g * pulse);
                float b = Mathf.Clamp01(_actionsBaseColor.b * pulse);
                Color pulsed = new Color(r, g, b, _actionsBaseColor.a);
                // Al picco massimo blend verso bianco per effetto più intenso
                float peakGlow = Mathf.Clamp01((tRemap - 0.75f) / 0.25f);
                pulsed = Color.Lerp(pulsed, Color.white, peakGlow * 0.22f);
                _iconActionsGlyph.style.unityBackgroundImageTintColor = new StyleColor(pulsed);
                _actionsLabel.style.color = new StyleColor(pulsed);
            }
            
            // Overlay barre refresh CRT sul ph-tooltip (1, 2 o 3 barre, velocità randomica)
            if (_phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex && _phTooltipCrtBars != null)
            {
                const float barHeightPx = 10f;
                float tooltipH = _phTooltip.resolvedStyle.height;
                if (tooltipH < 10f) tooltipH = 200f;
                float totalRange = tooltipH + barHeightPx * 2f;

                if (Time.time >= _phCrtNextRandomizeTime)
                {
                    _phCrtNextRandomizeTime = Time.time + 2f + UnityEngine.Random.Range(0f, 2.5f);
                    _phCrtVisibleCount = UnityEngine.Random.Range(1, 4);
                    for (int i = 0; i < 3; i++)
                    {
                        _phCrtBarSpeed[i] = UnityEngine.Random.Range(140f, 380f);
                        if (i >= _phCrtVisibleCount)
                            _phCrtBarTop[i] = -barHeightPx - 10f;
                        else if (_phCrtBarTop[i] < -barHeightPx || _phCrtBarTop[i] > tooltipH + 5f)
                            _phCrtBarTop[i] = UnityEngine.Random.Range(-barHeightPx, tooltipH * 0.3f);
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    var bar = _phTooltipCrtBars[i];
                    if (bar == null) continue;
                    bool visible = i < _phCrtVisibleCount;
                    bar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    if (!visible) continue;
                    _phCrtBarTop[i] += _phCrtBarSpeed[i] * Time.deltaTime;
                    if (_phCrtBarTop[i] > tooltipH + barHeightPx)
                        _phCrtBarTop[i] = -barHeightPx;
                    bar.style.top = _phCrtBarTop[i];
                }
            }

            // Pulsazione icona titolo ph-tooltip (rosso/verde)
            if (_phTooltipTitleIcon != null && _phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex)
            {
                float pulse = 0.92f + 0.14f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2.8f));
                _phTooltipTitleIcon.style.scale = new Scale(new Vector2(pulse, pulse));
            }
        }

        private void OnDisable()
        {
            _glowFrameGenerator?.Dispose();
            _glowFrameGenerator = null;
        }
        
        private void InitializeComponents()
        {
            // Mutation Orbit è gestito come componente separato opzionale
            // Se presente, verrà aggiornato via UpdateMutation()
            // Se non presente, solo il label verrà aggiornato
        }
        
        private void UpdateAllMetrics()
        {
            UpdateActions(_actionsLeft, _maxActions);
            UpdatePh(_phLevel);
            UpdateCondensation(_condensation);
            UpdateMutation(_mutationIndex);
            UpdateCryBalance(_cryBalance);
            UpdateGrate(_grateValue);
        }
        
        /// <summary>
        /// Aggiorna la barra ACTIONS con colori dinamici basati su threshold.
        /// </summary>
        public void UpdateActions(int current, int max)
        {
            _actionsLeft = current;
            _maxActions = max;
            
            if (_actionsValueLabel != null)
            {
                _actionsValueLabel.text = $"{current}/{max}";
            }
            
            // Determina colore basato su azioni disponibili: verde 4+, giallo 3-2, rosso 0-1
            Color fillColor;
            if (current >= 4)
                fillColor = _greenStable;
            else if (current >= 2)
                fillColor = _yellowWarning;
            else
                fillColor = _redCritical;
            
            _actionsBaseColor = fillColor;
            
            if (_iconActionsGlyph != null)
                _iconActionsGlyph.style.unityBackgroundImageTintColor = new StyleColor(fillColor);
            if (_actionsLabel != null)
                _actionsLabel.style.color = new StyleColor(fillColor);
            
            if (_actionsBar != null)
            {
                _actionsBar.SetColors(fillColor, new Color(0.118f, 0.157f, 0.165f, 1f), fillColor);
                _actionsBar.UpdateValue(current, max);
            }
        }
        
        /// <summary>
        /// Crea texture gradiente per pH (0-14 scale)
        /// </summary>
        private void CreatePhGradientTexture()
        {
            int width = 256;
            int height = 8;
            
            _phGradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _phGradientTexture.wrapMode = TextureWrapMode.Clamp;
            _phGradientTexture.filterMode = FilterMode.Bilinear;
            
            // Crea gradiente: Rosso (0) → Arancione (4) → Bianco (7) → Azzurro (10) → Viola (14)
            for (int x = 0; x < width; x++)
            {
                float normalizedX = x / (float)(width - 1);
                float phValue = normalizedX * 14f; // 0-14 scale
                Color pixelColor = GetPhColorFromScale(phValue);
                
                for (int y = 0; y < height; y++)
                {
                    _phGradientTexture.SetPixel(x, y, pixelColor);
                }
            }
            
            _phGradientTexture.Apply();
            
            // Applica texture al ph-gradient VisualElement
            if (_phGradient != null)
            {
                _phGradient.style.backgroundImage = new StyleBackground(_phGradientTexture);
            }
        }
        
        /// <summary>
        /// Ottiene colore pH dalla scala 0-14 con interpolazione lineare
        /// </summary>
        private Color GetPhColorFromScale(float phValue)
        {
            phValue = Mathf.Clamp(phValue, 0f, 14f);
            
            if (phValue <= 4f)
            {
                // Rosso (0) → Arancione (4)
                float t = phValue / 4f;
                return Color.Lerp(PH_RED, PH_ORANGE, t);
            }
            else if (phValue <= 7f)
            {
                // Arancione (4) → Bianco (7)
                float t = (phValue - 4f) / 3f;
                return Color.Lerp(PH_ORANGE, PH_WHITE, t);
            }
            else if (phValue <= 10f)
            {
                // Bianco (7) → Azzurro (10)
                float t = (phValue - 7f) / 3f;
                return Color.Lerp(PH_WHITE, PH_BLUE, t);
            }
            else
            {
                // Azzurro (10) → Viola (14)
                float t = (phValue - 10f) / 4f;
                return Color.Lerp(PH_BLUE, PH_PURPLE, t);
            }
        }
        
        /// <summary>
        /// Mappa drift pH (-100..+100) al colore della banda (rosso → bianco → blu) per tooltip.
        /// </summary>
        private Color GetPhColorFromDrift(float driftPh)
        {
            float phVisualScale = ((driftPh + 100f) / 200f) * 14f;
            phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
            return GetPhColorFromScale(phVisualScale);
        }
        
        /// <summary>
        /// Callback quando il layout del ph-slider viene calcolato
        /// </summary>
        private void OnPhSliderGeometryChanged(GeometryChangedEvent evt)
        {
            // Aggiorna la posizione del marker quando il layout è pronto
            if (_phLevel != 0f || _phSystem != null)
            {
                UpdatePh(_phLevel);
            }
        }
        
        /// <summary>
        /// Aggiorna il pH slider con marker posizionato dinamicamente.
        /// </summary>
        public void UpdatePh(float value)
        {
            _phLevel = value;
            
            // Mostra direttamente il valore pH nel range -100/+100 (come il vecchio sistema)
            // NON convertire in scala 0-14 - il vecchio sistema mostrava currentPh direttamente
            float phDisplayValue = value;
            
            // Colore base per "PH DRIFT" label
            Color phDriftColor = new Color(0.392f, 0.565f, 0.933f, 0.85f); // rgba(83, 144, 255, 0.85)
            
            // Aggiorna label "PH DRIFT" (solo label, senza valore)
            if (_phDriftLabel != null)
            {
                _phDriftLabel.text = "PH DRIFT";
                _phDriftLabel.style.color = new StyleColor(phDriftColor);
            }
            
            // Aggiorna valore numerico pH (mostra drift -100/+100; default 0 con oscillazione estetica)
            if (_phBandLabel != null)
            {
                // Mostra valore drift diretto (-100/+100), inizio partita = 0
                _phBandLabel.text = $"{value:F1}";
                // Per marker/gradient usiamo scala 0-14 (mapping: -100→0, 0→7, +100→14)
                float phVisualScale = ((value + 100f) / 200f) * 14f;
                phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
                
                // Colore basato sulla banda se PhSystem disponibile
                if (_phSystem != null)
                {
                    Color bandColor = _phSystem.GetBandColor();
                    _phBandLabel.style.color = new StyleColor(bandColor);
                }
                else
                {
                    // Fallback: usa colore default se PhSystem non disponibile
                    _phBandLabel.style.color = new StyleColor(phDriftColor);
                }
            }
            
            if (_phMarker != null && _phGradient != null)
            {
                // Converti da range PhSystem (-100/+100) a scala visualizzazione (0-14) SOLO per il marker
                // Mapping: -100 → 0, 0 → 7, +100 → 14
                float phVisualScale = ((value + 100f) / 200f) * 14f;
                phVisualScale = Mathf.Clamp(phVisualScale, 0f, 14f);
                
                // Posiziona marker: (phVisualScale / 14) * 100%
                // Il marker è largo 12px, quindi dobbiamo centrarlo: left = (normalizedPos * 100%) - (6px / sliderWidth)
                float normalizedPos = phVisualScale / 14f;
                
                // Usa left in percentuale per posizionare il marker
                // Il marker è largo 12px, quindi per centrarlo dobbiamo sottrarre metà della larghezza
                // In UI Toolkit, left in percentuale si riferisce al bordo sinistro del marker
                // Per centrare: left = (normalizedPos * 100%) - (markerWidth/2 / sliderWidth * 100%)
                
                // Prova a ottenere la larghezza del slider
                float sliderWidth = _phSlider != null ? _phSlider.resolvedStyle.width : float.NaN;
                
                float markerWidth = 12f;
                float leftPositionPercent = normalizedPos * 100f;
                
                // Se la larghezza del slider è disponibile, calcola l'offset per centrare il marker
                if (!float.IsNaN(sliderWidth) && sliderWidth > 0f)
                {
                    // Calcola offset in percentuale: (markerWidth/2 / sliderWidth) * 100
                    float offsetPercent = (markerWidth / 2f / sliderWidth) * 100f;
                    leftPositionPercent -= offsetPercent;
                }
                
                // Usa left in percentuale
                _phMarker.style.left = new StyleLength(new Length(leftPositionPercent, LengthUnit.Percent));
                
                // Reset margin-left per evitare conflitti
                _phMarker.style.marginLeft = 0f;
                
                // Colore marker interpolato dal gradiente
                Color markerColor = GetPhColorFromScale(phVisualScale);
                _phMarker.style.backgroundColor = new StyleColor(markerColor);
                
                // Glow contestuale sul track (ph-slider)
                UpdatePhGlow(phVisualScale);
                
                // Zona ottimale overlay (6-8)
                UpdatePhNeutralZone(phVisualScale);
                
                // Pulse animation se drift giornaliero > 1.0
                float totalDrift = CalculateTotalDailyDrift();
                if (Mathf.Abs(totalDrift) > 1.0f)
                {
                    if (_phPulseCoroutine == null)
                    {
                        _phPulseCoroutine = StartCoroutine(PhPulseAnimation());
                    }
                }
                else
                {
                    if (_phPulseCoroutine != null)
                    {
                        StopCoroutine(_phPulseCoroutine);
                        _phPulseCoroutine = null;
                        // Reset size
                        if (_phMarker != null)
                        {
                            _phMarker.style.width = 12f;
                            _phMarker.style.height = 12f;
                            _phMarker.style.marginLeft = 0f;
                            _phMarker.style.marginTop = 0f;
                        }
                    }
                }
            }
            
            // Aggiorna tooltip se visibile
            if (_phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex)
            {
                UpdatePhTooltipContent();
            }
        }
        
        /// <summary>
        /// Calcola drift rate giornaliero totale
        /// </summary>
        private float CalculateTotalDailyDrift()
        {
            if (_phSystem == null)
                return 0f;
            
            var contrib = _phSystem.GetContributions();
            // Drift rate giornaliero = somma di tutti i drift accodati o già applicati
            // Per ora usiamo i contributi già applicati (plants, actions, events, daily)
            float totalDrift = contrib.PlantsDrift + contrib.ActionsDrift + contrib.EventsDrift + contrib.DailyDrift;
            return totalDrift;
        }
        
        /// <summary>
        /// Aggiorna glow contestuale sul track basato su zona pH
        /// </summary>
        private void UpdatePhGlow(float phValue)
        {
            if (_phSlider == null)
                return;
            
            Color glowColor;
            float glowIntensity;
            
            if (phValue < 5f)
            {
                // Rosso per pH < 5
                glowColor = PH_GLOW_RED;
            }
            else if (phValue >= 5f && phValue <= 9f)
            {
                // Bianco-ghiaccio per zona neutrale 5-9
                glowColor = PH_GLOW_WHITE;
            }
            else
            {
                // Blu per pH > 9
                glowColor = PH_GLOW_BLUE;
            }
            
            // Intensità proporzionale al drift rate: |totalDrift| / 3
            float totalDrift = Mathf.Abs(CalculateTotalDailyDrift());
            glowIntensity = Mathf.Clamp01(totalDrift / 3f);
            
            // Applica glow come border color con opacità
            Color glowWithIntensity = new Color(glowColor.r, glowColor.g, glowColor.b, glowIntensity);
            _phSlider.style.borderTopColor = glowWithIntensity;
            _phSlider.style.borderRightColor = glowWithIntensity;
            _phSlider.style.borderBottomColor = glowWithIntensity;
            _phSlider.style.borderLeftColor = glowWithIntensity;
        }
        
        /// <summary>
        /// Aggiorna overlay zona ottimale (6-8)
        /// </summary>
        private void UpdatePhNeutralZone(float phValue)
        {
            if (_phNeutralZone == null)
                return;
            
            // Mostra overlay solo quando pH è tra 6-8
            if (phValue >= 6f && phValue <= 8f)
            {
                _phNeutralZone.style.display = DisplayStyle.Flex;
            }
            else
            {
                _phNeutralZone.style.display = DisplayStyle.None;
            }
        }
        
        private Color GetPhColor(float phValue)
        {
            // Gradient: Red (0) -> Orange (4) -> White (7) -> Blue (10) -> Purple (14)
            if (phValue <= 4f)
            {
                float t = phValue / 4f;
                return Color.Lerp(new Color(1f, 0.165f, 0.165f, 1f), new Color(1f, 0.667f, 0.2f, 1f), t); // Red to Orange
            }
            else if (phValue <= 7f)
            {
                float t = (phValue - 4f) / 3f;
                return Color.Lerp(new Color(1f, 0.667f, 0.2f, 1f), new Color(0.961f, 0.969f, 0.980f, 1f), t); // Orange to White
            }
            else if (phValue <= 10f)
            {
                float t = (phValue - 7f) / 3f;
                return Color.Lerp(new Color(0.961f, 0.969f, 0.980f, 1f), new Color(0.2f, 0.722f, 1f, 1f), t); // White to Blue
            }
            else
            {
                float t = (phValue - 10f) / 4f;
                return Color.Lerp(new Color(0.2f, 0.722f, 1f, 1f), new Color(0.357f, 0.310f, 1f, 1f), t); // Blue to Purple
            }
        }
        
        private IEnumerator PhPulseAnimation()
        {
            float baseSize = 12f;
            
            while (_phMarker != null)
            {
                float elapsed = 0f;
                float duration = 1f;
                
                while (_phMarker != null && elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.PingPong(elapsed / duration, 1f);
                    float size = Mathf.Lerp(baseSize, baseSize * 1.05f, t);
                    _phMarker.style.width = size;
                    _phMarker.style.height = size;
                    // Aggiorna margin per mantenere centrato
                    float margin = -(size - baseSize) / 2f;
                    _phMarker.style.marginLeft = margin;
                    _phMarker.style.marginTop = margin;
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// Aggiorna il valore CONDENSATION con idle animation.
        /// </summary>
        public void UpdateCondensation(float value)
        {
            _condensation = value;
            
            if (_condensationValueLabel != null)
            {
                _condensationValueLabel.text = $"{Mathf.RoundToInt(value)}%";
            }
        }
        
        private void StartIdleAnimations()
        {
            _condensationIdleCoroutine = StartCoroutine(CondensationIdleAnimation());
        }
        
        /// <summary>
        /// FASE 9: Animazione fittizia continua per condensazione (variazione fluida ±0.5-1.5% come pH drift).
        /// </summary>
        private IEnumerator CondensationIdleAnimation()
        {
            while (true)
            {
                float baseValue = _condensation; // Valore reale
                float time = Time.time;
                // Oscilla ±1% con frequenza 0.5 (variazione fluida continua)
                float variation = Mathf.Sin(time * 0.5f) * 1.0f;
                float displayValue = Mathf.Clamp(baseValue + variation, 0f, 100f);
                
                if (_condensationValueLabel != null)
                {
                    _condensationValueLabel.text = $"{Mathf.RoundToInt(displayValue)}%";
                }
                
                yield return null; // Aggiorna ogni frame per movimento fluido
            }
        }
        
        /// <summary>
        /// Aggiorna l'indice MUTATION.
        /// </summary>
        public void UpdateMutation(float index)
        {
            _mutationIndex = Mathf.Clamp01(index);
            
            if (_mutationValueLabel != null)
            {
                // Mostra come percentuale (come nell'immagine di riferimento: 45%)
                int percentage = Mathf.RoundToInt(_mutationIndex * 100f);
                _mutationValueLabel.text = $"{percentage}%";
                
                // Colore dinamico
                Color color;
                if (_mutationIndex <= 0.33f)
                {
                    color = _greenStable; // Stable
                }
                else if (_mutationIndex <= 0.66f)
                {
                    color = _yellowWarning; // Warning
                }
                else
                {
                    color = _redCritical; // Critical
                }
                
                _mutationValueLabel.style.color = new StyleColor(color);
            }
            
            // Update MutationOrbitUI component
            var mutationOrbit = GetComponent<MutationOrbitUI>();
            if (mutationOrbit != null)
            {
                mutationOrbit.UpdateMutation(_mutationIndex);
            }
        }
        
        /// <summary>
        /// Aggiorna il CRY BALANCE con formattazione numerica.
        /// </summary>
        public void UpdateCryBalance(int value)
        {
            _cryBalance = value;
            
            if (_cryValueLabel != null)
            {
                _cryValueLabel.text = value.ToString("N0"); // Formato con virgole
            }
        }
        
        /// <summary>
        /// Aggiorna il valore GRATE.
        /// </summary>
        public void UpdateGrate(int value)
        {
            _grateValue = value;
            
            if (_grateValueLabel != null)
            {
                _grateValueLabel.text = $"+{value}";
            }
        }

        /// <summary>Usato da EndOfDay Dawn Summary per mostrare l'indice di mutazione corrente.</summary>
        public float GetMutationIndex() => _mutationIndex;

        /// <summary>Usato da EndOfDay Dawn Summary per mostrare il G-rate corrente.</summary>
        public int GetGrateValue() => _grateValue;
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_actionSystem != null)
            {
                _actionSystem.OnActionsChanged -= OnActionsChanged;
            }
            
            if (_economySystem != null)
            {
                _economySystem.OnCRYChanged -= OnCRYChanged;
            }
            
            // FASE 10: Unsubscribe da CondensationSystem
            if (_gameManager != null)
            {
                _gameManager.OnCondensationChanged -= OnCondensationChanged;
            }
            
            // Unsubscribe from ServiceContainer event
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
            
            if (_phSystem != null)
            {
                _phSystem.OnPhChanged -= OnPhChanged;
            }
            
            // Unregister hover events
            if (_phDisplay != null)
            {
                _phDisplay.UnregisterCallback<MouseEnterEvent>(OnPhHoverEnter);
                _phDisplay.UnregisterCallback<MouseLeaveEvent>(OnPhHoverExit);
                _phDisplay.UnregisterCallback<MouseMoveEvent>(OnPhHoverMove);
            }
            
            // FASE 8: Unregister condensation hover events
            if (_condensationDisplay != null)
            {
                _condensationDisplay.UnregisterCallback<MouseEnterEvent>(OnCondensationHoverEnter);
                _condensationDisplay.UnregisterCallback<MouseLeaveEvent>(OnCondensationHoverExit);
                _condensationDisplay.UnregisterCallback<MouseMoveEvent>(OnCondensationHoverMove);
            }
            
            // FASE 8: Unregister tooltip hover events
            if (_condensationTooltip != null)
            {
                _condensationTooltip.UnregisterCallback<MouseEnterEvent>(OnCondensationTooltipHoverEnter);
                _condensationTooltip.UnregisterCallback<MouseLeaveEvent>(OnCondensationTooltipHoverExit);
            }
            
            // FASE 8: Unregister collect button
            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.clicked -= OnCondensationCollectClicked;
            }
            
            // Unregister geometry changed callback
            if (_phSlider != null)
            {
                _phSlider.UnregisterCallback<GeometryChangedEvent>(OnPhSliderGeometryChanged);
            }
            
            // Cleanup texture
            if (_phGradientTexture != null)
            {
                Destroy(_phGradientTexture);
                _phGradientTexture = null;
            }
            
            if (_condensationIdleCoroutine != null)
                StopCoroutine(_condensationIdleCoroutine);
            
            if (_phPulseCoroutine != null)
                StopCoroutine(_phPulseCoroutine);
        }
        
        private void OnEnable()
        {
            // Re-subscribe when enabled
            if (_actionSystem != null)
            {
                _actionSystem.OnActionsChanged += OnActionsChanged;
            }
            
            if (_economySystem != null)
            {
                _economySystem.OnCRYChanged += OnCRYChanged;
            }
            
            // FASE 10: Re-subscribe a CondensationSystem
            if (_gameManager != null)
            {
                _gameManager.OnCondensationChanged += OnCondensationChanged;
            }
            
            // Re-subscribe to ServiceContainer event
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            }
            
            // Try to reconnect PhSystem
            if (_phSystem == null)
            {
                TryConnectPhSystem();
            }
            else
            {
                _phSystem.OnPhChanged += OnPhChanged;
            }
        }
        
        /// <summary>
        /// FASE 8: Calcola giorni virtuali da condensazione (helper per tooltip).
        /// </summary>
        private float GetVirtualDaysFromCondensation(float percentage)
        {
            if (percentage < 50f)
                return 0f;
            if (percentage < 60f)
                return 0.5f;
            if (percentage < 80f)
                return 1.0f;
            return 1.5f; // 80-100%
        }
        
        /// <summary>
        /// FASE 8: Setup tooltip per condensazione (simile a SetupPhTooltip).
        /// </summary>
        private void SetupCondensationTooltip()
        {
            if (_condensationDisplay == null)
                return;

            _condensationTooltip = _root.Q<VisualElement>("condensation-tooltip");
            _condensationTooltipText = _condensationTooltip?.Q<Label>("condensation-tooltip-text");
            _condensationCollectButton = _condensationTooltip?.Q<Button>("condensation-collect-button");
            if (_condensationTooltip != null)
                _condensationTooltip.pickingMode = PickingMode.Position;

            if (_condensationTooltip == null)
                return;

            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.clicked += OnCondensationCollectClicked;
                _condensationCollectButton.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    _condensationCollectButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                });
                _condensationCollectButton.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    _condensationCollectButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                });
            }

            _condensationDisplay.RegisterCallback<MouseEnterEvent>(OnCondensationHoverEnter);
            _condensationDisplay.RegisterCallback<MouseLeaveEvent>(OnCondensationHoverExit);
            _condensationDisplay.RegisterCallback<MouseMoveEvent>(OnCondensationHoverMove);
            _condensationTooltip.RegisterCallback<MouseEnterEvent>(OnCondensationTooltipHoverEnter);
            _condensationTooltip.RegisterCallback<MouseLeaveEvent>(OnCondensationTooltipHoverExit);
        }
        
        /// <summary>
        /// FASE 8: Handler hover enter per condensazione display.
        /// </summary>
        private void OnCondensationHoverEnter(MouseEnterEvent evt)
        {
            if (_gameManager != null && _condensationTooltip != null)
            {
                UpdateCondensationTooltipContent();
                _condensationTooltip.style.display = DisplayStyle.Flex;
                _condensationTooltip.BringToFront();
            }
        }
        
        /// <summary>
        /// FASE 8: Handler hover exit per condensazione display.
        /// Non chiude il tooltip se il mouse è ancora sopra il tooltip stesso.
        /// </summary>
        private void OnCondensationHoverExit(MouseLeaveEvent evt)
        {
            // Non chiudere immediatamente - il tooltip gestirà la chiusura quando il mouse esce anche da lì
            // Questo permette di spostare il mouse dal display al tooltip senza chiuderlo
        }
        
        /// <summary>
        /// FASE 8: Handler hover enter per tooltip condensazione.
        /// Mantiene il tooltip aperto quando il mouse entra nel tooltip.
        /// </summary>
        private void OnCondensationTooltipHoverEnter(MouseEnterEvent evt)
        {
            if (_condensationTooltip != null)
            {
                _condensationTooltip.style.display = DisplayStyle.Flex;
                _condensationTooltip.BringToFront();
            }
        }
        
        /// <summary>
        /// FASE 8: Handler hover exit per tooltip condensazione.
        /// Chiude il tooltip solo quando il mouse esce anche dal tooltip.
        /// </summary>
        private void OnCondensationTooltipHoverExit(MouseLeaveEvent evt)
        {
            if (_condensationTooltip != null)
            {
                _condensationTooltip.style.display = DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// FASE 8: Handler hover move per condensazione (posizionamento tooltip).
        /// </summary>
        private void OnCondensationHoverMove(MouseMoveEvent evt)
        {
            if (_condensationTooltip != null && _condensationTooltip.style.display == DisplayStyle.Flex && _condensationDisplay != null)
            {
                var displayBounds = _condensationDisplay.worldBound;
                var rootBounds = _root.worldBound;
                
                float tooltipX = displayBounds.xMax + 10f;
                float tooltipY = displayBounds.yMax - 20f;
                
                float tooltipWidth = 320f;
                float tooltipHeight = _condensationTooltip.resolvedStyle.height;
                
                if (tooltipX + tooltipWidth > rootBounds.width)
                {
                    tooltipX = displayBounds.xMin - tooltipWidth - 10f;
                }
                
                if (tooltipY + tooltipHeight > rootBounds.height)
                {
                    tooltipY = displayBounds.yMin - tooltipHeight - 10f;
                }
                
                _condensationTooltip.style.left = tooltipX;
                _condensationTooltip.style.top = tooltipY;
            }
        }
        
        /// <summary>
        /// FASE 8: Aggiorna contenuto tooltip condensazione con informazioni complete.
        /// </summary>
        private void UpdateCondensationTooltipContent()
        {
            if (_gameManager == null || _condensationTooltipText == null || _gameManager.CondensationSystem == null)
                return;
            
            var condensationSystem = _gameManager.CondensationSystem;
            float currentPercentage = condensationSystem.CurrentAccumulation;
            float dailyProduction = condensationSystem.DailyProduction;
            bool hasLed = false;
            
            // Verifica LED attivi (se DayCycleController disponibile)
            var dayCycleController = UnityEngine.Object.FindObjectOfType<DayCycleController>();
            if (dayCycleController != null)
            {
                // Usa reflection o metodo pubblico se disponibile (per ora semplificato
                hasLed = false; // TODO: Aggiungere metodo pubblico HasAnyActiveLed() in DayCycleController
            }
            
            // Calcola giorni virtuali
            float virtualDays = GetVirtualDaysFromCondensation(currentPercentage);
            
            // Calcola previsione produzione domani (stima basata su produzione odierna)
            float tomorrowProduction = dailyProduction; // Stima conservativa
            
            // Calcola reward stimato
            int estimatedRewardMin = 0, estimatedRewardMax = 0;
            if (currentPercentage < 50f)
            {
                estimatedRewardMin = 5;
                estimatedRewardMax = 10;
            }
            else if (currentPercentage < 80f)
            {
                estimatedRewardMin = 15;
                estimatedRewardMax = 25;
            }
            else
            {
                estimatedRewardMin = 30;
                estimatedRewardMax = 40;
            }
            
            var sb = new System.Text.StringBuilder();
            
            // Titolo
            sb.AppendLine($"💧 <color=#5DB6E3><b>CONDENSAZIONE</b></color>");
            sb.AppendLine();
            
            // Livello attuale
            sb.AppendLine($"<b>LIVELLO ATTUALE</b> <color=#5DB6E3>{Mathf.RoundToInt(currentPercentage)}%</color>");
            sb.AppendLine();
            
            // Definizione
            sb.AppendLine($"La <color=#5DB6E3>condensazione</color> è acqua grezza (WAT-RAW) raccolta dalla traspirazione delle piante (0-100%).");
            sb.AppendLine();
            
            // Effetto alto
            if (currentPercentage >= 50f)
            {
                sb.AppendLine($"Oltre il 50%, l'umidità ambientale aggiunge <color=#FF0000>giorni virtuali</color> al <color=#FF0000>Rischio Muffa</color> di tutte le piante (fino a +1,5g/giorno).");
                if (virtualDays > 0f)
                {
                    sb.AppendLine($"<color=#FF0000>Attualmente aggiunge +{virtualDays:F1} giorni virtuali/giorno</color>");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"Oltre il 50%, l'umidità ambientale aggiunge <color=#FF0000>giorni virtuali</color> al <color=#FF0000>Rischio Muffa</color> di tutte le piante (fino a +1,5g/giorno).");
                sb.AppendLine();
            }
            
            // Raccolta
            sb.AppendLine($"Raccogliere azzera la %, rimuove i giorni virtuali e produce acqua grezza: <color=#FFA500>più aspetti, maggiore è la ricompensa ma anche il rischio muffa</color>.");
            sb.AppendLine();
            
            // Ricompensa stimata
            sb.AppendLine($"<b>Ricompensa stimata:</b> {estimatedRewardMin}-{estimatedRewardMax} WAT-RAW");
            sb.AppendLine();
            
            // Produzione
            sb.AppendLine($"<b>Produzione di oggi:</b> {dailyProduction:F1}%");
            if (tomorrowProduction > 0f)
            {
                sb.AppendLine($"<b>Stima di domani:</b> ~{tomorrowProduction:F1}%");
            }
            sb.AppendLine();
            
            // LED Bonus
            if (hasLed)
            {
                sb.AppendLine($"✨ <color=#00FF00>Bonus LED attivo: +2 WAT-RAW</color>");
                sb.AppendLine();
            }
            
            // Suggerimento
            sb.AppendLine($"💡 <color=#00FF00>SUGGERIMENTO: L'intervallo ottimale è 70-85%. Monitora ogni giorno per evitare problemi.</color>");
            
            _condensationTooltipText.text = sb.ToString();
            
            // FASE 8: Aggiorna stato button Collect (visibile solo se c'è condensazione disponibile)
            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.style.display = currentPercentage > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                _condensationCollectButton.SetEnabled(currentPercentage > 0f);
            }
        }
        
        /// <summary>
        /// FASE 8: Handler click button Collect nel tooltip condensazione.
        /// </summary>
        private void OnCondensationCollectClicked()
        {
            if (_gameManager == null)
                return;
            
            // Raccoglie condensazione
            int reward = _gameManager.CollectCondensation();
            
            if (reward > 0)
            {
                // Aggiunge WAT-RAW all'inventario
                _gameManager.PlayerInventory.Add(Items.Water, reward);
                
                // Mostra notifica toast
                var foundation = Sporae.UI.UIToolkit.NotificationsFoundation.FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundation != null && foundation.Enabled)
                {
                    foundation.PostToast("WATER-001", new Sporae.UI.UIToolkit.NotificationsFoundation.NotificationPayload().With("amount", reward.ToString()));
                }
                else
                {
                    var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                    if (toastManager != null)
                    {
                        toastManager.ShowToast(ToastNotificationType.ResourceGained, $"You collected Rainwater: {reward}!", "WATER-001");
                    }
                }
                
                // Aggiorna tooltip dopo raccolta
                UpdateCondensationTooltipContent();
            }
        }
        
        #pragma warning restore 0414
    }
}


