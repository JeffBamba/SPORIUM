using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Botanical;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlantCard.Helpers;
using Sporae.UI.UIToolkit;
using Sporae.UI.UIToolkit.HUD;

namespace Sporae.UI.UIToolkit.DomeStatusHUD
{
    /// <summary>
    /// Controller UIToolkit per l'HUD unificato Dome Status (pots attivi + cryo slot).
    /// Sidebar fissa sempre visibile. Ogni card POT può essere espansa con click.
    /// sortingOrder = 55 (tra TopBar/Bottom 50 e Foundation 60).
    /// In UI Builder apri <c>DomeStatusHUD.uxml</c>: ogni elemento ha <c>name</c>; <c>dome-hud-tooltip</c> include
    /// placeholder con le stesse classi di <c>SetTooltipLines</c>; <c>dome-hud-builder-reference</c> aggiunge card/espanso/CRYO.
    /// Parità Builder/runtime: vedi <c>.cursor/rules/ui-hud-foundation-ui-builder-parity.mdc</c>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DomeStatusHUDController : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private StyleSheet _spFoundation;
        [SerializeField] private StyleSheet _spPanelBase;
        [SerializeField] private StyleSheet _panelUss;

        [Header("UI Builder")]
        [Tooltip("Se attivo, il blocco dome-hud-builder-reference (campione tooltip/card/CRYO) resta visibile anche in Play Mode.")]
        [SerializeField] private bool _showBuilderReferenceDuringPlay;

        [Header("HUD collapse")]
        [Tooltip("Se true, all'avvio mostra tab POT/CRYO e card; se false, resta solo la barra con toggle (come header notifiche).")]
        [SerializeField] private bool _startHudExpanded = true;

        // ── Services ──────────────────────────────────────────────────
        private DomePotRegistry _potRegistry;
        private CryoMachineController _cryoMachine;
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;
        private PotSystemConfig _potSystemConfig;

        // ── Root elements ──────────────────────────────────────────────
        private VisualElement _hudRoot;
        private Button _tabPots;
        private Button _tabCryo;
        private Label _tabPotsLabel;
        private Label _tabCryoLabel;
        private VisualElement _sectionPots;
        private VisualElement _sectionCryo;
        private VisualElement _tooltip;
        private VisualElement _tooltipLines;

        // ── Per-pot card elements ──────────────────────────────────────
        private readonly VisualElement[] _potCards        = new VisualElement[4];
        private readonly VisualElement[] _potHeaders      = new VisualElement[4];
        private readonly Label[]         _potChevrons     = new Label[4];
        private readonly Label[]         _potBadges       = new Label[4];
        private readonly VisualElement[] _potPreviews     = new VisualElement[4];
        private readonly Label[]         _potNames        = new Label[4];
        private readonly Label[]         _potSubs         = new Label[4];
        private readonly VisualElement[] _potCondRows     = new VisualElement[4];
        private readonly VisualElement[] _potCondDots     = new VisualElement[4];
        private readonly Label[]         _potConds        = new Label[4];
        private readonly Label[]         _potWater        = new Label[4];
        private readonly Label[]         _potLed          = new Label[4];

        // ── Per-pot expanded area elements ─────────────────────────────
        private readonly VisualElement[] _potExpandedAreas = new VisualElement[4];
        private readonly Label[]         _potStatWater     = new Label[4];
        private readonly Label[]         _potStatFert      = new Label[4];
        private readonly Label[]         _potStatLed       = new Label[4];
        private readonly Label[]         _potStatPh        = new Label[4];

        // ── Per-cryo row elements ──────────────────────────────────────
        private readonly VisualElement[] _cryoRows    = new VisualElement[3];
        private readonly Label[]         _cryoIds     = new Label[3];
        private readonly Label[]         _cryoPlants  = new Label[3];
        private readonly Label[]         _cryoDetails = new Label[3];

        // ── Data caches ────────────────────────────────────────────────
        private readonly PotSlot[]       _cachedPots      = new PotSlot[4];
        private readonly PotStateModel[] _cachedStates    = new PotStateModel[4];
        private readonly PlantData[]     _cachedPlantData = new PlantData[4];
        private readonly CryoSlot[]      _cachedCryoSlots = new CryoSlot[3];

        // ── State ──────────────────────────────────────────────────────
        private readonly bool[] _expandedPots = new bool[4];
        private bool _showingCryo;
        private bool _hudBodyExpanded = true;
        private Button _btnHudToggle;
        private Label _hudToggleChevron;
        private float _refreshTimer;
        private const float RefreshInterval = 0.5f;
        private IVisualElementScheduledItem _tooltipSchedule;

        private static Sprite _previewPlaceholderSprite;

        // ─────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument != null)
                _uiDocument.sortingOrder = 55;

            if (_previewPlaceholderSprite == null)
                _previewPlaceholderSprite = Resources.Load<Sprite>("icona_Placeholder");
        }

        private void OnEnable()
        {
            _potRegistry    = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            _cryoMachine    = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            _phSystem       = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            EnsurePotSystemConfig();

            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;

            PotEvents.OnPotStateChanged += HandlePotStateChanged;

            SetupUI();
            RefreshData();
        }

        private void OnDisable()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= HandleDayChanged;

            PotEvents.OnPotStateChanged -= HandlePotStateChanged;
        }

        private void Update()
        {
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                RefreshData();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────────────────────────

        private void HandleDayChanged(int day)
        {
            _phSystem = _phSystem ?? ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            RefreshData();
        }

        private void HandlePotStateChanged(PotSlot pot)
        {
            RefreshData();
            // Collapse expanded cards whose pot became empty
            for (int i = 0; i < 4; i++)
            {
                if (_expandedPots[i] && !(_cachedStates[i]?.HasPlant ?? false))
                {
                    _expandedPots[i] = false;
                    ApplyPotExpandState(i);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // UI setup
        // ─────────────────────────────────────────────────────────────

        private void SetupUI()
        {
            if (_uiDocument == null) return;

            var docRoot = _uiDocument.rootVisualElement;
            if (docRoot == null) return;

            if (_spFoundation != null && !docRoot.styleSheets.Contains(_spFoundation))
                docRoot.styleSheets.Add(_spFoundation);
            if (_spPanelBase != null && !docRoot.styleSheets.Contains(_spPanelBase))
                docRoot.styleSheets.Add(_spPanelBase);
            if (_panelUss != null && !docRoot.styleSheets.Contains(_panelUss))
                docRoot.styleSheets.Add(_panelUss);

            _hudRoot = docRoot.Q<VisualElement>("dome-hud-root");
            if (_hudRoot == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[DomeStatusHUD] dome-hud-root not found in UXML.");
                return;
            }

            _tabPots      = _hudRoot.Q<Button>("btn-tab-pots");
            _tabCryo      = _hudRoot.Q<Button>("btn-tab-cryo");
            _tabPotsLabel = _tabPots?.Q<Label>("lbl-tab-pots");
            _tabCryoLabel = _tabCryo?.Q<Label>("lbl-tab-cryo");
            _sectionPots  = _hudRoot.Q<VisualElement>("dome-hud-section-pots");
            _sectionCryo  = _hudRoot.Q<VisualElement>("dome-hud-section-cryo");
            _tooltip      = _hudRoot.Q<VisualElement>("dome-hud-tooltip");
            _tooltipLines = _hudRoot.Q<VisualElement>("dome-hud-tooltip-lines");

            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;

            var builderRef = _hudRoot.Q<VisualElement>("dome-hud-builder-reference");
            if (builderRef != null)
            {
                bool hideBuilder = Application.isPlaying && !_showBuilderReferenceDuringPlay;
                builderRef.style.display = hideBuilder ? DisplayStyle.None : DisplayStyle.Flex;
            }

            for (int i = 0; i < 4; i++)
            {
                _potCards[i]         = _hudRoot.Q<VisualElement>($"dome-pot-card-{i}");
                _potHeaders[i]       = _hudRoot.Q<VisualElement>($"dome-pot-header-{i}");
                _potChevrons[i]      = _hudRoot.Q<Label>($"dome-pot-chevron-{i}");
                _potBadges[i]        = _hudRoot.Q<Label>($"dome-pot-badge-{i}");
                _potPreviews[i]      = _hudRoot.Q<VisualElement>($"dome-pot-preview-{i}");
                _potNames[i]         = _hudRoot.Q<Label>($"dome-pot-name-{i}");
                _potSubs[i]          = _hudRoot.Q<Label>($"dome-pot-sub-{i}");
                _potCondRows[i]      = _hudRoot.Q<VisualElement>($"dome-pot-cond-row-{i}");
                _potCondDots[i]      = _hudRoot.Q<VisualElement>($"dome-pot-cond-dot-{i}");
                _potConds[i]         = _hudRoot.Q<Label>($"dome-pot-cond-{i}");
                _potWater[i]         = _hudRoot.Q<Label>($"dome-pot-water-{i}");
                _potLed[i]           = _hudRoot.Q<Label>($"dome-pot-led-{i}");
                _potExpandedAreas[i] = _hudRoot.Q<VisualElement>($"dome-pot-expanded-{i}");
                _potStatWater[i]     = _hudRoot.Q<Label>($"dome-pot-stat-water-{i}");
                _potStatFert[i]      = _hudRoot.Q<Label>($"dome-pot-stat-fert-{i}");
                _potStatLed[i]       = _hudRoot.Q<Label>($"dome-pot-stat-led-{i}");
                _potStatPh[i]        = _hudRoot.Q<Label>($"dome-pot-stat-ph-{i}");

                int idx = i;
                _potHeaders[i]?.AddToClassList(HudTooltipCursor.TooltipHostUssClass);
                _potHeaders[i]?.RegisterCallback<MouseEnterEvent>(_ => OnPotRowHover(idx));
                _potHeaders[i]?.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());
                _potHeaders[i]?.RegisterCallback<ClickEvent>(_ => TogglePotExpand(idx));
            }

            for (int i = 0; i < 3; i++)
            {
                _cryoRows[i]    = _hudRoot.Q<VisualElement>($"dome-cryo-row-{i}");
                _cryoIds[i]     = _hudRoot.Q<Label>($"dome-cryo-id-{i}");
                _cryoPlants[i]  = _hudRoot.Q<Label>($"dome-cryo-plant-{i}");
                _cryoDetails[i] = _hudRoot.Q<Label>($"dome-cryo-detail-{i}");

                int idx = i;
                _cryoRows[i]?.AddToClassList(HudTooltipCursor.TooltipHostUssClass);
                _cryoRows[i]?.RegisterCallback<MouseEnterEvent>(_ => OnCryoRowHover(idx));
                _cryoRows[i]?.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());
            }

            if (_tabPots != null)
            {
                _tabPots.clicked -= OnTabPotsClicked;
                _tabPots.clicked += OnTabPotsClicked;
            }

            if (_tabCryo != null)
            {
                _tabCryo.clicked -= OnTabCryoClicked;
                _tabCryo.clicked += OnTabCryoClicked;
            }

            _btnHudToggle = _hudRoot.Q<Button>("btn-dome-hud-toggle");
            _hudToggleChevron = _hudRoot.Q<Label>("dome-hud-toggle-chevron");
            if (_btnHudToggle != null)
            {
                _btnHudToggle.clicked -= ToggleHudBodyExpanded;
                _btnHudToggle.clicked += ToggleHudBodyExpanded;
            }

            // Collassa tutte le aree espanse e resetta i placeholder di authoring di card-0.
            // card-0 in UXML è visibile (expanded) per l'editing in UI Builder;
            // a runtime parte collassata e il testo/colore viene sovrascritto da RefreshPots.
            for (int i = 0; i < 4; i++)
            {
                if (_potExpandedAreas[i] != null)
                    _potExpandedAreas[i].style.display = DisplayStyle.None;
                if (_potCondRows[i] != null)
                    _potCondRows[i].style.display = DisplayStyle.None;
            }

            // SwitchTab prima, poi ApplyHudBodyExpandedState: se collassato, sovrascrive la visibilità sezioni
            SwitchTab(false);
            _hudBodyExpanded = _startHudExpanded;
            ApplyHudBodyExpandedState();
        }

        private void ToggleHudBodyExpanded()
        {
            _hudBodyExpanded = !_hudBodyExpanded;
            ApplyHudBodyExpandedState();
        }

        /// <summary>
        /// Collassa / espande le sezioni card POT e CRYO. La barra tab (con toggle) è sempre visibile.
        /// Le sezioni hanno inline style settato da SwitchTab, quindi gestiamo la visibilità direttamente
        /// in C# piuttosto che affidarci solo al selettore CSS (inline > USS).
        /// Chevron: ^ = espanso, v = collassato.
        /// </summary>
        private void ApplyHudBodyExpandedState()
        {
            if (_hudRoot == null) return;
            _hudRoot.EnableInClassList("dome-hud--collapsed", !_hudBodyExpanded);
            if (_hudToggleChevron != null)
                _hudToggleChevron.text = _hudBodyExpanded ? "^" : "v";

            if (!_hudBodyExpanded)
            {
                HideTooltip();
                if (_sectionPots != null) _sectionPots.style.display = DisplayStyle.None;
                if (_sectionCryo != null) _sectionCryo.style.display = DisplayStyle.None;
            }
            else
            {
                // Ripristina lo stato tab corrente
                SwitchTab(_showingCryo);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Per-pot expand / collapse
        // ─────────────────────────────────────────────────────────────

        private void TogglePotExpand(int idx)
        {
            if (!(_cachedStates[idx]?.HasPlant ?? false)) return;
            _expandedPots[idx] = !_expandedPots[idx];
            ApplyPotExpandState(idx);
        }

        private void ApplyPotExpandState(int idx)
        {
            bool hasPlant  = _cachedStates[idx]?.HasPlant ?? false;
            bool isExpanded = _expandedPots[idx] && hasPlant;

            if (_potChevrons[idx] != null)
                _potChevrons[idx].text = isExpanded ? "v" : ">";

            if (_potExpandedAreas[idx] != null)
                _potExpandedAreas[idx].style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ─────────────────────────────────────────────────────────────
        // Tab switching
        // ─────────────────────────────────────────────────────────────

        private void OnTabPotsClicked() => SwitchTab(false);
        private void OnTabCryoClicked() => SwitchTab(true);

        private void SwitchTab(bool cryo)
        {
            _showingCryo = cryo;
            if (_sectionPots != null)
                _sectionPots.style.display = cryo ? DisplayStyle.None : DisplayStyle.Flex;
            if (_sectionCryo != null)
                _sectionCryo.style.display = cryo ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tabPots != null)
                _tabPots.EnableInClassList("dome-tab-active", !cryo);
            if (_tabCryo != null)
                _tabCryo.EnableInClassList("dome-tab-active", cryo);

            HideTooltip();
        }

        // ─────────────────────────────────────────────────────────────
        // Refresh
        // ─────────────────────────────────────────────────────────────

        private void RefreshData()
        {
            if (_hudRoot == null) return;
            RefreshPots();
            RefreshCryo();
            UpdateTabCounters();
        }

        private void RefreshPots()
        {
            var pots = _potRegistry?.GetActivePotsSnapshot();

            for (int i = 0; i < 4; i++)
            {
                PotSlot pot         = (pots != null && i < pots.Count) ? pots[i] : null;
                PotStateModel state = pot?.PotActions?.GetCurrentState();
                PlantData plantData = state?.GetPlantData();

                _cachedPots[i]      = pot;
                _cachedStates[i]    = state;
                _cachedPlantData[i] = plantData;

                bool hasPlant  = state?.HasPlant ?? false;
                string potLabel = pot?.PotId ?? $"POT-00{i + 1}";

                // ── Badge ──
                if (_potBadges[i] != null)
                {
                    _potBadges[i].text = potLabel;
                    if (hasPlant)
                    {
                        _potBadges[i].RemoveFromClassList("dome-pot-badge--empty");
                        _potBadges[i].AddToClassList("dome-pot-badge--occupied");
                    }
                    else
                    {
                        _potBadges[i].RemoveFromClassList("dome-pot-badge--occupied");
                        _potBadges[i].AddToClassList("dome-pot-badge--empty");
                    }
                }

                // ── Card: outline per famiglia; sfondo neutro (niente tinta condizione); vuoto = celeste ──
                if (_potCards[i] != null)
                {
                    _potCards[i].RemoveFromClassList("dome-pot-card--empty");
                    _potCards[i].RemoveFromClassList("dome-pot-card--fam-pure");
                    _potCards[i].RemoveFromClassList("dome-pot-card--fam-evil");
                    _potCards[i].RemoveFromClassList("dome-pot-card--fam-wild");

                    if (hasPlant && state != null)
                    {
                        PlantFamily cardFamily = GetPlantFamilyForDisplay(plantData, state);
                        switch (cardFamily)
                        {
                            case PlantFamily.Pure:
                                _potCards[i].AddToClassList("dome-pot-card--fam-pure");
                                break;
                            case PlantFamily.Evil:
                                _potCards[i].AddToClassList("dome-pot-card--fam-evil");
                                break;
                            default:
                                _potCards[i].AddToClassList("dome-pot-card--fam-wild");
                                break;
                        }
                    }
                    else
                    {
                        _potCards[i].AddToClassList("dome-pot-card--empty");
                    }
                }

                // ── Preview sprite — usa il visual set della pianta (adultSprite = identità specie)
                //    NON la sprite dello SpriteRenderer di scena (che cambierebbe con lo stadio
                //    e al Seed mostrerebbe la sprite del seme, non la pianta).
                if (_potPreviews[i] != null)
                {
                    Sprite previewSprite = null;
                    if (hasPlant && plantData != null)
                    {
                        var vs = plantData.VisualSet;
                        if (vs != null)
                            previewSprite = vs.adultSprite ?? vs.floweringSprite;
                    }

                    if (previewSprite != null)
                    {
                        _potPreviews[i].style.backgroundImage = new StyleBackground(previewSprite);
                        _potPreviews[i].style.backgroundSize  = new BackgroundSize(BackgroundSizeType.Contain);
                    }
                    else if (hasPlant && _previewPlaceholderSprite != null)
                    {
                        _potPreviews[i].style.backgroundImage = new StyleBackground(_previewPlaceholderSprite);
                        _potPreviews[i].style.backgroundSize  = new BackgroundSize(BackgroundSizeType.Contain);
                    }
                    else
                    {
                        _potPreviews[i].style.backgroundImage = new StyleBackground(StyleKeyword.Null);
                    }
                }

                // ── Name (colore = famiglia, come outline / ref. allegato 2) ──
                if (_potNames[i] != null)
                {
                    if (hasPlant && state != null)
                    {
                        _potNames[i].text = !string.IsNullOrWhiteSpace(state.CustomPlantName)
                            ? state.CustomPlantName
                            : GetPlantDisplayName(plantData, state.PlantCode);
                        _potNames[i].style.color =
                            new StyleColor(GetFamilyColor(GetPlantFamilyForDisplay(plantData, state)));
                    }
                    else
                    {
                        _potNames[i].text = "VASO VUOTO";
                        _potNames[i].style.color = StyleKeyword.Null;
                    }
                }

                // ── Sub: Lvl • stadio • famiglia — grigio chiaro fisso (USS), non colore famiglia ──
                if (_potSubs[i] != null)
                {
                    if (hasPlant && state != null)
                    {
                        PlantFamily family  = GetPlantFamilyForDisplay(plantData, state);
                        string famLabel     = family.ToString().ToUpperInvariant();
                        _potSubs[i].text    = $"Lvl.{state.PlantLevel} • {PlantStageLabel(state.Stage)} • {famLabel}";
                        _potSubs[i].style.color = StyleKeyword.Null;
                    }
                    else
                    {
                        _potSubs[i].text = "Pronto per la piantagione";
                        _potSubs[i].style.color = StyleKeyword.Null;
                    }
                }

                // ── Condition (pallino + testo; allineato al tooltip: nome + %) ──
                if (_potCondRows[i] != null)
                    _potCondRows[i].style.display =
                        hasPlant && state != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasPlant && state != null)
                {
                    var condEnum = ConditionFromScore(state);
                    Color condCol = ConditionColor(condEnum);
                    if (_potCondDots[i] != null)
                        _potCondDots[i].style.backgroundColor = new StyleColor(condCol);
                    if (_potConds[i] != null)
                    {
                        string condStr = ConditionLabel(condEnum);
                        _potConds[i].text = $"{condStr.ToUpperInvariant()} ({state.ConditionScore}%)";
                        _potConds[i].style.color = new StyleColor(condCol);
                    }
                }
                else
                {
                    if (_potCondDots[i] != null)
                        _potCondDots[i].style.backgroundColor = StyleKeyword.Null;
                    if (_potConds[i] != null)
                    {
                        _potConds[i].text = "";
                        _potConds[i].style.color = StyleKeyword.Null;
                    }
                }

                // ── Water indicator ──
                if (_potWater[i] != null)
                {
                    if (hasPlant && state != null)
                    {
                        bool waterOn = state.WateringSystemOn;
                        _potWater[i].text = waterOn ? "●" : "○";
                        _potWater[i].style.color = waterOn
                            ? new StyleColor(new Color(0.50f, 1.00f, 0.48f))
                            : new StyleColor(new Color(0.55f, 0.55f, 0.55f));
                    }
                    else
                    {
                        _potWater[i].text = "○";
                        _potWater[i].style.color = new StyleColor(new Color(0.45f, 0.45f, 0.45f));
                    }
                }

                // ── LED indicator ──
                if (_potLed[i] != null)
                {
                    if (hasPlant && state != null)
                    {
                        switch (state.LedSystemState)
                        {
                            case LedSystemState.Blue:
                                _potLed[i].text = "●";
                                _potLed[i].style.color = new StyleColor(new Color(0.36f, 0.71f, 0.89f));
                                break;
                            case LedSystemState.Red:
                                _potLed[i].text = "●";
                                _potLed[i].style.color = new StyleColor(new Color(0.83f, 0.37f, 0.37f));
                                break;
                            default:
                                _potLed[i].text = "○";
                                _potLed[i].style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
                                break;
                        }
                    }
                    else
                    {
                        _potLed[i].text = "○";
                        _potLed[i].style.color = new StyleColor(new Color(0.45f, 0.45f, 0.45f));
                    }
                }

                // ── Expanded area stats (valori 0 / LED off = rosso; label/outline da USS per tipo) ──
                if (hasPlant && state != null)
                {
                    int hydPct = GetHydrationPercent(state);
                    PlantData careData = LabHybridGameplayModifiers.ResolvePlantDataForCareRequirements(state, plantData) ?? plantData;

                    if (_potStatWater[i] != null)
                    {
                        _potStatWater[i].text = $"{hydPct}%";
                        StageRequirements reqW = careData?.GetStageRequirements((PlantStage)state.Stage);
                        if (hydPct <= 0)
                            _potStatWater[i].style.color = new StyleColor(TipRed);
                        else if (reqW != null)
                            _potStatWater[i].style.color = new StyleColor(RangeColor(hydPct, reqW.hydrationMin, reqW.hydrationMax));
                        else
                            _potStatWater[i].style.color = new StyleColor(TipMuted);
                    }

                    if (_potStatFert[i] != null)
                    {
                        var stageEnum = (PlantStage)state.Stage;
                        StageRequirements reqF = careData?.GetStageRequirements(stageEnum);
                        if (IsFertilizerOptionalForStage(stageEnum))
                        {
                            _potStatFert[i].text = "NON NECESSARIO";
                            _potStatFert[i].style.color = new StyleColor(TipMuted);
                        }
                        else
                        {
                            _potStatFert[i].text = $"{state.FertilizerLevel}%";
                            if (state.FertilizerLevel <= 0)
                                _potStatFert[i].style.color = new StyleColor(TipRed);
                            else if (reqF != null)
                                _potStatFert[i].style.color = new StyleColor(RangeColor(state.FertilizerLevel, reqF.fertilizerMin, reqF.fertilizerMax));
                            else
                                _potStatFert[i].style.color = new StyleColor(TipMuted);
                        }
                    }

                    if (_potStatLed[i] != null)
                    {
                        string ledText = FormatLedStatText(state.LedSystemState);
                        Color  ledColor;
                        switch (state.LedSystemState)
                        {
                            case LedSystemState.Blue:
                                ledColor = new Color(0.36f, 0.71f, 0.89f);
                                break;
                            case LedSystemState.Red:
                                ledColor = new Color(0.83f, 0.37f, 0.37f);
                                break;
                            default:
                                ledColor = TipRed;
                                break;
                        }
                        _potStatLed[i].text = ledText;
                        _potStatLed[i].style.color = new StyleColor(ledColor);
                    }

                    if (_potStatPh[i] != null && plantData != null)
                    {
                        float shownDrift = ComputeShownDailyPhDrift(state, plantData);
                        _potStatPh[i].text = $"pH {FormatPhDrift(shownDrift)}/g";
                        _potStatPh[i].style.color = _phSystem != null
                            ? new StyleColor(PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState()))
                            : new StyleColor(PhGradientDisplayColors.GetColorFromScale(7f));
                    }
                    else if (_potStatPh[i] != null)
                        _potStatPh[i].text = "";
                }
                else
                {
                    if (_potStatWater[i] != null)
                    {
                        _potStatWater[i].text = "—";
                        _potStatWater[i].style.color = StyleKeyword.Null;
                    }
                    if (_potStatFert[i] != null)
                    {
                        _potStatFert[i].text = "—";
                        _potStatFert[i].style.color = StyleKeyword.Null;
                    }
                    if (_potStatLed[i] != null)
                    {
                        _potStatLed[i].text = "—";
                        _potStatLed[i].style.color = StyleKeyword.Null;
                    }
                    if (_potStatPh[i] != null)
                    {
                        _potStatPh[i].text = "";
                        _potStatPh[i].style.color = StyleKeyword.Null;
                    }
                }

                // ── Sync expand state visually ──
                ApplyPotExpandState(i);
            }
        }

        private void RefreshCryo()
        {
            IReadOnlyList<CryoSlot> slots = _cryoMachine?.GetPassiveSlotsSnapshot();
            List<PhSystem.CryoPassiveModifier> phMods = _phSystem?.GetCryoPassiveModifiers();

            for (int i = 0; i < 3; i++)
            {
                CryoSlot slot = (slots != null && i < slots.Count) ? slots[i] : null;
                _cachedCryoSlots[i] = slot;

                bool occupied         = slot?.IsOccupied ?? false;
                CryoPlantPayload payload = slot?.Payload;

                if (_cryoIds[i] != null)
                    _cryoIds[i].text = slot?.SlotId ?? $"CRYO-0{i + 1}";

                if (_cryoPlants[i] != null)
                    _cryoPlants[i].text = occupied && payload != null
                        ? GetCryoPlantDisplayName(payload)
                        : "—";

                if (_cryoDetails[i] != null)
                {
                    if (occupied && payload != null)
                    {
                        string driftStr = "—";
                        if (phMods != null && slot != null)
                        {
                            foreach (var mod in phMods)
                            {
                                if (mod.SlotId == slot.SlotId)
                                {
                                    driftStr = FormatPhDrift(mod.DailyDrift) + "/g";
                                    break;
                                }
                            }
                        }

                        string powerLabel = !string.IsNullOrWhiteSpace(payload.PassivePowerLabel)
                            ? TruncateString(payload.PassivePowerLabel, 30)
                            : "—";
                        _cryoDetails[i].text = $"pH {driftStr}  ·  {powerLabel}";
                    }
                    else
                    {
                        _cryoDetails[i].text = "slot disponibile";
                    }
                }
            }
        }

        private void UpdateTabCounters()
        {
            int potsOccupied = 0;
            for (int i = 0; i < 4; i++)
                if (_cachedStates[i]?.HasPlant ?? false)
                    potsOccupied++;

            var slots = _cryoMachine?.GetPassiveSlotsSnapshot();
            int cryoOccupied = 0;
            if (slots != null)
                foreach (var s in slots)
                    if (s?.IsOccupied ?? false) cryoOccupied++;

            if (_tabPotsLabel != null)
                _tabPotsLabel.text = $"POT [{potsOccupied}/4]";
            else if (_tabPots != null)
                _tabPots.text = $"POT [{potsOccupied}/4]";
            if (_tabCryoLabel != null)
                _tabCryoLabel.text = $"CRYO [{cryoOccupied}/3]";
            else if (_tabCryo != null)
                _tabCryo.text = $"CRYO [{cryoOccupied}/3]";
        }

        // ─────────────────────────────────────────────────────────────
        // Tooltip (delayed ~200ms on hover)
        // ─────────────────────────────────────────────────────────────

        private void OnPotRowHover(int idx)
        {
            _tooltipSchedule?.Pause();
            if (_tooltip == null || idx >= 4) return;

            var state = _cachedStates[idx];
            if (state == null || !state.HasPlant)
            {
                HideTooltip();
                return;
            }

            int capturedIdx = idx;
            _tooltipSchedule = _hudRoot.schedule.Execute(() =>
            {
                var s  = _cachedStates[capturedIdx];
                var pd = _cachedPlantData[capturedIdx];
                if (s == null || !s.HasPlant || _tooltip == null) return;
                SetTooltipLines(BuildPotTooltipLines(s, pd));
                _tooltip.style.display = DisplayStyle.Flex;
            }).StartingIn(200);
        }

        private void OnCryoRowHover(int idx)
        {
            _tooltipSchedule?.Pause();
            if (_tooltip == null || idx >= 3) return;

            var slot = _cachedCryoSlots[idx];
            if (slot == null || !slot.IsOccupied || slot.Payload == null)
            {
                HideTooltip();
                return;
            }

            int capturedIdx = idx;
            _tooltipSchedule = _hudRoot.schedule.Execute(() =>
            {
                var s = _cachedCryoSlots[capturedIdx];
                if (s == null || !s.IsOccupied || s.Payload == null || _tooltip == null) return;
                SetTooltipLines(BuildCryoTooltipLines(s));
                _tooltip.style.display = DisplayStyle.Flex;
            }).StartingIn(200);
        }

        private void SetTooltipLines(System.Collections.Generic.List<TooltipLine> lines)
        {
            if (_tooltipLines == null) return;
            _tooltipLines.Clear();

            int contentIndex = 0;
            string botZone = null; // "poteri" | "subiti" — stile reference 2 sui blocchi botanici

            foreach (var line in lines)
            {
                if (line.IsSep)
                {
                    var rule = new VisualElement();
                    rule.AddToClassList("dome-hud-tooltip-sep");
                    _tooltipLines.Add(rule);
                    continue;
                }

                var lbl = new Label(line.Text);
                lbl.AddToClassList("dome-hud-tooltip-line");
                if (line.Bold)
                    lbl.AddToClassList("dome-hud-tooltip-line--bold");
                lbl.style.color = new StyleColor(line.Color);

                string t = line.Text ?? string.Empty;

                if (contentIndex == 0)
                    lbl.AddToClassList("dome-hud-tooltip-line--title");
                else if (contentIndex == 1)
                    lbl.AddToClassList("dome-hud-tooltip-line--subtitle");

                if (line.Bold && IsTooltipSectionHeader(t))
                    lbl.AddToClassList("dome-hud-tooltip-line--section");

                if (IsTooltipIndentedKeyValueLine(t))
                    lbl.AddToClassList("dome-hud-tooltip-line--kv");

                if (t.Contains("Poteri") && t.Contains("──"))
                {
                    lbl.AddToClassList("dome-hud-tooltip-line--poteri-h");
                    botZone = "poteri";
                }
                else if (t.Contains("Subiti") && t.Contains("──"))
                {
                    lbl.AddToClassList("dome-hud-tooltip-line--subiti-h");
                    botZone = "subiti";
                }
                else
                {
                    if (botZone == "poteri")
                        lbl.AddToClassList("dome-hud-tooltip-line--in-poteri");
                    else if (botZone == "subiti")
                        lbl.AddToClassList("dome-hud-tooltip-line--in-subiti");
                }

                _tooltipLines.Add(lbl);
                contentIndex++;
            }
        }

        private static bool IsTooltipSectionHeader(string t)
        {
            if (string.IsNullOrEmpty(t)) return false;
            string u = t.TrimStart();
            return u.StartsWith("REQUISITI", StringComparison.Ordinal)
                || u.StartsWith("STATO ATTUALE", StringComparison.Ordinal)
                || u.StartsWith("POTERE PASSIVO", StringComparison.Ordinal)
                || u.StartsWith("EFFETTO pH", StringComparison.Ordinal);
        }

        private static bool IsTooltipIndentedKeyValueLine(string t)
        {
            if (string.IsNullOrEmpty(t) || t.IndexOf(':') < 0) return false;
            return t.StartsWith("  ", StringComparison.Ordinal) || t.StartsWith("    ", StringComparison.Ordinal);
        }

        private void HideTooltip()
        {
            _tooltipSchedule?.Pause();
            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;
        }

        // ── TooltipLine struct ───────────────────────────────────────
        private struct TooltipLine
        {
            public string Text;
            public Color  Color;
            public bool   Bold;
            public bool   IsSep;

            public TooltipLine(string text, Color color, bool bold = false)
            {
                Text = text; Color = color; Bold = bold; IsSep = false;
            }

            public static TooltipLine Sep() =>
                new TooltipLine { Text = "────────────────────────────────", IsSep = true };
        }

        // ── Palette shortcuts (tooltip allineato a TopBar ph-tooltip CRT) ─
        private static readonly Color TipGreen  = new Color(0.498f, 1f, 0.478f);
        private static readonly Color TipYellow = new Color(0.902f, 0.788f, 0.435f);
        private static readonly Color TipRed    = new Color(0.827f, 0.373f, 0.373f);
        private static readonly Color TipMuted  = new Color(0.753f, 0.784f, 0.773f);
        private static readonly Color TipPhCyan = new Color(80f / 255f, 200f / 255f, 220f / 255f);
        private static readonly Color TipPhSection = new Color(200f / 255f, 203f / 255f, 200f / 255f);

        private static Color RangeColor(float value, float min, float max)
        {
            if (value >= min && value <= max) return TipGreen;
            float margin = Mathf.Max((max - min) * 0.25f, 8f);
            if (value >= min - margin && value <= max + margin) return TipYellow;
            return TipRed;
        }

        /// <summary>Allineato a GrowthPointsCalculator (LED off): stress ottimale 20–80%.</summary>
        private const int LightStressOkMinHud = 20;
        private const int LightStressOkMaxHud = 80;

        private static bool IsFertilizerOptionalForStage(PlantStage stage) =>
            stage == PlantStage.Seed || stage == PlantStage.Sprout;

        private int GetLightStressPercent(PotStateModel state)
        {
            if (state == null) return 0;
            EnsurePotSystemConfig();
            int consecutiveDays = state.GetConsecutiveLedDays();
            int maxDays = _potSystemConfig != null ? _potSystemConfig.MaxDaysForFullStress : 5;
            if (maxDays <= 0) maxDays = 5;
            return Mathf.RoundToInt(Mathf.Clamp01((float)consecutiveDays / maxDays) * 100f);
        }

        private static Color LightStressColor(int stressPercent) =>
            RangeColor(stressPercent, LightStressOkMinHud, LightStressOkMaxHud);

        /// <summary>Stesso caricamento di <see cref="PotActions"/> / PlantCard: Resources/Configs/PotSystemConfig.</summary>
        private void EnsurePotSystemConfig()
        {
            if (_potSystemConfig != null) return;
            _potSystemConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");
            if (_potSystemConfig == null)
            {
                PotSystemConfig[] all = Resources.LoadAll<PotSystemConfig>("Configs");
                if (all != null && all.Length > 0)
                {
                    foreach (PotSystemConfig cfg in all)
                    {
                        if (cfg != null && cfg.MaxHydration != 4)
                        {
                            _potSystemConfig = cfg;
                            break;
                        }
                    }
                    if (_potSystemConfig == null)
                        _potSystemConfig = all[0];
                }
            }
        }

        private int GetHydrationPercent(PotStateModel state)
        {
            if (state == null) return 0;
            int maxH = _potSystemConfig != null ? _potSystemConfig.MaxHydration : 10;
            return PlantCardCalculators.CalculateHydrationPercent(state.Hydration, maxH);
        }

        private static string FormatLedStatText(LedSystemState s)
        {
            switch (s)
            {
                case LedSystemState.Blue: return "BLUE";
                case LedSystemState.Red:  return "RED";
                default:                   return "OFF";
            }
        }

        private static Color LedColor(LedSystemState current, LedType? required)
        {
            if (!required.HasValue) return TipGreen;
            if (current == LedSystemState.Off) return TipRed;
            LedType currentType = current == LedSystemState.Blue ? LedType.Blue : LedType.Red;
            return currentType == required.Value ? TipGreen : TipYellow;
        }

        private System.Collections.Generic.List<TooltipLine> BuildPotTooltipLines(PotStateModel state, PlantData plantData)
        {
            var lines = new System.Collections.Generic.List<TooltipLine>();

            string name  = GetPotPlantDisplayName(state, plantData);
            string stage = PlantStageLabel(state.Stage);
            var condEnum = ConditionFromScore(state);
            string cond  = ConditionLabel(condEnum);

            lines.Add(new TooltipLine($"■ {name}  Lvl {state.PlantLevel}", TipPhCyan, bold: true));
            lines.Add(new TooltipLine($"  {state.PotId} · {stage} · Giorno {state.DaysInCurrentStage}", TipMuted));

            var stageEnum = (PlantStage)state.Stage;
            PlantData careForReq = LabHybridGameplayModifiers.ResolvePlantDataForCareRequirements(state, plantData) ?? plantData;
            StageRequirements req = careForReq?.GetStageRequirements(stageEnum);
            int hydPct = GetHydrationPercent(state);
            string ledStat = FormatLedStatText(state.LedSystemState);
            int lightStressPct = GetLightStressPercent(state);

            if (req != null)
            {
                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("REQUISITI E AVANZAMENTO", TipPhSection, bold: true));
                lines.Add(new TooltipLine($"  Idratazione   : {req.hydrationMin}–{req.hydrationMax}%", TipMuted));
                LedType? reqLed = req.GetRequiredLed();
                lines.Add(new TooltipLine($"  LED           : {(reqLed.HasValue ? reqLed.Value.ToString() : "nessuno")}", TipMuted));
                string fertReqLine = $"  Fertilizzante : {req.fertilizerMin}–{req.fertilizerMax}%";
                if (IsFertilizerOptionalForStage(stageEnum))
                    fertReqLine += " (opzionale Seme/Germoglio)";
                lines.Add(new TooltipLine(fertReqLine, TipMuted));
                lines.Add(new TooltipLine($"  Durata        : {req.durationDays} giorni", TipMuted));

                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("STATO ATTUALE", TipPhSection, bold: true));
                lines.Add(new TooltipLine($"  Idratazione   : {hydPct}%",
                    RangeColor(hydPct, req.hydrationMin, req.hydrationMax)));
                if (IsFertilizerOptionalForStage(stageEnum))
                {
                    lines.Add(new TooltipLine(
                        "  Fertilizzante : non necessario in questo stadio",
                        TipMuted));
                }
                else
                {
                    lines.Add(new TooltipLine(
                        $"  Fertilizzante : {state.FertilizerLevel}% (necessario)",
                        RangeColor(state.FertilizerLevel, req.fertilizerMin, req.fertilizerMax)));
                }

                lines.Add(new TooltipLine($"  LED           : {ledStat}",
                    LedColor(state.LedSystemState, req.GetRequiredLed())));
                lines.Add(new TooltipLine($"  Light stress  : {lightStressPct}% (20–80% ideale)",
                    LightStressColor(lightStressPct)));
            }
            else
            {
                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("STATO ATTUALE", TipPhSection, bold: true));
                lines.Add(new TooltipLine($"  Idratazione   : {hydPct}%", TipMuted));
                if (IsFertilizerOptionalForStage(stageEnum))
                {
                    lines.Add(new TooltipLine(
                        "  Fertilizzante : non necessario in questo stadio",
                        TipMuted));
                }
                else
                    lines.Add(new TooltipLine($"  Fertilizzante : {state.FertilizerLevel}%", TipMuted));
                lines.Add(new TooltipLine($"  LED           : {ledStat}", TipMuted));
                lines.Add(new TooltipLine($"  Light stress  : {lightStressPct}% (20–80% ideale)",
                    LightStressColor(lightStressPct)));
            }

            lines.Add(TooltipLine.Sep());
            lines.Add(new TooltipLine("CONDIZIONE", TipPhSection, bold: true));
            lines.Add(new TooltipLine($"  {cond.ToUpperInvariant()} ({state.ConditionScore}%)  {PlantConditionSystem.GetForecastSymbol((ForecastDirection)state.ForecastDirection)}",
                ConditionColor(condEnum), bold: true));
            string effectHint = ConditionEffectHint(condEnum);
            if (!string.IsNullOrEmpty(effectHint))
                lines.Add(new TooltipLine($"  ↳ {effectHint}", TipMuted));

            // Top 3 fattori that are pushing condition
            var drivers = BuildConditionDrivers(state, plantData);
            if (drivers.Count > 0)
            {
                lines.Add(new TooltipLine("  Fattori:", TipMuted));
                foreach (var (driverText, driverCol) in drivers)
                    lines.Add(new TooltipLine($"    · {driverText}", driverCol));
            }
            lines.Add(new TooltipLine($"  Giorni ottimali: {state.DaysConsecutiveOptimal}",
                state.DaysConsecutiveOptimal > 0 ? TipGreen : TipMuted));

            if (state.MoldRiskLevel > 0)
            {
                lines.Add(TooltipLine.Sep());
                Color moldCol = state.MoldRiskLevel >= 3 ? TipRed : TipYellow;
                lines.Add(new TooltipLine($"  ⚠ Rischio muffa: livello {state.MoldRiskLevel}", moldCol, bold: true));
            }

            if (state.IsInfested)
            {
                if (state.MoldRiskLevel == 0) lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("  ✗ INFESTATA DA MUFFE", TipRed, bold: true));
            }

            lines.Add(TooltipLine.Sep());
            var snap = BotanicalRosterSnapshot.FromServices(_phSystem);
            var botanicalLines = new List<BotanicalHudTooltipLine>();
            BotanicalPowerFacade.AppendDomeHudTooltipLines(botanicalLines, state, plantData, snap);
            for (int i = 0; i < botanicalLines.Count; i++)
            {
                var bl = botanicalLines[i];
                lines.Add(new TooltipLine(bl.Text, bl.Color, bl.Bold));
            }

            return lines;
        }

        private System.Collections.Generic.List<TooltipLine> BuildCryoTooltipLines(CryoSlot slot)
        {
            var payload = slot.Payload;
            var lines   = new System.Collections.Generic.List<TooltipLine>();

            lines.Add(new TooltipLine($"❄  {GetCryoPlantDisplayName(payload)}  Lvl {payload.PlantLevel}", TipPhCyan, bold: true));
            lines.Add(new TooltipLine($"   {slot.SlotId}", TipMuted));

            if (!string.IsNullOrWhiteSpace(payload.PassivePowerLabel))
            {
                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("POTERE PASSIVO", TipPhSection, bold: true));
                lines.Add(new TooltipLine($"   {payload.PassivePowerLabel}", TipMuted));
            }

            var phMods = _phSystem?.GetCryoPassiveModifiers();
            if (phMods != null)
            {
                foreach (var mod in phMods)
                {
                    if (mod.SlotId == slot.SlotId)
                    {
                        lines.Add(TooltipLine.Sep());
                        lines.Add(new TooltipLine("EFFETTO pH", TipPhCyan, bold: true));
                        float drift    = mod.DailyDrift;
                        Color driftCol = Mathf.Abs(drift) < 0.01f
                            ? TipMuted
                            : (_phSystem != null
                                ? PhGradientDisplayColors.GetColorForPhBand(_phSystem.EvaluateState())
                                : PhGradientDisplayColors.GetColorFromDrift(drift));
                        lines.Add(new TooltipLine($"   Drift/giorno: {FormatPhDrift(drift)}", driftCol));
                        if (Mathf.Abs(mod.PhCap) > 0.01f)
                            lines.Add(new TooltipLine($"   Cap pH: {mod.PhCap:F1}", TipMuted));
                        break;
                    }
                }
            }

            return lines;
        }

        // ─────────────────────────────────────────────────────────────
        // Static helpers — display names / labels
        // ─────────────────────────────────────────────────────────────

        private static string GetPlantDisplayName(PlantData plantData, string plantCode)
        {
            if (string.IsNullOrEmpty(plantCode)) return "—";
            if (plantData != null)
            {
                switch (plantData.PlantCode)
                {
                    case "PLT-STD-001":  return "Ferric Fern";
                    case "PLT-PURE-001": return "Arctic Hask";
                    case "PLT-EVIL-001": return "Glasscap Fungus";
                    default:             return plantData.name.Replace("PLT-", "").Replace("-", " ");
                }
            }
            return plantCode.Replace("PLT-", "").Replace("-", " ");
        }

        private static string GetCryoPlantDisplayName(CryoPlantPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.CustomPlantName)) return payload.CustomPlantName;
            var pd = PlantDatabase.Instance?.GetPlantDataByCode(payload.PlantCode);
            return GetPlantDisplayName(pd, payload.PlantCode);
        }

        private static string GetPotPlantDisplayName(PotStateModel state, PlantData plantData)
        {
            if (state == null) return "—";
            if (!string.IsNullOrWhiteSpace(state.CustomPlantName))
                return state.CustomPlantName;
            return GetPlantDisplayName(plantData, state.PlantCode);
        }

        private static string PlantStageLabel(int stage) => stage switch
        {
            0 => "Vuoto",
            1 => "Seme",
            2 => "Germoglio",
            3 => "Crescita",
            4 => "Fioritura",
            5 => "Raccolta",
            6 => "Riposo",
            _ => $"Stadio {stage}"
        };

        // Derives PlantCondition from live ConditionScore using simulation thresholds (80/40/20).
        // Morta is irreversible and set explicitly — trust stored ConditionLabel only for that.
        private static PlantCondition ConditionFromScore(PotStateModel state)
        {
            if (state == null) return PlantCondition.Sana;
            if ((PlantCondition)state.ConditionLabel == PlantCondition.Morta) return PlantCondition.Morta;
            int score = state.ConditionScore;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdRigogliosa) return PlantCondition.Rigogliosa;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdSana)       return PlantCondition.Sana;
            if (score >= DifficultyCalibrationConfig.ConditionThresholdAppassita)  return PlantCondition.Appassita;
            return PlantCondition.Critica;
        }

        // Uses the enum derived from live score — avoids stale ConditionLabel (updated only at day-end).
        private static string ConditionLabel(PlantCondition cond, bool overwatering = false)
            => PlantConditionSystem.GetConditionName(cond, overwatering);

        private static Color ConditionColor(PlantCondition cond) => cond switch
        {
            PlantCondition.Rigogliosa => new Color(0.22f, 0.90f, 0.22f),
            PlantCondition.Sana       => new Color(0.498f, 1f, 0.478f),
            PlantCondition.Appassita  => new Color(0.902f, 0.788f, 0.435f),
            PlantCondition.Critica    => new Color(0.827f, 0.373f, 0.373f),
            PlantCondition.Morta      => new Color(0.50f, 0.10f, 0.10f),
            _                         => new Color(0.498f, 1f, 0.478f),
        };

        private static string ConditionEffectHint(PlantCondition cond) => cond switch
        {
            PlantCondition.Rigogliosa => "Crescita +20% · Produzione +15%",
            PlantCondition.Sana       => "Crescita normale",
            PlantCondition.Appassita  => "Crescita -30% · Avanzamento bloccato",
            PlantCondition.Critica    => "Avanzamento bloccato · rischio morte",
            PlantCondition.Morta      => "Nessuna crescita — esegui UPROOT",
            _                         => string.Empty,
        };

        // Returns up to 3 condition drivers (text + color) derived from existing state data.
        private List<(string text, Color col)> BuildConditionDrivers(PotStateModel state, PlantData plantData)
        {
            var drivers = new List<(string, Color)>();
            if (state == null) return drivers;

            var stageEnum = (PlantStage)state.Stage;
            PlantData careData = LabHybridGameplayModifiers.ResolvePlantDataForCareRequirements(state, plantData) ?? plantData;
            StageRequirements req = careData?.GetStageRequirements(stageEnum);

            // Hydration
            int hydPct = GetHydrationPercent(state);
            if (req != null)
            {
                if (hydPct < req.hydrationMin)
                    drivers.Add(($"Idratazione bassa ({hydPct}% / min {req.hydrationMin}%)", TipRed));
                else if (hydPct > req.hydrationMax)
                    drivers.Add(($"Sovra-irrigazione ({hydPct}% / max {req.hydrationMax}%)", TipYellow));
            }

            // Fertilizer
            if (!IsFertilizerOptionalForStage(stageEnum) && req != null)
            {
                if (state.FertilizerLevel < req.fertilizerMin)
                    drivers.Add(($"Fertilizzante basso ({state.FertilizerLevel}% / min {req.fertilizerMin}%)", TipRed));
                else if (state.FertilizerLevel > req.fertilizerMax)
                    drivers.Add(($"Fertilizzante in eccesso ({state.FertilizerLevel}%)", TipYellow));
            }

            // Light stress
            int lightStress = GetLightStressPercent(state);
            if (lightStress > 80)
                drivers.Add(($"Light stress critico ({lightStress}%)", TipRed));
            else if (lightStress > 50)
                drivers.Add(($"Light stress elevato ({lightStress}%)", TipYellow));

            // Mold
            if (state.IsInfested)
                drivers.Add(("INFESTATA DA MUFFE — esegui PRUNE", TipRed));
            else if (state.MoldRiskLevel >= 3)
                drivers.Add(($"Rischio muffa critico (liv. {state.MoldRiskLevel})", TipRed));
            else if (state.MoldRiskLevel >= 1)
                drivers.Add(($"Rischio muffa (liv. {state.MoldRiskLevel})", TipYellow));

            // Neglect streak
            if (state.DaysNeglectedStreak >= 3)
                drivers.Add(($"Vaso trascurato da {state.DaysNeglectedStreak} giorni", TipYellow));

            // Return top 3 only
            if (drivers.Count > 3) drivers = drivers.GetRange(0, 3);
            return drivers;
        }

        private static string FormatPhDrift(float drift)
        {
            if (Mathf.Abs(drift) < 0.01f) return "0";
            return drift > 0 ? $"+{drift:F1}" : $"{drift:F1}";
        }

        private static float ComputeShownDailyPhDrift(PotStateModel state, PlantData plantData)
        {
            if (state == null || plantData == null) return 0f;
            float drift = plantData.GetDailyPhDrift();
            if (HasArcticPurificationActive(state, plantData))
                drift += 5f;
            return LabHybridGameplayModifiers.ScaleDailyPhDrift(drift, state);
        }

        private static bool HasArcticPurificationActive(PotStateModel state, PlantData plantData)
        {
            if (state == null && plantData == null) return false;
            if (BotanicalPlantCodes.IsArcticHask(plantData != null ? plantData.PlantCode : state?.PlantCode))
                return true;
            string active = !string.IsNullOrWhiteSpace(state?.ActivePowerLabel)
                ? state.ActivePowerLabel
                : plantData?.ActivePower;
            return !string.IsNullOrWhiteSpace(active) &&
                   active.IndexOf("Arctic Purification", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string TruncateString(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxLen) return s;
            return s.Substring(0, maxLen - 3) + "…";
        }

        // ─────────────────────────────────────────────────────────────
        // Static helpers — family (aligned with PlantCardV3 logic)
        // ─────────────────────────────────────────────────────────────

        private static PlantFamily GetPlantFamilyForDisplay(PlantData plantData, PotStateModel state)
        {
            if (plantData != null) return plantData.Family;

            string familyMetadata = state?.PlantFamilyMetadata;
            if (!string.IsNullOrWhiteSpace(familyMetadata))
            {
                if (familyMetadata.Equals("PURE", System.StringComparison.OrdinalIgnoreCase))
                    return PlantFamily.Pure;
                if (familyMetadata.Equals("EVIL", System.StringComparison.OrdinalIgnoreCase))
                    return PlantFamily.Evil;
            }

            return PlantFamily.Standard;
        }

        // Standard maps to WILD in the visual design (#E6C96F = rgb(230,201,111)).
        private static Color GetFamilyColor(PlantFamily family) => family switch
        {
            PlantFamily.Pure     => new Color(0.498f, 1f, 0.478f, 1f),      // #7FFF7A verde LED
            PlantFamily.Evil     => new Color(0.827f, 0.373f, 0.373f, 1f),  // #D35F5F rosso
            PlantFamily.Standard => new Color(0.902f, 0.788f, 0.435f, 1f),  // #E6C96F giallo/gold
            _                    => new Color(0.72f,  0.72f,  0.72f,  1f),
        };
    }
}
