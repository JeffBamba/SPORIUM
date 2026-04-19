using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;
using Sporae.UI.UIToolkit.HUD;
using Sporae.DevTools;
using _Project.Sporae.Core;
using _Project;
using Sporae.Core;
using Sporae.UI.UIToolkit.HUD.Components;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Botanical;
using Sporae.Dome;
using Sporae.UI.Icons;
using Sporae.UI.UIToolkit;

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
        /// <summary>Base designer (inspector); la UI mostra base + bonus Glasscap attivo.</summary>
        private float _mutationDesignerBase;
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
        private VisualElement _actionsDisplay;
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
        private VisualElement _mutationDisplay;
        private VisualElement _mutationTooltip;
        private Label _mutationTooltipCurrentLevel;
        private Label _mutationTooltipBreakdownBase;
        private Label _mutationTooltipBreakdownGlasscap;
        private Label _mutationValueLabel;
        // _cryValueLabel rimosso: CRY ora in CompactBottomBar
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
        private VisualElement _phTooltipPassiveList;
        private VisualElement _phTooltipSectionEffects;
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
        private Label _condensationTooltipLevel;
        private Label _condensationTooltipTip;
        private VisualElement _condensationTooltipTipSection;
        private Button _condensationCollectButton;

        // Actions tooltip (foundation CRT, parity con ph/condensation/mutation)
        private VisualElement _actionsTooltip;
        private Label _actionsTooltipTitleStatus;
        private Label _actionsTooltipCurrent;
        private VisualElement _actionsTooltipBreakdownList;
        private Label _actionsTooltipTotal;
        private Label _actionsTooltipHydrationValue;
        private Label _actionsTooltipHydrationSpeed;
        private Label _actionsTooltipHydrationNote;
        private Label _actionsTooltipTip;
        
        // pH Gradient Texture (creata runtime)
        private Texture2D _phGradientTexture;
        
        // pH marker gradient usa PhGradientDisplayColors (stesso gradiente barra)
        private static readonly Color PH_NEUTRAL_ZONE = new Color(0.847f, 1f, 0.898f, 0.35f); // #D8FFE5 35%
        
        // Game Systems
        private GameManager _gameManager;
        private ActionSystem _actionSystem;
        private EconomySystem _economySystem;
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;
        private DayCycleController _dayCycleController;
        private MutationOrbitUI _mutationOrbit;
        
        // Animation coroutines
        private Coroutine _condensationIdleCoroutine;
        private Coroutine _phPulseCoroutine;
        
        // Condensation threshold tracking
        private float _previousCondensation = -1f; // -1 indica valore iniziale non ancora impostato
        
        // pH marker smooth animation (segue oscillazione idle)
        private float _phMarkerLeftPercent = 50f;
        private const float PhCursorOscillationAmplitude = 7.02f;  // +20% forbice (era 5.85)
        private const float PhCursorStepSize = 5f;              // scatti netti: step 5 sulla scala -100..+100 (compatibile con ampiezza ±7)
        private const float PhValueOscillationAmplitude = 0.5f;   // forbice valore numerico: max ±0,5 (oscillazione visibile ma non 3 punti)
        private const float PhCursorOscillationSpeed = 0.25f;
        private const float PhCursorOscillationSeed = 47.3f;
        
        // Colors
        private readonly Color _greenStable = new Color(0.498f, 1f, 0.478f, 1f); // #7FFF7A
        private readonly Color _yellowWarning = new Color(0.902f, 0.788f, 0.435f, 1f); // #E6C96F
        private readonly Color _redCritical = new Color(0.827f, 0.373f, 0.373f, 1f); // #D35F5F
        private readonly Color _blueInfo = new Color(0.365f, 0.714f, 0.890f, 1f); // #5DB6E3
        private Color _actionsBaseColor = new Color(0.902f, 0.788f, 0.435f, 1f); // default giallo
        private const float ActionsPulseSeed = 17.73f;
        private const int ActionsVisualSlots = 5;
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            _mutationOrbit = GetComponent<MutationOrbitUI>();
            
            // Sopra Foundation toasts (150) così i tooltip restano leggibili; sotto modali (400+) e PlantCard (600).
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 200;
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
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true) ?? FindObjectOfType<GameManager>();
            
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
                    // OnCRYChanged rimosso da TopBar: CRY ora gestito da CompactBottomBar
                    _cryBalance = _economySystem.CurrentCRY; // mantiene _cryBalance aggiornato per debug
                    
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
                _dayCycleController = ServiceContainer.Instance?.Get<DayCycleController>(suppressWarning: true);
                
                // Sottoscrivi all'evento OnServiceRegistered per collegarsi quando PhSystem viene registrato
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
                }

                if (_dayCycleSystem != null)
                    _dayCycleSystem.OnDayChanged += OnDayChangedRefreshBotanicalMutation;
                PotEvents.OnPotStateChanged += OnPotStateChangedRefreshBotanicalMutation;

                if (_gameManager.PlayerHydrationSystem != null)
                    _gameManager.PlayerHydrationSystem.OnHydrationChanged += OnHydrationChangedForActionsTooltip;
                if (_gameManager.ActionBudgetLedger != null)
                    _gameManager.ActionBudgetLedger.OnChanged += UpdateActionsTooltipText;
                UpdateActionsTooltipText();
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "TopBarController: GameManager non trovato. Usando valori mock.");
            }
        }

        private void OnDayChangedRefreshBotanicalMutation(int _) => ApplyMutationDisplayFromDesignerBase();

        private void OnPotStateChangedRefreshBotanicalMutation(PotSlot _) => ApplyMutationDisplayFromDesignerBase();
        
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
                ApplyMutationDisplayFromDesignerBase();
                
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
            else if (service is DayCycleController dayCycleController && _dayCycleController == null)
            {
                _dayCycleController = dayCycleController;
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

            UpdateActionsTooltipText();
        }

        private void OnHydrationChangedForActionsTooltip(float _, float __) => UpdateActionsTooltipText();

        private void UpdateActionsTooltipText()
        {
            // Il tooltip ricco (foundation CRT) si aggiorna quando viene mostrato e a ogni
            // evento rilevante (azioni/idratazione/ledger). Qui manteniamo solo un fallback
            // OS-tooltip minimale per accessibilità prima che InitializeUI completi, così da
            // non lasciare tooltip "fantasma" diversi dal contenuto vero.
            if (_actionsTooltip != null)
            {
                // Tooltip ricco già montato: nessun tooltip OS, evita doppie etichette.
                if (_iconActions != null)
                    _iconActions.tooltip = string.Empty;
                if (_actionsDisplay != null)
                    _actionsDisplay.tooltip = string.Empty;

                UpdateActionsTooltipContent();
                return;
            }

            var gm = _gameManager;
            float mul = gm?.PlayerHydrationSystem != null ? gm.PlayerHydrationSystem.GetMovementSpeedMultiplier() : 1f;
            int max = _actionSystem != null ? _actionSystem.MaxActions : 0;
            int left = _actionSystem != null ? _actionSystem.ActionsLeft : 0;
            string text =
                $"Azioni oggi: {left}/{max}.\n" +
                "Cap all’alba dalla colazione (max 5); senza cibo il cap cala; 3 giorni a 1 az. senza cibo → game over.\n" +
                $"Velocità di movimento da idratazione: circa {mul * 100f:0}% del normale.";

            if (_iconActions != null)
                _iconActions.tooltip = text;
            if (_actionsDisplay != null)
                _actionsDisplay.tooltip = text;
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
            _phTooltipPassiveList = _phTooltip?.Q<VisualElement>("ph-tooltip-passive-list");
            _phTooltipSectionEffects = _phTooltip?.Q<VisualElement>("ph-tooltip-section-effects");
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
            if (_phTooltipCrtBars != null)
            {
                foreach (var bar in _phTooltipCrtBars)
                {
                    if (bar != null)
                        bar.pickingMode = PickingMode.Ignore;
                }
            }

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
            float currentPh = _phSystem.CurrentPh;
            int currentDay = _dayCycleSystem != null ? _dayCycleSystem.CurrentDay : 1;

            // Stesso valore oscillante (fake) della top bar — Perlin identico, forbice ridotta per il numero
            float noise = Mathf.PerlinNoise(Time.time * PhCursorOscillationSpeed, PhCursorOscillationSeed);
            float offsetValue = (noise * 2f - 1f) * PhValueOscillationAmplitude;
            float valuePh = Mathf.Clamp(currentPh + offsetValue, -100f, 100f);
            string bandNameDisplay = GetPhBandNameForDisplay(valuePh);

            // CURRENT VALUE: valore con oscillazione ridotta (±0,5)
            _phTooltipValueCurrent.text = $"pH {valuePh.ToString("F1", culture)} — {bandNameDisplay}";
            _phTooltipValueCurrent.style.color = new StyleColor(PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState()));

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
                            string plantName = GetPhModifierPlantLabel(p.PlantCode, p.PotId);
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

            // SLOT PASSIVI CRYO: legge da PhSystem.GetCryoPassiveModifiers() (delta numerico + cap)
            // Fallback label-only da CryoMachineController se PhSystem non ha ancora contributi (giorno 1).
            if (_phTooltipPassiveList != null)
            {
                _phTooltipPassiveList.Clear();
                var cryoMods = _phSystem?.GetCryoPassiveModifiers();
                bool hasCryo = cryoMods != null && cryoMods.Count > 0;

                if (hasCryo)
                {
                    foreach (var m in cryoMods)
                    {
                        string driftStr = m.DailyDrift.ToString("+#0.0;-#0.0;0", culture);
                        string capStr = Mathf.Abs(m.PhCap) > 0.01f
                            ? $" (cap {m.PhCap:+0;-0})" : "";
                        AddPhPassiveRow(_phTooltipPassiveList,
                            m.SlotId, m.PassivePowerLabel,
                            $"{driftStr}/g{capStr}");
                    }
                }
                else
                {
                    // Fallback: legge da CryoMachineController prima del primo tick
                    var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
                    var cryoSlots = cryo?.GetPassiveSlotsSnapshot();
                    bool hasOccupied = false;

                    if (cryoSlots != null)
                    {
                        foreach (var slot in cryoSlots)
                        {
                            if (!slot.IsOccupied) continue;
                            hasOccupied = true;
                            var p = slot.Payload;
                            string plantLabel = string.IsNullOrEmpty(p.CustomPlantName) ? p.PlantCode : p.CustomPlantName;
                            string powerLabel = string.IsNullOrEmpty(p.PassivePowerLabel) ? "—" : p.PassivePowerLabel;
                            AddPhPassiveRow(_phTooltipPassiveList, slot.SlotId, plantLabel, powerLabel);
                        }
                    }

                    if (!hasOccupied)
                    {
                        var emptyRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                        var emptyIcon = new VisualElement { style = { width = 20, height = 20, minWidth = 20, minHeight = 20, marginRight = 8, backgroundColor = new Color(0.31f, 0.78f, 0.86f, 0.1f), borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1, borderLeftColor = new Color(0.31f, 0.78f, 0.86f, 0.3f), borderRightColor = new Color(0.31f, 0.78f, 0.86f, 0.3f), borderTopColor = new Color(0.31f, 0.78f, 0.86f, 0.3f), borderBottomColor = new Color(0.31f, 0.78f, 0.86f, 0.3f) } };
                        var emptyLabel = new Label { text = "Nessun slot cryo attivo", enableRichText = false, style = { color = new Color(0.52f, 0.52f, 0.52f), fontSize = 10 } };
                        emptyRow.Add(emptyIcon);
                        emptyRow.Add(emptyLabel);
                        _phTooltipPassiveList.Add(emptyRow);
                    }
                }
            }

            // Total daily drift = somma degli elementi in Active Modifiers (coerente con la lista)
            float totalDaily = totalFromModifiers;
            string totalStr = totalDaily.ToString("+#0.0;-#0.0;0", culture);
            string stableStr = Mathf.Abs(totalDaily) < 0.2f ? " (Stable)" : "";
            if (_phTooltipValueTotal != null)
                _phTooltipValueTotal.text = $"<color=#7FFF7A>{totalStr}{stableStr}</color>";

            // Effetti globali Dome: solo Attivo/Passivo (PlantData) specie Task 4 — niente banda pH (già sopra)
            if (_phTooltipValueEffects != null)
            {
                var domeFx = new List<string>();
                BotanicalPowerFacade.AppendDomeGlobalPlantPowersTooltipLines(domeFx, _phSystem);
                _phTooltipValueEffects.text = string.Join("\n", domeFx);
                if (_phTooltipSectionEffects != null)
                    _phTooltipSectionEffects.style.display = DisplayStyle.Flex;
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
                "Il forecast del giorno dopo (fine giornata) include tutti gli effetti: piante, LED, azioni.",
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

        /// <summary>Nome specie leggibile + Pot (es. Arctic Hask - Pot-001) per modificatori pH tooltip.</summary>
        private static string GetPhModifierPlantLabel(string plantCode, string potId)
        {
            // Priorita a nome custom del vaso (ibridi Lab), fallback su specie da PlantCode.
            var registry = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            var potState = registry?.FindPotById(potId)?.PotActions?.PotState;
            if (potState != null && !string.IsNullOrWhiteSpace(potState.CustomPlantName))
            {
                string potCustom = string.IsNullOrEmpty(potId) || string.Equals(potId, "Unknown", StringComparison.OrdinalIgnoreCase)
                    ? "—"
                    : potId;
                return $"{potState.CustomPlantName} - {potCustom}";
            }

            string species = BotanicalPlantCodes.GetSpeciesUiDisplayName(plantCode);
            if (string.IsNullOrEmpty(species))
            {
                var pd = PlantDatabase.Instance?.GetPlantDataByCode(plantCode);
                if (pd != null && !string.IsNullOrWhiteSpace(pd.Description))
                {
                    string d = pd.Description.Trim();
                    int nl = d.IndexOfAny(new[] { '\n', '\r' });
                    if (nl > 0)
                        d = d.Substring(0, nl).Trim();
                    int dot = d.IndexOf('.');
                    if (dot > 8 && dot < 52)
                        species = d.Substring(0, dot);
                    else if (d.Length > 44)
                        species = d.Substring(0, 41) + "…";
                    else
                        species = d;
                }
            }

            if (string.IsNullOrEmpty(species))
                species = GetPlantDisplayName(plantCode);

            string pot = string.IsNullOrEmpty(potId) || string.Equals(potId, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? "—"
                : potId;
            return $"{species} - {pot}";
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
                iconBox.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
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
        
        /// <summary>
        /// Aggiunge una riga alla lista Slot Passivi Cryo: [icon box ciano] [SlotId + NomePianta] [PassivePowerLabel in ciano].
        /// </summary>
        private static void AddPhPassiveRow(VisualElement list, string slotId, string plantName, string powerLabel)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var iconBox = new VisualElement { style = { width = 20, height = 20, minWidth = 20, minHeight = 20, marginRight = 8, backgroundColor = new Color(0.31f, 0.78f, 0.86f, 0.2f), borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1, borderLeftColor = new Color(0.31f, 0.78f, 0.86f, 0.6f), borderRightColor = new Color(0.31f, 0.78f, 0.86f, 0.6f), borderTopColor = new Color(0.31f, 0.78f, 0.86f, 0.6f), borderBottomColor = new Color(0.31f, 0.78f, 0.86f, 0.6f) } };
            var nameLabel = new Label { text = $"<b>{slotId}</b>  {plantName}", enableRichText = true };
            nameLabel.style.color = new Color(0.94f, 0.95f, 0.96f);
            nameLabel.style.fontSize = 11;
            nameLabel.style.flexGrow = 1;
            var valueLabel = new Label { text = powerLabel, enableRichText = false };
            valueLabel.style.color = new Color(0.31f, 0.78f, 0.86f);
            valueLabel.style.fontSize = 10;
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
            _actionsDisplay = _root.Q<VisualElement>("actions-display");
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
            _mutationDisplay = _root.Q<VisualElement>("mutation-display");
            _mutationValueLabel = _root.Q<Label>("mutation-value");
            // cry-value rimosso da TopBar — ora in CompactBottomBar
            _grateValueLabel = _root.Q<Label>("grate-value");
            
            // Setup tooltip per pH
            SetupPhTooltip();
            
            // FASE 8: Setup tooltip per condensazione
            SetupCondensationTooltip();

            SetupMutationTooltip();

            SetupActionsTooltip();
            
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
                    5,
                    _yellowWarning,
                    new Color(0.118f, 0.157f, 0.165f, 1f),
                    _yellowWarning
                );
            }
        }

        private void SetupGlowFrame()
        {
            if (_glowFrame == null) return;

            _glowFrame.pickingMode = PickingMode.Ignore;

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
            
            // Cursore pH: animazione idle che segue l'oscillazione (valore numerico e marker in sync)
            if (_phSystem != null && _phMarker != null && _phSlider != null)
            {
                float currentPh = _phSystem.CurrentPh;
                // Oscillazione: valore numerico con forbice ridotta (±0,5); cursore con forbice +20% e scatti netti
                float noise = Mathf.PerlinNoise(Time.time * PhCursorOscillationSpeed, PhCursorOscillationSeed);
                float offsetValue = (noise * 2f - 1f) * PhValueOscillationAmplitude;
                float offsetCursor = (noise * 2f - 1f) * PhCursorOscillationAmplitude;
                float valuePh = Mathf.Clamp(currentPh + offsetValue, -100f, 100f);
                float cursorPh = Mathf.Clamp(currentPh + offsetCursor, -100f, 100f);
                // Cursore a scatti netti: quantizza a step fissi
                float cursorPhStepped = Mathf.Round(cursorPh / PhCursorStepSize) * PhCursorStepSize;
                cursorPhStepped = Mathf.Clamp(cursorPhStepped, -100f, 100f);
                // Valore numerico: oscillazione ridotta (max ±0,5)
                if (_phBandLabel != null)
                {
                    _phBandLabel.text = $"{valuePh:F1}";
                    _phBandLabel.style.color = new StyleColor(
                        _phSystem != null
                            ? PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState())
                            : GetPhColorFromDrift(valuePh));
                }
                // Target posizione marker: forbice +20%, movimento a scatti
                float targetLeftPercent = ((cursorPhStepped + 100f) / 200f) * 100f;
                float sliderWidth = _phSlider.resolvedStyle.width;
                float markerWidth = 12f;
                if (!float.IsNaN(sliderWidth) && sliderWidth > 0f)
                {
                    float offsetPercent = (markerWidth / 2f / sliderWidth) * 100f;
                    targetLeftPercent -= offsetPercent;
                }
                _phMarkerLeftPercent = targetLeftPercent; // scatti netti: niente lerp
                _phMarker.style.left = new StyleLength(new Length(_phMarkerLeftPercent, LengthUnit.Percent));
                _phMarker.style.backgroundColor = new StyleColor(Color.white);
            }
            
            // Lampeggio cursore pH: sempre attivo quando il marker esiste, per far capire che è "staccato" e animato
            if (_phMarker != null)
            {
                float blink = 0.5f + 0.5f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f));
                _phMarker.style.opacity = blink;
            }

            // Tooltip pH: aggiorna Current Value ogni frame quando aperto così segue l'oscillazione come la top bar
            if (_phTooltip != null && _phTooltip.style.display == DisplayStyle.Flex)
                UpdatePhTooltipContent();
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
            UpdateMutation(_mutationIndex); // primo avvio: serializzato → designer base + bonus
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

            int visibleCurrent = Mathf.Clamp(current, 0, ActionsVisualSlots);

            if (_actionsValueLabel != null)
                _actionsValueLabel.text = $"{visibleCurrent}/{ActionsVisualSlots}";

            // Barra su scala fissa 0..5: il cap runtime può scendere (es. malnutrizione),
            // ma la visualizzazione deve restare "X su 5" per evitare ambiguita'.
            Color fillColor;
            if (visibleCurrent >= 4)
                fillColor = _greenStable;
            else if (visibleCurrent >= 2)
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
                _actionsBar.UpdateValue(visibleCurrent, ActionsVisualSlots);
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
            
            // Gradiente striscia 0–14 (rosso → … → viola), stesso campionamento di PhGradientDisplayColors
            for (int x = 0; x < width; x++)
            {
                float normalizedX = x / (float)(width - 1);
                float phValue = normalizedX * 14f; // 0-14 scale
                Color pixelColor = PhGradientDisplayColors.GetColorFromScale(phValue);
                
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

        private Color GetPhColorFromDrift(float driftPh) => PhGradientDisplayColors.GetColorFromDrift(driftPh);

        /// <summary>Restituisce una banda pH per display (scala -100..+100) per il valore oscillante nel tooltip.</summary>
        private static string GetPhBandNameForDisplay(float ph)
        {
            if (ph < -25f) return "Acido";
            if (ph > 25f) return "Basico";
            return "Neutrale";
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
            
            // Aggiorna label "PH DRIFT" — colore dalla banda pH Dome corrente
            if (_phDriftLabel != null)
            {
                _phDriftLabel.text = "DRIFT pH";
                Color phDriftColor = _phSystem != null
                    ? PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState())
                    : PhGradientDisplayColors.GetColorFromScale(7f);
                phDriftColor.a = 0.85f;
                _phDriftLabel.style.color = new StyleColor(phDriftColor);
            }
            
            // Aggiorna valore numerico pH (mostra drift -100/+100; default 0 con oscillazione estetica)
            if (_phBandLabel != null)
            {
                // Mostra valore drift diretto (-100/+100), inizio partita = 0
                _phBandLabel.text = $"{value:F1}";
                Color valueColor = _phSystem != null
                    ? PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState())
                    : GetPhColorFromDrift(value);
                _phBandLabel.style.color = new StyleColor(valueColor);
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
                _phMarkerLeftPercent = leftPositionPercent; // sync per animazione idle
                
                // Reset margin-left per evitare conflitti
                _phMarker.style.marginLeft = 0f;
                
                // Marker sempre bianco (il colore di stato è sul numero e sulla barra)
                _phMarker.style.backgroundColor = new StyleColor(Color.white);
                
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
            
            if (phValue < 6f)
                glowColor = PhGradientDisplayColors.GetColorFromScale(2f);
            else if (phValue <= 8f)
                glowColor = PhGradientDisplayColors.GetColorFromScale(7f);
            else
                glowColor = PhGradientDisplayColors.GetColorFromScale(11f);
            
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
        /// Aggiorna la base designer dell'indice mutazione e ricalcola il display (incluso bonus Glasscap attivo).
        /// </summary>
        public void UpdateMutation(float designerBaseIndex)
        {
            _mutationDesignerBase = Mathf.Clamp01(designerBaseIndex);
            ApplyMutationDisplayFromDesignerBase();
        }

        private void ApplyMutationDisplayFromDesignerBase()
        {
            var mutSvc = ServiceContainer.Instance?.Get<DomeMutationRuntimeService>(suppressWarning: true);
            if (mutSvc != null)
            {
                mutSvc.SyncDisplay(_mutationDesignerBase, _phSystem);
                _mutationIndex = mutSvc.DisplayNormalized;
            }
            else
            {
                // Fallback: scena senza GamePlayInstaller / servizio non registrato.
                float bonus = 0f;
                if (_phSystem != null)
                    bonus = BotanicalRosterSnapshot.FromServices(_phSystem).GlasscapActiveMutationBonusSum;
                _mutationIndex = Mathf.Clamp01(_mutationDesignerBase + bonus);
            }

            if (_mutationValueLabel != null)
            {
                int percentage = Mathf.RoundToInt(_mutationIndex * 100f);
                _mutationValueLabel.text = $"{percentage}%";

                Color color;
                if (_mutationIndex <= 0.33f)
                    color = _greenStable;
                else if (_mutationIndex <= 0.66f)
                    color = _yellowWarning;
                else
                    color = _redCritical;

                _mutationValueLabel.style.color = new StyleColor(color);
            }

            if (_mutationOrbit != null)
                _mutationOrbit.UpdateMutation(_mutationIndex);

            RefreshMutationTooltipIfOpen();
        }
        
        /// <summary>
        /// Aggiorna il valore CRY interno (nessuna UI: il saldo è in CompactBottomBar).
        /// </summary>
        public void UpdateCryBalance(int value)
        {
            _cryBalance = value;
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
            
            // OnCRYChanged unsub rimosso: subscription non più registrata in TopBar
            
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

            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= OnDayChangedRefreshBotanicalMutation;
            PotEvents.OnPotStateChanged -= OnPotStateChangedRefreshBotanicalMutation;

            if (_gameManager?.PlayerHydrationSystem != null)
                _gameManager.PlayerHydrationSystem.OnHydrationChanged -= OnHydrationChangedForActionsTooltip;

            if (_gameManager?.ActionBudgetLedger != null)
                _gameManager.ActionBudgetLedger.OnChanged -= UpdateActionsTooltipText;

            if (_actionsDisplay != null)
            {
                _actionsDisplay.UnregisterCallback<MouseEnterEvent>(OnActionsHoverEnter);
                _actionsDisplay.UnregisterCallback<MouseLeaveEvent>(OnActionsHoverExit);
                _actionsDisplay.UnregisterCallback<MouseMoveEvent>(OnActionsHoverMove);
            }

            if (_mutationDisplay != null)
            {
                _mutationDisplay.UnregisterCallback<MouseEnterEvent>(OnMutationHoverEnter);
                _mutationDisplay.UnregisterCallback<MouseLeaveEvent>(OnMutationHoverExit);
                _mutationDisplay.UnregisterCallback<MouseMoveEvent>(OnMutationHoverMove);
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
            
            // FASE 8: Unregister collect shortcut (doppio clic sulla metrica, stesso modello tooltip ph: niente hover sul tooltip)
            if (_condensationDisplay != null)
                _condensationDisplay.UnregisterCallback<ClickEvent>(OnCondensationDisplayDoubleClickCollect);
            
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
            
            // OnCRYChanged re-sub rimosso: CRY ora in CompactBottomBar
            
            // FASE 10: Re-subscribe a CondensationSystem
            if (_gameManager != null)
            {
                _gameManager.OnCondensationChanged += OnCondensationChanged;
                if (_gameManager.PlayerHydrationSystem != null)
                {
                    _gameManager.PlayerHydrationSystem.OnHydrationChanged -= OnHydrationChangedForActionsTooltip;
                    _gameManager.PlayerHydrationSystem.OnHydrationChanged += OnHydrationChangedForActionsTooltip;
                }
                if (_gameManager.ActionBudgetLedger != null)
                {
                    _gameManager.ActionBudgetLedger.OnChanged -= UpdateActionsTooltipText;
                    _gameManager.ActionBudgetLedger.OnChanged += UpdateActionsTooltipText;
                }
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
        
        /// <summary>Pixel font sizes per tooltip condensazione — in sync con TopBar.uss (.condensation-tooltip-root).</summary>
        private static class CondensationTooltipFontPx
        {
            public const float Title = 13f;
            public const float Caption = 12f;
            public const float Level = 12f;
            public const float Body = 10f;
            public const float Tip = 10f;
            public const float Button = 11f;
        }

        /// <summary>Riapplica tipografia dopo USS e dopo assegnazione rich text (il testo ricco può ignorare la font-size del Label).</summary>
        private void ReapplyCondensationTooltipTypography()
        {
            if (_condensationTooltip == null)
                return;

            void SetFont(Label label, float px, FontStyle weight)
            {
                if (label == null) return;
                label.style.fontSize = px;
                label.style.unityFontStyleAndWeight = weight;
            }

            SetFont(_condensationTooltip.Q<Label>("condensation-tooltip-title"), CondensationTooltipFontPx.Title, FontStyle.Bold);
            SetFont(_condensationTooltip.Q<Label>("condensation-tooltip-level-caption"), CondensationTooltipFontPx.Caption, FontStyle.Normal);
            SetFont(_condensationTooltipLevel, CondensationTooltipFontPx.Level, FontStyle.Bold);
            SetFont(_condensationTooltipText, CondensationTooltipFontPx.Body, FontStyle.Normal);
            SetFont(_condensationTooltip.Q<Label>("condensation-tooltip-tip-caption"), CondensationTooltipFontPx.Caption, FontStyle.Bold);
            SetFont(_condensationTooltipTip, CondensationTooltipFontPx.Tip, FontStyle.Normal);

            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.style.fontSize = CondensationTooltipFontPx.Button;
                _condensationCollectButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        }

        private void SetupMutationTooltip()
        {
            _mutationTooltip = _root?.Q<VisualElement>("mutation-tooltip");
            if (_mutationTooltip != null)
                _mutationTooltip.pickingMode = PickingMode.Ignore;
            _mutationTooltipCurrentLevel = _mutationTooltip?.Q<Label>("mutation-tooltip-current-level");
            _mutationTooltipBreakdownBase = _mutationTooltip?.Q<Label>("mutation-tooltip-breakdown-base");
            _mutationTooltipBreakdownGlasscap = _mutationTooltip?.Q<Label>("mutation-tooltip-breakdown-glasscap");
            if (_mutationDisplay == null || _mutationTooltip == null)
                return;
            _mutationDisplay.RegisterCallback<MouseEnterEvent>(OnMutationHoverEnter);
            _mutationDisplay.RegisterCallback<MouseLeaveEvent>(OnMutationHoverExit);
            _mutationDisplay.RegisterCallback<MouseMoveEvent>(OnMutationHoverMove);
        }

        private void OnMutationHoverEnter(MouseEnterEvent evt)
        {
            if (_mutationTooltip == null) return;
            UpdateMutationTooltipContent();
            _mutationTooltip.style.display = DisplayStyle.Flex;
            _mutationTooltip.BringToFront();
            PositionMutationTooltipNearDisplay();
        }

        private void OnMutationHoverExit(MouseLeaveEvent evt)
        {
            if (_mutationTooltip != null)
                _mutationTooltip.style.display = DisplayStyle.None;
        }

        private void OnMutationHoverMove(MouseMoveEvent evt)
        {
            PositionMutationTooltipNearDisplay();
        }

        private void PositionMutationTooltipNearDisplay()
        {
            if (_mutationTooltip == null || _mutationTooltip.style.display != DisplayStyle.Flex || _mutationDisplay == null || _root == null)
                return;
            var b = _mutationDisplay.worldBound;
            var rootBounds = _root.worldBound;
            float x = b.xMax + 10f;
            float y = b.yMax - 20f;
            const float tw = 420f;
            float th = _mutationTooltip.resolvedStyle.height;
            if (th < 1f) th = 200f;
            if (x + tw > rootBounds.width)
                x = Mathf.Max(0f, b.xMin - tw - 10f);
            if (y + th > rootBounds.height)
                y = Mathf.Max(0f, b.yMin - th - 10f);
            _mutationTooltip.style.left = x;
            _mutationTooltip.style.top = y;
        }

        private void UpdateMutationTooltipContent()
        {
            if (_mutationTooltipCurrentLevel == null) return;
            var culture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
            int pct = Mathf.RoundToInt(_mutationIndex * 100f);
            string band = DomeMutationRuntimeService.GetBandLabelItalian(_mutationIndex);
            _mutationTooltipCurrentLevel.text = $"{pct.ToString(culture)}% — {band}";
            Color c;
            if (_mutationIndex <= DomeMutationRuntimeService.BandStableMax)
                c = _greenStable;
            else if (_mutationIndex <= DomeMutationRuntimeService.BandBalancedMax)
                c = _yellowWarning;
            else
                c = _redCritical;
            _mutationTooltipCurrentLevel.style.color = new StyleColor(c);

            var mutSvc = ServiceContainer.Instance?.Get<DomeMutationRuntimeService>(suppressWarning: true);
            if (_mutationTooltipBreakdownBase != null)
            {
                float b = mutSvc != null ? mutSvc.DesignerBaseNormalized : _mutationDesignerBase;
                _mutationTooltipBreakdownBase.text = $"Base designer: {Mathf.RoundToInt(b * 100f).ToString(culture)}%";
            }

            if (_mutationTooltipBreakdownGlasscap != null)
            {
                float g = mutSvc != null ? mutSvc.GlasscapActiveBonusSum : 0f;
                if (mutSvc == null && _phSystem != null)
                    g = BotanicalRosterSnapshot.FromServices(_phSystem).GlasscapActiveMutationBonusSum;
                int gp = Mathf.RoundToInt(g * 100f);
                _mutationTooltipBreakdownGlasscap.text = gp > 0
                    ? $"Bonus Glasscap (vasi attivi): +{gp.ToString(culture)}%"
                    : "Bonus Glasscap (vasi attivi): +0% (nessuna Glasscap in crescita)";
            }
        }

        private void RefreshMutationTooltipIfOpen()
        {
            if (_mutationTooltip != null && _mutationTooltip.style.display == DisplayStyle.Flex)
                UpdateMutationTooltipContent();
        }

        // ---------------------------------------------------------------------
        // Tooltip AZIONI (foundation CRT, parity con ph/condensation/mutation)
        // Mostra: breakdown del cap giornaliero (chi ha dato le azioni — colazione,
        // futuri moduli/ambiente/item) + stato idratazione (velocità di movimento).
        // ---------------------------------------------------------------------
        private void SetupActionsTooltip()
        {
            _actionsTooltip = _root?.Q<VisualElement>("actions-tooltip");
            if (_actionsTooltip == null) return;

            _actionsTooltip.pickingMode = PickingMode.Ignore;
            _actionsTooltipTitleStatus = _actionsTooltip.Q<Label>("actions-tooltip-title-status");
            _actionsTooltipCurrent = _actionsTooltip.Q<Label>("actions-tooltip-current");
            _actionsTooltipBreakdownList = _actionsTooltip.Q<VisualElement>("actions-tooltip-breakdown-list");
            _actionsTooltipTotal = _actionsTooltip.Q<Label>("actions-tooltip-total");
            _actionsTooltipHydrationValue = _actionsTooltip.Q<Label>("actions-tooltip-hydration-value");
            _actionsTooltipHydrationSpeed = _actionsTooltip.Q<Label>("actions-tooltip-hydration-speed");
            _actionsTooltipHydrationNote = _actionsTooltip.Q<Label>("actions-tooltip-hydration-note");
            _actionsTooltipTip = _actionsTooltip.Q<Label>("actions-tooltip-tip");

            if (_actionsDisplay != null)
            {
                _actionsDisplay.RegisterCallback<MouseEnterEvent>(OnActionsHoverEnter);
                _actionsDisplay.RegisterCallback<MouseLeaveEvent>(OnActionsHoverExit);
                _actionsDisplay.RegisterCallback<MouseMoveEvent>(OnActionsHoverMove);
            }

            // Ora che il tooltip ricco è pronto, popola immediatamente con stato corrente.
            UpdateActionsTooltipContent();
        }

        private void OnActionsHoverEnter(MouseEnterEvent evt)
        {
            if (_actionsTooltip == null) return;
            UpdateActionsTooltipContent();
            _actionsTooltip.style.display = DisplayStyle.Flex;
            _actionsTooltip.BringToFront();
            PositionActionsTooltipNearDisplay();
        }

        private void OnActionsHoverExit(MouseLeaveEvent evt)
        {
            if (_actionsTooltip != null)
                _actionsTooltip.style.display = DisplayStyle.None;
        }

        private void OnActionsHoverMove(MouseMoveEvent evt)
        {
            PositionActionsTooltipNearDisplay();
        }

        private void PositionActionsTooltipNearDisplay()
        {
            if (_actionsTooltip == null || _actionsTooltip.style.display != DisplayStyle.Flex || _actionsDisplay == null || _root == null)
                return;

            var b = _actionsDisplay.worldBound;
            var rootBounds = _root.worldBound;
            float x = b.xMax + 10f;
            float y = b.yMax - 20f;
            const float tw = 480f;
            float th = _actionsTooltip.resolvedStyle.height;
            if (th < 1f) th = 220f;
            if (x + tw > rootBounds.width)
                x = Mathf.Max(0f, b.xMin - tw - 10f);
            if (y + th > rootBounds.height)
                y = Mathf.Max(0f, b.yMin - th - 10f);
            _actionsTooltip.style.left = x;
            _actionsTooltip.style.top = y;
        }

        private void UpdateActionsTooltipContent()
        {
            if (_actionsTooltip == null) return;

            int left = _actionSystem != null ? _actionSystem.ActionsLeft : 0;
            int max = _actionSystem != null ? _actionSystem.MaxActions : 0;

            if (_actionsTooltipTitleStatus != null)
                _actionsTooltipTitleStatus.text = $"{left}/{max}";

            if (_actionsTooltipCurrent != null)
                _actionsTooltipCurrent.text = $"<b>{left}</b>/<b>{max}</b> azioni rimanenti oggi";

            // Breakdown dal ledger (colazione + futuri moduli/ambiente/item).
            if (_actionsTooltipBreakdownList != null)
            {
                _actionsTooltipBreakdownList.Clear();
                var ledger = _gameManager != null ? _gameManager.ActionBudgetLedger : null;
                int ledgerSum = 0;

                if (ledger != null && ledger.Entries.Count > 0)
                {
                    foreach (var e in ledger.Entries)
                    {
                        ledgerSum += e.Amount;
                        _actionsTooltipBreakdownList.Add(BuildBreakdownRow(e));
                    }
                }
                else
                {
                    _actionsTooltipBreakdownList.Add(BuildBreakdownRow(new ActionBudgetEntry
                    {
                        Source = ActionBudgetSource.Breakfast,
                        Label = "Colazione (base)",
                        Amount = max
                    }));
                }

                // Se il cap effettivo diverge dal ledger (es. debug diretto su ActionSystem), mostra una riga "altro".
                int clampedLedger = Mathf.Clamp(ledgerSum, 0, 5);
                int delta = max - clampedLedger;
                if (delta != 0)
                {
                    _actionsTooltipBreakdownList.Add(BuildBreakdownRow(new ActionBudgetEntry
                    {
                        Source = ActionBudgetSource.Other,
                        Label = delta > 0 ? "Regolazione runtime" : "Limite runtime",
                        Amount = delta
                    }));
                }
            }

            if (_actionsTooltipTotal != null)
                _actionsTooltipTotal.text = $"<b>{max}</b> azioni  ({left} rimanenti)";

            UpdateActionsTooltipHydrationSection();

            if (_actionsTooltipTip != null)
            {
                _actionsTooltipTip.text =
                    "Devi mangiare (cibo o frutta) almeno una volta ogni due giorni: dal terzo giorno " +
                    "senza pasto il cap scende di 1 al giorno (minimo 1/5). " +
                    "Tre giorni di fila a 1 azione senza cibo → game over per fame. " +
                    "In futuro: moduli, bonus ambiente e item sul cap.";
            }
        }

        private VisualElement BuildBreakdownRow(ActionBudgetEntry entry)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2f;

            var dot = new VisualElement();
            dot.style.width = 6f;
            dot.style.height = 6f;
            dot.style.minWidth = 6f;
            dot.style.minHeight = 6f;
            dot.style.marginRight = 6f;
            dot.style.borderTopLeftRadius = 3f;
            dot.style.borderTopRightRadius = 3f;
            dot.style.borderBottomLeftRadius = 3f;
            dot.style.borderBottomRightRadius = 3f;
            dot.style.backgroundColor = new StyleColor(GetBreakdownColor(entry.Source));
            row.Add(dot);

            var label = new Label($"{GetSourceIcon(entry.Source)}  {entry.Label}");
            label.style.color = new StyleColor(new Color(0.78f, 0.80f, 0.78f, 1f));
            label.style.fontSize = 11f;
            label.enableRichText = true;
            row.Add(label);

            if (!string.IsNullOrEmpty(entry.Detail))
            {
                var detail = new Label($"— {entry.Detail}");
                detail.style.color = new StyleColor(new Color(0.55f, 0.58f, 0.55f, 1f));
                detail.style.fontSize = 10f;
                detail.style.marginLeft = 6f;
                detail.enableRichText = true;
                row.Add(detail);
            }

            var value = new Label(entry.Amount > 0 ? $"+{entry.Amount}" : entry.Amount.ToString());
            value.style.marginLeft = StyleKeyword.Auto;
            value.style.marginRight = 8f;
            value.style.color = new StyleColor(entry.Amount >= 0 ? _greenStable : _redCritical);
            value.style.fontSize = 11f;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(value);

            return row;
        }

        private Color GetBreakdownColor(ActionBudgetSource source)
        {
            switch (source)
            {
                case ActionBudgetSource.Breakfast: return new Color(0.498f, 1f, 0.478f, 1f); // verde stabile
                case ActionBudgetSource.Module: return new Color(0.365f, 0.714f, 0.890f, 1f); // azzurro
                case ActionBudgetSource.Environment: return new Color(0.902f, 0.788f, 0.435f, 1f); // giallo
                case ActionBudgetSource.Item: return new Color(0.851f, 0.439f, 0.937f, 1f); // magenta
                case ActionBudgetSource.Malnutrition: return new Color(0.827f, 0.373f, 0.373f, 1f); // rosso fame
                case ActionBudgetSource.Override: return new Color(0.78f, 0.80f, 0.78f, 1f); // grigio
                default: return new Color(0.55f, 0.58f, 0.55f, 1f);
            }
        }

        private string GetSourceIcon(ActionBudgetSource source)
        {
            switch (source)
            {
                case ActionBudgetSource.Breakfast: return "▣"; // piatto / colazione
                case ActionBudgetSource.Module: return "◫";
                case ActionBudgetSource.Environment: return "◊";
                case ActionBudgetSource.Item: return "◉";
                case ActionBudgetSource.Malnutrition: return "−";
                case ActionBudgetSource.Override: return "⚙";
                default: return "·";
            }
        }

        private void UpdateActionsTooltipHydrationSection()
        {
            var hyd = _gameManager != null ? _gameManager.PlayerHydrationSystem : null;
            if (hyd == null)
            {
                if (_actionsTooltipHydrationValue != null) _actionsTooltipHydrationValue.text = "—";
                if (_actionsTooltipHydrationSpeed != null) _actionsTooltipHydrationSpeed.text = "Velocità di movimento: —";
                if (_actionsTooltipHydrationNote != null)
                    _actionsTooltipHydrationNote.text = "L’idratazione non influenza le Azioni: regola solo la velocità di movimento.";
                return;
            }

            float h = hyd.HydrationPercent;
            float mul = hyd.GetMovementSpeedMultiplier();

            if (_actionsTooltipHydrationValue != null)
                _actionsTooltipHydrationValue.text = $"{Mathf.RoundToInt(h)}% H";

            string speedLabel;
            Color speedColor;
            if (h > 50f)
            {
                speedLabel = $"Velocità di movimento: <b>100%</b> (H {Mathf.RoundToInt(h)}% — nessuna penalità)";
                speedColor = _greenStable;
            }
            else if (h > 25f)
            {
                speedLabel = $"Velocità di movimento: <b>~{Mathf.RoundToInt(mul * 100f)}%</b> del normale (H {Mathf.RoundToInt(h)}% — leggera penalità sotto il 50%)";
                speedColor = _yellowWarning;
            }
            else if (h > 0.01f)
            {
                speedLabel = $"Velocità di movimento: <b>~{Mathf.RoundToInt(mul * 100f)}%</b> del normale (H {Mathf.RoundToInt(h)}% — forte penalità sotto il 25%)";
                speedColor = _redCritical;
            }
            else
            {
                speedLabel = "Velocità di movimento: <b>minima</b> (H ≈ 0% — rischio game over se resti a 0% per 2 giorni)";
                speedColor = _redCritical;
            }

            if (_actionsTooltipHydrationSpeed != null)
            {
                _actionsTooltipHydrationSpeed.text = speedLabel;
                _actionsTooltipHydrationSpeed.style.color = new StyleColor(speedColor);
            }

            if (_actionsTooltipHydrationNote != null)
            {
                _actionsTooltipHydrationNote.text =
                    "L’idratazione non influenza le Azioni: regola solo la velocità. " +
                    "Il cap azioni dipende dalla colazione e dalla fame (mangiare almeno ogni 2 giorni).";
            }
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
            _condensationTooltipLevel = _condensationTooltip?.Q<Label>("condensation-tooltip-level");
            _condensationTooltipTip = _condensationTooltip?.Q<Label>("condensation-tooltip-tip");
            _condensationTooltipTipSection = _condensationTooltip?.Q<VisualElement>("condensation-tooltip-section-tip");
            _condensationCollectButton = _condensationTooltip?.Q<Button>("condensation-collect-button");

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

            // Stesso modello del ph-tooltip: il tooltip non intercetta il puntatore; chiusura all'uscita dalla metrica.
            if (_condensationTooltip != null)
                _condensationTooltip.pickingMode = PickingMode.Ignore;

            _condensationDisplay.RegisterCallback<MouseEnterEvent>(OnCondensationHoverEnter);
            _condensationDisplay.RegisterCallback<MouseLeaveEvent>(OnCondensationHoverExit);
            _condensationDisplay.RegisterCallback<MouseMoveEvent>(OnCondensationHoverMove);
            // Raccolta: con tooltip come ph non si può passare dal display al pulsante nel tooltip; doppio clic sulla metrica equivale a "Raccogli".
            _condensationDisplay.RegisterCallback<ClickEvent>(OnCondensationDisplayDoubleClickCollect);

            ReapplyCondensationTooltipTypography();
        }
        
        /// <summary>
        /// FASE 8: Handler hover enter per condensazione display.
        /// </summary>
        private void OnCondensationHoverEnter(MouseEnterEvent evt)
        {
            if (_gameManager != null && _gameManager.CondensationSystem != null && _condensationTooltip != null)
            {
                UpdateCondensationTooltipContent();
                _condensationTooltip.style.display = DisplayStyle.Flex;
                _condensationTooltip.BringToFront();
                UpdateCondensationTooltipPosition();
            }
        }

        /// <summary>FASE 8: Come OnPhHoverExit — chiude appena il puntatore esce dalla metrica condensazione.</summary>
        private void OnCondensationHoverExit(MouseLeaveEvent evt)
        {
            if (_condensationTooltip != null)
                _condensationTooltip.style.display = DisplayStyle.None;
        }

        /// <summary>FASE 8: Doppio clic sulla metrica condensazione per raccogliere (stesso tooltip non interattivo come ph-tooltip).</summary>
        private void OnCondensationDisplayDoubleClickCollect(ClickEvent evt)
        {
            if (evt.clickCount != 2)
                return;
            if (_gameManager == null || _gameManager.CondensationSystem == null)
                return;
            if (_gameManager.CondensationSystem.CurrentAccumulation <= 0f)
                return;
            OnCondensationCollectClicked();
        }

        /// <summary>FASE 8: Posizionamento tooltip accanto a condensation-display (stessa logica numerica di OnPhHoverMove).</summary>
        private void UpdateCondensationTooltipPosition()
        {
            if (_condensationTooltip == null || _condensationTooltip.style.display != DisplayStyle.Flex || _condensationDisplay == null)
                return;

            var displayBounds = _condensationDisplay.worldBound;
            var rootBounds = _root.worldBound;

            float tooltipX = displayBounds.xMax + 10f;
            float tooltipY = displayBounds.yMax - 20f;

            float tooltipWidth = 480f;
            float tooltipHeight = _condensationTooltip.resolvedStyle.height;

            if (tooltipX + tooltipWidth > rootBounds.width)
                tooltipX = displayBounds.xMin - tooltipWidth - 10f;

            if (tooltipY + tooltipHeight > rootBounds.height)
                tooltipY = displayBounds.yMin - tooltipHeight - 10f;

            _condensationTooltip.style.left = tooltipX;
            _condensationTooltip.style.top = tooltipY;
        }
        
        /// <summary>
        /// FASE 8: Handler hover move per condensazione (posizionamento tooltip).
        /// </summary>
        private void OnCondensationHoverMove(MouseMoveEvent evt)
        {
            UpdateCondensationTooltipPosition();
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
            var dayCycleController = _dayCycleController ?? ServiceContainer.Instance?.Get<DayCycleController>(suppressWarning: true);
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
            
            if (_condensationTooltipLevel != null)
                _condensationTooltipLevel.text = $"{Mathf.RoundToInt(currentPercentage)}%";

            var sb = new System.Text.StringBuilder();

            // Definizione (titolo e livello sono nel layout UXML, come il ph-tooltip)
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
            
            _condensationTooltipText.text = sb.ToString();

            const string tipRich =
                "💡 <color=#7FFF7A>L'intervallo ottimale è 70-85%. Monitora ogni giorno per evitare problemi.</color>";
            if (_condensationTooltipTip != null)
                _condensationTooltipTip.text = tipRich;
            if (_condensationTooltipTipSection != null)
                _condensationTooltipTipSection.style.display = DisplayStyle.Flex;
            
            // FASE 8: Aggiorna stato button Collect (visibile solo se c'è condensazione disponibile)
            if (_condensationCollectButton != null)
            {
                _condensationCollectButton.style.display = currentPercentage > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                _condensationCollectButton.SetEnabled(currentPercentage > 0f);
            }

            ReapplyCondensationTooltipTypography();
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


