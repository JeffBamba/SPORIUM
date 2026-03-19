using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.DomeStatusHUD
{
    /// <summary>
    /// Controller UIToolkit per l'HUD unificato Dome Status (pots attivi + cryo slot).
    /// Si espande orizzontalmente verso sinistra. Sempre visibile.
    /// Auto-apre quando almeno un pot ha una pianta; l'utente può forzare il collasso.
    /// sortingOrder = 55 (tra TopBar/Bottom 50 e Foundation 60).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DomeStatusHUDController : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private StyleSheet _spFoundation;
        [SerializeField] private StyleSheet _spPanelBase;
        [SerializeField] private StyleSheet _panelUss;

        // ── Services ──────────────────────────────────────────────────
        private DomePotRegistry _potRegistry;
        private CryoMachineController _cryoMachine;
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;

        // ── Root elements ──────────────────────────────────────────────
        private VisualElement _hudRoot;
        private Button _toggleBtn;
        private Button _tabPots;
        private Button _tabCryo;
        private VisualElement _sectionPots;
        private VisualElement _sectionCryo;
        private VisualElement _tooltip;
        private VisualElement _tooltipLines;

        // ── Per-pot row elements ───────────────────────────────────────
        private readonly VisualElement[] _potRows = new VisualElement[4];
        private readonly VisualElement[] _potPreviews = new VisualElement[4];
        private readonly Label[] _potNames = new Label[4];
        private readonly Label[] _potSubs = new Label[4];
        private readonly Label[] _potConds = new Label[4];
        private readonly Label[] _potWater = new Label[4];
        private readonly Label[] _potLed = new Label[4];

        // ── Per-cryo row elements ──────────────────────────────────────
        private readonly VisualElement[] _cryoRows = new VisualElement[3];
        private readonly Label[] _cryoIds = new Label[3];
        private readonly Label[] _cryoPlants = new Label[3];
        private readonly Label[] _cryoDetails = new Label[3];

        // ── Data caches (for tooltip) ──────────────────────────────────
        private readonly PotSlot[] _cachedPots = new PotSlot[4];
        private readonly PotStateModel[] _cachedStates = new PotStateModel[4];
        private readonly PlantData[] _cachedPlantData = new PlantData[4];
        private readonly CryoSlot[] _cachedCryoSlots = new CryoSlot[3];

        // ── State ──────────────────────────────────────────────────────
        private bool _expanded;
        private bool _userForcedCollapsed; // user explicitly closed → block auto-open
        private bool _userForcedExpanded;  // user explicitly opened → block auto-close on empty
        private bool _showingCryo;
        private float _refreshTimer;
        private const float RefreshInterval = 0.5f;

        // ─────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument != null)
                _uiDocument.sortingOrder = 55;
        }

        private void OnEnable()
        {
            _potRegistry   = ServiceContainer.Instance?.Get<DomePotRegistry>(suppressWarning: true);
            _cryoMachine   = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            _phSystem      = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);

            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;

            PotEvents.OnPotStateChanged += HandlePotStateChanged;

            SetupUI();
            Refresh();
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
                // Timer only refreshes displayed data — does NOT touch expand state.
                // Auto-expand logic runs only on real game events (OnEnable, pot changes, day changed).
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
            // Game state changed: clear the "user forced open" guard so auto-logic can recheck.
            _userForcedExpanded = false;
            Refresh();
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

            _toggleBtn    = _hudRoot.Q<Button>("dome-hud-toggle-btn");
            _tabPots      = _hudRoot.Q<Button>("btn-tab-pots");
            _tabCryo      = _hudRoot.Q<Button>("btn-tab-cryo");
            _sectionPots  = _hudRoot.Q<VisualElement>("dome-hud-section-pots");
            _sectionCryo  = _hudRoot.Q<VisualElement>("dome-hud-section-cryo");
            _tooltip      = _hudRoot.Q<VisualElement>("dome-hud-tooltip");
            _tooltipLines = _hudRoot.Q<VisualElement>("dome-hud-tooltip-lines");

            for (int i = 0; i < 4; i++)
            {
                _potRows[i]     = _hudRoot.Q<VisualElement>($"dome-pot-row-{i}");
                _potPreviews[i] = _hudRoot.Q<VisualElement>($"dome-pot-preview-{i}");
                _potNames[i]    = _hudRoot.Q<Label>($"dome-pot-name-{i}");
                _potSubs[i]     = _hudRoot.Q<Label>($"dome-pot-sub-{i}");
                _potConds[i]    = _hudRoot.Q<Label>($"dome-pot-cond-{i}");
                _potWater[i]    = _hudRoot.Q<Label>($"dome-pot-water-{i}");
                _potLed[i]      = _hudRoot.Q<Label>($"dome-pot-led-{i}");

                int idx = i;
                _potRows[i]?.RegisterCallback<MouseEnterEvent>(_ => OnPotRowHover(idx));
                _potRows[i]?.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());
            }

            for (int i = 0; i < 3; i++)
            {
                _cryoRows[i]    = _hudRoot.Q<VisualElement>($"dome-cryo-row-{i}");
                _cryoIds[i]     = _hudRoot.Q<Label>($"dome-cryo-id-{i}");
                _cryoPlants[i]  = _hudRoot.Q<Label>($"dome-cryo-plant-{i}");
                _cryoDetails[i] = _hudRoot.Q<Label>($"dome-cryo-detail-{i}");

                int idx = i;
                _cryoRows[i]?.RegisterCallback<MouseEnterEvent>(_ => OnCryoRowHover(idx));
                _cryoRows[i]?.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());
            }

            if (_toggleBtn != null)
            {
                _toggleBtn.clicked -= ToggleExpanded;
                _toggleBtn.clicked += ToggleExpanded;
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

            _expanded = false;
            ApplyExpandedState();
            SwitchTab(false);
        }

        // ─────────────────────────────────────────────────────────────
        // Expand / collapse
        // ─────────────────────────────────────────────────────────────

        private void ToggleExpanded()
        {
            _expanded = !_expanded;
            if (_expanded)
            {
                _userForcedCollapsed = false;
                _userForcedExpanded  = true;  // user manually opened → don't auto-close on empty
            }
            else
            {
                _userForcedCollapsed = true;  // user manually closed → don't auto-open on plant
                _userForcedExpanded  = false;
            }
            ApplyExpandedState();
        }

        private void ApplyExpandedState()
        {
            if (_hudRoot == null) return;
            _hudRoot.EnableInClassList("dome-hud-collapsed", !_expanded);
            if (_toggleBtn != null)
                _toggleBtn.text = _expanded ? "»" : "«";
        }

        private void CheckAutoExpand()
        {
            var pots = _potRegistry?.GetActivePotsSnapshot();
            bool hasAny = false;
            if (pots != null)
                foreach (var p in pots)
                    if (p?.PotActions?.HasPlant ?? false) { hasAny = true; break; }

            if (hasAny && !_expanded && !_userForcedCollapsed)
            {
                // Auto-open: plants exist and user hasn't forced it closed
                _expanded = true;
                ApplyExpandedState();
            }
            else if (!hasAny && _expanded && !_userForcedExpanded)
            {
                // Auto-close: no plants and user hasn't forced it open
                _expanded = false;
                ApplyExpandedState();
            }
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

        // Full refresh: data + auto-expand check. Called from OnEnable and game events.
        private void Refresh()
        {
            if (_hudRoot == null) return;
            RefreshPots();
            RefreshCryo();
            UpdateTabCounters();
            CheckAutoExpand();
        }

        // Data-only refresh: no expand-state changes. Called from the Update timer.
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
                PotSlot pot = (pots != null && i < pots.Count) ? pots[i] : null;
                PotStateModel state = pot?.PotActions?.GetCurrentState();
                PlantData plantData = state?.GetPlantData();

                _cachedPots[i]      = pot;
                _cachedStates[i]    = state;
                _cachedPlantData[i] = plantData;

                bool hasPlant = state?.HasPlant ?? false;

                // Preview sprite
                if (_potPreviews[i] != null)
                {
                    var sprite = hasPlant ? pot?.Sprite : null;
                    if (sprite != null)
                    {
                        _potPreviews[i].style.backgroundImage = new StyleBackground(sprite);
                        _potPreviews[i].style.backgroundSize  = new BackgroundSize(BackgroundSizeType.Contain);
                    }
                    else
                    {
                        _potPreviews[i].style.backgroundImage = new StyleBackground(StyleKeyword.Null);
                    }
                }

                // Name — green when occupied, muted when empty
                if (_potNames[i] != null)
                {
                    string potLabel = pot?.PotId ?? $"POT-00{i + 1}";
                    if (hasPlant)
                    {
                        _potNames[i].text = GetPlantDisplayName(plantData, state.PlantCode);
                        _potNames[i].style.color = new StyleColor(new Color(0.498f, 1f, 0.478f)); // rgb(127,255,122)
                    }
                    else
                    {
                        _potNames[i].text = $"{potLabel}  —";
                        _potNames[i].style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
                    }
                }

                // Sub info: Lvl, Stage, pH drift
                if (_potSubs[i] != null)
                {
                    if (hasPlant && state != null)
                    {
                        string phStr = plantData != null ? FormatPhDrift(plantData.GetDailyPhDrift()) : "0";
                        _potSubs[i].text = $"Lvl {state.PlantLevel} | {PlantStageLabel(state.Stage)} | pH {phStr}";
                    }
                    else
                    {
                        _potSubs[i].text = "";
                    }
                }

                // Condition — colour matches terminal: green/yellow/red
                if (_potConds[i] != null)
                {
                    if (hasPlant && state != null)
                    {
                        string condStr = ConditionLabel(state.ConditionScore);
                        _potConds[i].text = $"{condStr} ({state.ConditionScore}%)";
                        _potConds[i].style.color = new StyleColor(ConditionColor(state.ConditionScore));
                    }
                    else
                    {
                        _potConds[i].text = "";
                        _potConds[i].style.color = StyleKeyword.Null;
                    }
                }

                // Water indicator
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

                // LED indicator
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

                bool occupied = slot?.IsOccupied ?? false;
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

            if (_tabPots != null) _tabPots.text = $"POTS [{potsOccupied}/4]";
            if (_tabCryo != null) _tabCryo.text = $"CRYO [{cryoOccupied}/3]";
        }

        // ─────────────────────────────────────────────────────────────
        // Tooltip
        // ─────────────────────────────────────────────────────────────

        private void OnPotRowHover(int idx)
        {
            if (_tooltip == null || idx >= 4) return;

            var state     = _cachedStates[idx];
            var plantData = _cachedPlantData[idx];

            if (state == null || !state.HasPlant)
            {
                HideTooltip();
                return;
            }

            SetTooltipLines(BuildPotTooltipLines(state, plantData));
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private void OnCryoRowHover(int idx)
        {
            if (_tooltip == null || idx >= 3) return;

            var slot = _cachedCryoSlots[idx];
            if (slot == null || !slot.IsOccupied || slot.Payload == null)
            {
                HideTooltip();
                return;
            }

            SetTooltipLines(BuildCryoTooltipLines(slot));
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private void SetTooltipLines(System.Collections.Generic.List<TooltipLine> lines)
        {
            if (_tooltipLines == null) return;
            _tooltipLines.Clear();
            foreach (var line in lines)
            {
                var lbl = new Label(line.Text);
                if (line.IsSep)
                {
                    lbl.AddToClassList("dome-hud-tooltip-sep");
                }
                else
                {
                    lbl.AddToClassList("dome-hud-tooltip-line");
                    if (line.Bold)
                        lbl.AddToClassList("dome-hud-tooltip-line--bold");
                    lbl.style.color = new StyleColor(line.Color);
                }
                _tooltipLines.Add(lbl);
            }
        }

        private void HideTooltip()
        {
            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;
        }

        // ── TooltipLine ──────────────────────────────────────────────
        private struct TooltipLine
        {
            public string Text;
            public Color  Color;
            public bool   Bold;
            public bool   IsSep; // renders as dim separator (smaller font via CSS)

            public TooltipLine(string text, Color color, bool bold = false)
            {
                Text  = text; Color = color; Bold = bold; IsSep = false;
            }
            public static TooltipLine Sep() =>
                new TooltipLine { Text = "────────────────────────────────", IsSep = true };
        }

        // ── Palette shortcuts ────────────────────────────────────────
        private static readonly Color TipGreen  = new Color(0.498f, 1f, 0.478f);
        private static readonly Color TipYellow = new Color(0.902f, 0.788f, 0.435f);
        private static readonly Color TipRed    = new Color(0.827f, 0.373f, 0.373f);
        private static readonly Color TipMuted  = new Color(0.753f, 0.784f, 0.773f);

        // Returns green/yellow/red based on whether value is within [min,max]
        private static Color RangeColor(float value, float min, float max)
        {
            if (value >= min && value <= max) return TipGreen;
            float margin = Mathf.Max((max - min) * 0.25f, 8f);
            if (value >= min - margin && value <= max + margin) return TipYellow;
            return TipRed;
        }

        // Returns green if LED matches requirement, yellow if wrong LED active, red if off when needed
        private static Color LedColor(LedSystemState current, LedType? required)
        {
            if (!required.HasValue) return TipGreen;
            if (current == LedSystemState.Off)  return TipRed;
            LedType currentType = current == LedSystemState.Blue ? LedType.Blue : LedType.Red;
            return currentType == required.Value ? TipGreen : TipYellow;
        }

        private System.Collections.Generic.List<TooltipLine> BuildPotTooltipLines(PotStateModel state, PlantData plantData)
        {
            var lines = new System.Collections.Generic.List<TooltipLine>();

            string name  = GetPlantDisplayName(plantData, state.PlantCode);
            string stage = PlantStageLabel(state.Stage);
            string cond  = ConditionLabel(state.ConditionScore);

            lines.Add(new TooltipLine($"■ {name}  Lvl {state.PlantLevel}", TipGreen, bold: true));
            lines.Add(new TooltipLine($"  {state.PotId} · {stage} · Giorno {state.DaysInCurrentStage}", TipMuted));

            StageRequirements req = plantData?.GetStageRequirements((PlantStage)state.Stage);
            int hydPct = Mathf.Clamp(state.Hydration * 10, 0, 100);

            if (req != null)
            {
                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("REQUISITI E AVANZAMENTO", TipGreen, bold: true));
                lines.Add(new TooltipLine($"  Idratazione   : {req.hydrationMin}–{req.hydrationMax}%", TipMuted));
                LedType? reqLed = req.GetRequiredLed();
                lines.Add(new TooltipLine($"  LED           : {(reqLed.HasValue ? reqLed.Value.ToString() : "nessuno")}", TipMuted));
                lines.Add(new TooltipLine($"  Fertilizzante : {req.fertilizerMin}–{req.fertilizerMax}%", TipMuted));
                lines.Add(new TooltipLine($"  Durata        : {req.durationDays} giorni", TipMuted));

                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("STATO ATTUALE", TipGreen, bold: true));
                lines.Add(new TooltipLine($"  Idratazione   : {hydPct}%",
                    RangeColor(hydPct, req.hydrationMin, req.hydrationMax)));
                lines.Add(new TooltipLine($"  Fertilizzante : {state.FertilizerLevel}%",
                    RangeColor(state.FertilizerLevel, req.fertilizerMin, req.fertilizerMax)));
                lines.Add(new TooltipLine($"  LED           : {state.LedSystemState}",
                    LedColor(state.LedSystemState, req.GetRequiredLed())));
            }
            else
            {
                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("STATO ATTUALE", TipGreen, bold: true));
                lines.Add(new TooltipLine($"  Idratazione   : {hydPct}%", TipMuted));
                lines.Add(new TooltipLine($"  Fertilizzante : {state.FertilizerLevel}%", TipMuted));
                lines.Add(new TooltipLine($"  LED           : {state.LedSystemState}", TipMuted));
            }

            lines.Add(new TooltipLine($"  Condizione    : {cond} ({state.ConditionScore}%)",
                ConditionColor(state.ConditionScore)));
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

            return lines;
        }

        private System.Collections.Generic.List<TooltipLine> BuildCryoTooltipLines(CryoSlot slot)
        {
            var payload = slot.Payload;
            var lines   = new System.Collections.Generic.List<TooltipLine>();

            lines.Add(new TooltipLine($"❄  {GetCryoPlantDisplayName(payload)}  Lvl {payload.PlantLevel}", TipGreen, bold: true));
            lines.Add(new TooltipLine($"   {slot.SlotId}", TipMuted));

            if (!string.IsNullOrWhiteSpace(payload.PassivePowerLabel))
            {
                lines.Add(TooltipLine.Sep());
                lines.Add(new TooltipLine("POTERE PASSIVO", TipGreen, bold: true));
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
                        lines.Add(new TooltipLine("EFFETTO pH", TipGreen, bold: true));
                        float drift = mod.DailyDrift;
                        Color driftCol = Mathf.Abs(drift) < 0.01f ? TipMuted
                                       : drift > 0 ? TipGreen : TipYellow;
                        lines.Add(new TooltipLine($"   Drift/giorno: {FormatPhDrift(drift)}", driftCol));
                        if (Mathf.Abs(mod.PhCap) > 0.01f)
                            lines.Add(new TooltipLine($"   Cap pH: {mod.PhCap:F1}", TipMuted));
                        break;
                    }
                }
            }

            lines.Add(TooltipLine.Sep());
            lines.Add(new TooltipLine("NOTE", TipGreen, bold: true));
            lines.Add(new TooltipLine("   Poteri attivi sospesi in cryo", TipMuted));
            lines.Add(new TooltipLine("   Nessuna manutenzione richiesta", TipMuted));

            return lines;
        }

        // ─────────────────────────────────────────────────────────────
        // Static helpers
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

        private static string ConditionLabel(int score)
        {
            if (score >= 80) return "Rigogliosa";
            if (score >= 60) return "Sana";
            if (score >= 40) return "Appassita";
            return "Critica";
        }

        // Terminal palette: green ≥60, yellow 40-59, red <40
        private static Color ConditionColor(int score)
        {
            if (score >= 60) return new Color(0.498f, 1f, 0.478f);    // rgb(127,255,122) terminal green
            if (score >= 40) return new Color(0.902f, 0.788f, 0.435f); // rgb(230,201,111) terminal yellow
            return new Color(0.827f, 0.373f, 0.373f);                  // rgb(211, 95, 95) terminal red
        }

        private static string FormatPhDrift(float drift)
        {
            if (Mathf.Abs(drift) < 0.01f) return "0";
            return drift > 0 ? $"+{drift:F1}" : $"{drift:F1}";
        }

        private static string TruncateString(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxLen) return s;
            return s.Substring(0, maxLen - 3) + "…";
        }
    }
}
