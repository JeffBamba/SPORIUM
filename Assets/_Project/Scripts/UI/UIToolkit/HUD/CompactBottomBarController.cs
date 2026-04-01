using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using _Project.World.VaultMap;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Controller per la CompactBottomBar (42px).
    /// Gestisce: DAY counter, CRY balance + tooltip, room icons + hover tooltips,
    /// pulsanti Options / Save / Exit.
    /// Sostituisce BottomNavigationController.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CompactBottomBarController : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("System References")]
        [SerializeField] private AppRoot _appRoot;
        [Tooltip("Menu in-game (stesso effetto del tasto ESC su Pages). Assegna il GO Menu con MainMenuScreens.")]
        [SerializeField] private MainMenuScreens _mainMenuScreens;
        /// <summary>Fallback se MainMenuScreens non è in scena.</summary>
        [SerializeField] private OptionsPopupController _optionsController;

        [Header("Configuration")]
        [SerializeField] private string _defaultRoom = "dome";
        [SerializeField] private bool _enableDebugLogs = false;

        [Header("Location (RoomAreaTag)")]
        [Tooltip("Ritardo tra caratteri per l'etichetta [Location: …] in basso.")]
        [SerializeField, Range(0.01f, 0.2f)] private float _locationTypewriterCharDelay = 0.045f;

        // ── Services ──
        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        private EconomySystem _economySystem;
        private DiaryStatistics _diaryStatistics;
        private RoomTracker _roomTracker;
        private SaveManager _saveManager;

        // ── UI Elements ──
        private VisualElement _root;

        // Status zone
        private Label _dayLabel;
        private VisualElement _cryBadge;
        private Label _cryLabel;
        private Label _locationLabel;
        private Coroutine _locationTypewriterRoutine;
        private VisualElement _cryTooltip;
        private Label _cryBalanceValue;
        private Label _cryEarnedToday;
        private Label _crySpentToday;
        private Label _cryNetToday;

        // Room zone
        private readonly Dictionary<string, VisualElement> _roomButtons = new();

        // Room tooltip
        private VisualElement _roomTooltip;
        private Label _roomTooltipName;
        private Label _roomTooltipFloor;
        private Label _roomTooltipDesc;

        // System zone
        private Button _btnOptions;
        private Button _btnSave;
        private Button _btnExit;

        // State
        private string _activeRoom = string.Empty;
        private static readonly string[] RoomIds = { "dome", "lab", "kitchen", "dormitory", "visitor", "storage", "restricted1", "restricted2" };

        // ── Room metadata cache (populated from RoomAreaTag via RoomTracker/scene) ──
        private readonly Dictionary<string, RoomAreaTag> _roomTags = new();

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            // PlayerStatusPanelController / TopBar usano sortingOrder 50. Senza questo, l’intero documento
            // (incluso cry-tooltip) resta sotto al Player Status box. Tooltip deve vincere lo stesso layer HUD.
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 55;

            // UXML tiene cry/room tooltip in display:flex per UI Builder; in Play li nascondiamo subito (hover li riapre).
            HideTooltipsUntilHover();
        }

        private void HideTooltipsUntilHover()
        {
            var root = _uiDocument?.rootVisualElement;
            if (root == null) return;
            var cry = root.Q<VisualElement>("cry-tooltip");
            if (cry != null) cry.style.display = DisplayStyle.None;
            var room = root.Q<VisualElement>("room-tooltip");
            if (room != null) room.style.display = DisplayStyle.None;
        }

        private void Start()
        {
            ResolveServices();
            BuildUI();
            TryBindMainMenuScreens();
            SubscribeToServices();
            if (_roomTracker != null && !string.IsNullOrEmpty(_roomTracker.CurrentRoomId))
                OnRoomTrackerChanged(_roomTracker.CurrentRoomId);
            else
            {
                SetActiveRoom(_defaultRoom);
                PlayLocationTypewriter(_defaultRoom);
            }
        }

        private void OnDestroy()
        {
            if (_locationTypewriterRoutine != null)
            {
                StopCoroutine(_locationTypewriterRoutine);
                _locationTypewriterRoutine = null;
            }
            UnsubscribeFromServices();
        }

        // ── Service resolution ──

        private void ResolveServices()
        {
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            _gameManager    = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            _diaryStatistics = ServiceContainer.Instance?.Get<DiaryStatistics>(suppressWarning: true);
            _saveManager    = ServiceContainer.Instance?.Get<SaveManager>(suppressWarning: true);
            _roomTracker    = ServiceContainer.Instance?.Get<RoomTracker>(suppressWarning: true);

            if (_gameManager != null)
                _economySystem = _gameManager.EconomySystem;

            if (_appRoot == null)
                _appRoot = AppRoot.Instance;

            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, "[CompactBottomBar] Services resolved.");
        }

        // ── UI setup ──

        private void BuildUI()
        {
            var uiRoot = _uiDocument.rootVisualElement;
            _root = uiRoot.Q<VisualElement>("compact-bottom-bar");
            if (_root == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "[CompactBottomBar] Root element 'compact-bottom-bar' not found.");
                enabled = false;
                return;
            }

            // Status zone
            _dayLabel = _root.Q<Label>("day-label");
            _cryBadge = _root.Q<VisualElement>("cry-badge");
            _cryLabel = _root.Q<Label>("cry-label");
            _locationLabel = _root.Q<Label>("location-label");

            // CRY tooltip — figlio di uiRoot (dopo TopBar) così il draw order è sopra il Player Box HUD
            _cryTooltip     = uiRoot.Q<VisualElement>("cry-tooltip");
            _cryBalanceValue = _cryTooltip?.Q<Label>("cry-balance-value");
            _cryEarnedToday  = _cryTooltip?.Q<Label>("cry-earned-today");
            _crySpentToday   = _cryTooltip?.Q<Label>("cry-spent-today");
            _cryNetToday     = _cryTooltip?.Q<Label>("cry-net-today");

            // CRY badge hover
            if (_cryBadge != null && _cryTooltip != null)
            {
                _cryBadge.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    _cryTooltip.BringToFront();
                    _cryTooltip.style.display = DisplayStyle.Flex;
                });
                _cryBadge.RegisterCallback<MouseLeaveEvent>(_ => _cryTooltip.style.display = DisplayStyle.None);
            }

            // Room tooltip — stesso layer di CRY (dopo TopBar)
            _roomTooltip      = uiRoot.Q<VisualElement>("room-tooltip");
            _roomTooltipName  = _roomTooltip?.Q<Label>("room-tooltip-name");
            _roomTooltipFloor = _roomTooltip?.Q<Label>("room-tooltip-floor");
            _roomTooltipDesc  = _roomTooltip?.Q<Label>("room-tooltip-desc");

            // Room buttons
            foreach (var id in RoomIds)
            {
                var btn = _root.Q<VisualElement>($"room-btn-{id}");
                if (btn == null) continue;
                _roomButtons[id] = btn;

                string capturedId = id;
                btn.RegisterCallback<ClickEvent>(_ => OnRoomButtonClicked(capturedId));
                btn.RegisterCallback<MouseEnterEvent>(_ => ShowRoomTooltip(capturedId, btn));
                btn.RegisterCallback<MouseLeaveEvent>(_ => HideRoomTooltip());
            }

            // Gather RoomAreaTags from scene for tooltip metadata
            var tags = FindObjectsOfType<RoomAreaTag>();
            foreach (var tag in tags)
                if (!string.IsNullOrEmpty(tag.RoomId))
                    _roomTags[tag.RoomId] = tag;

            // Apply locked state from tags
            foreach (var kv in _roomTags)
            {
                if (kv.Value.IsLocked && _roomButtons.TryGetValue(kv.Key, out var btn))
                    ApplyLockedState(btn, true);
            }

            // System buttons
            _btnOptions = _root.Q<Button>("btn-options");
            _btnSave    = _root.Q<Button>("btn-save");
            _btnExit    = _root.Q<Button>("btn-exit");

            _btnOptions?.RegisterCallback<ClickEvent>(_ => OnOptionsClicked());
            _btnSave?.RegisterCallback<ClickEvent>(_ => OnSaveClicked());
            _btnExit?.RegisterCallback<ClickEvent>(_ => OnExitClicked());
        }

        // ── Event subscriptions ──

        private void SubscribeToServices()
        {
            if (_dayCycleSystem != null)
            {
                _dayCycleSystem.OnDayChanged += OnDayChanged;
                OnDayChanged(_dayCycleSystem.CurrentDay);
            }

            if (_economySystem != null)
            {
                _economySystem.OnCRYChanged += OnCRYChanged;
                OnCRYChanged(_economySystem.CurrentCRY);
            }

            if (_roomTracker != null)
                _roomTracker.OnRoomChanged += OnRoomTrackerChanged;
        }

        private void UnsubscribeFromServices()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= OnDayChanged;

            if (_economySystem != null)
                _economySystem.OnCRYChanged -= OnCRYChanged;

            if (_roomTracker != null)
                _roomTracker.OnRoomChanged -= OnRoomTrackerChanged;
        }

        // ── DAY ──

        private void OnDayChanged(int day)
        {
            if (_dayLabel != null)
                _dayLabel.text = NotificationLocalization.Pick($"GIORNO - {day}", $"DAY - {day}");
        }

        // ── CRY ──

        private void OnCRYChanged(int cry)
        {
            if (_cryLabel != null)
                _cryLabel.text = $"{cry:N0} CRY";

            RefreshCryTooltip(cry);
        }

        private void RefreshCryTooltip(int balance)
        {
            if (_cryBalanceValue != null)
                _cryBalanceValue.text = $"{balance:N0} CRY";

            if (_diaryStatistics != null)
            {
                int earned = _diaryStatistics.CryEarned;
                int spent  = _diaryStatistics.CrySpent;
                int net    = earned - spent;

                if (_cryEarnedToday != null) _cryEarnedToday.text = $"+{earned:N0} CRY";
                if (_crySpentToday  != null) _crySpentToday.text  = $"-{spent:N0} CRY";
                if (_cryNetToday    != null)
                {
                    _cryNetToday.text = net >= 0 ? $"+{net:N0} CRY" : $"{net:N0} CRY";
                    _cryNetToday.RemoveFromClassList("cbb-cry-green");
                    _cryNetToday.RemoveFromClassList("cbb-cry-red");
                    _cryNetToday.AddToClassList(net >= 0 ? "cbb-cry-green" : "cbb-cry-red");
                }
            }
        }

        // ── Location label (RoomTracker + RoomAreaTag.DisplayName) ──

        private void OnRoomTrackerChanged(string roomId)
        {
            SetActiveRoom(roomId);
            PlayLocationTypewriter(roomId);
        }

        private void PlayLocationTypewriter(string roomId)
        {
            if (_locationLabel == null) return;
            string display = ResolveRoomDisplayName(roomId);
            string full = NotificationLocalization.Pick($"[Posizione: {display}]", $"[Location: {display}]");
            if (_locationTypewriterRoutine != null)
            {
                StopCoroutine(_locationTypewriterRoutine);
                _locationTypewriterRoutine = null;
            }
            _locationTypewriterRoutine = StartCoroutine(LocationTypewriterRoutine(full));
        }

        private string ResolveRoomDisplayName(string roomId)
        {
            if (string.IsNullOrEmpty(roomId))
                return "—";
            if (TryGetLocalizedRoomTooltip(roomId, out var locName, out _, out _))
                return locName;
            if (_roomTracker != null &&
                string.Equals(_roomTracker.CurrentRoomId, roomId, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(_roomTracker.CurrentDisplayName))
                return _roomTracker.CurrentDisplayName;
            if (_roomTags.TryGetValue(roomId, out var tag) && !string.IsNullOrEmpty(tag.DisplayName))
                return tag.DisplayName;
            return roomId.ToUpperInvariant();
        }

        private IEnumerator LocationTypewriterRoutine(string target)
        {
            _locationLabel.text = string.Empty;
            if (string.IsNullOrEmpty(target))
            {
                _locationTypewriterRoutine = null;
                yield break;
            }

            var wait = new WaitForSeconds(_locationTypewriterCharDelay);
            for (int len = 1; len <= target.Length; len++)
            {
                _locationLabel.text = target.Substring(0, len);
                yield return wait;
            }

            _locationTypewriterRoutine = null;
        }

        // ── Room navigation ──

        private void OnRoomButtonClicked(string roomId)
        {
            if (_roomButtons.TryGetValue(roomId, out var btn) && btn.ClassListContains("room-locked"))
                return;

            SetActiveRoom(roomId);
        }

        public void SetActiveRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return;

            // Deactivate previous
            if (!string.IsNullOrEmpty(_activeRoom) && _roomButtons.TryGetValue(_activeRoom, out var prev))
            {
                prev.RemoveFromClassList("room-active");
                if (!prev.ClassListContains("room-locked"))
                    prev.AddToClassList("room-available");
            }

            _activeRoom = roomId;

            // Activate new
            if (_roomButtons.TryGetValue(roomId, out var next))
            {
                next.RemoveFromClassList("room-available");
                next.AddToClassList("room-active");
            }

            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, $"[CompactBottomBar] Active room: {roomId}");
        }

        private void ShowRoomTooltip(string roomId, VisualElement btn)
        {
            if (_roomTooltip == null) return;

            if (TryGetLocalizedRoomTooltip(roomId, out var name, out var floor, out var desc))
            {
                if (_roomTooltipName  != null) _roomTooltipName.text  = name;
                if (_roomTooltipFloor != null) _roomTooltipFloor.text = floor;
                if (_roomTooltipDesc  != null) _roomTooltipDesc.text  = desc;
            }
            else if (_roomTags.TryGetValue(roomId, out var tag))
            {
                if (_roomTooltipName  != null) _roomTooltipName.text  = tag.DisplayName;
                if (_roomTooltipFloor != null) _roomTooltipFloor.text = tag.FloorName;
                if (_roomTooltipDesc  != null) _roomTooltipDesc.text  = tag.TooltipText;
            }
            else
            {
                if (_roomTooltipName  != null) _roomTooltipName.text  = roomId.ToUpperInvariant();
                if (_roomTooltipFloor != null) _roomTooltipFloor.text = string.Empty;
                if (_roomTooltipDesc  != null) _roomTooltipDesc.text  = string.Empty;
            }

            _roomTooltip.BringToFront();
            _roomTooltip.style.display = DisplayStyle.Flex;

            // Posiziona la tooltip centrata sul bottone, clampata per non uscire dallo schermo
            _roomTooltip.schedule.Execute(() =>
            {
                var btnBounds     = btn.worldBound;
                float tooltipW    = _roomTooltip.resolvedStyle.width;
                float panelW      = _uiDocument.rootVisualElement.resolvedStyle.width;
                float idealLeft   = btnBounds.center.x - tooltipW * 0.5f;
                float clampedLeft = Mathf.Clamp(idealLeft, 4f, panelW - tooltipW - 4f);
                _roomTooltip.style.left = clampedLeft;
            }).ExecuteLater(0);
        }

        private void HideRoomTooltip()
        {
            if (_roomTooltip != null)
                _roomTooltip.style.display = DisplayStyle.None;
        }

        private static void ApplyLockedState(VisualElement btn, bool locked)
        {
            if (locked)
            {
                btn.RemoveFromClassList("room-available");
                btn.AddToClassList("room-locked");
            }
            else
            {
                btn.RemoveFromClassList("room-locked");
                btn.AddToClassList("room-available");
            }
        }

        // ── System actions ──

        /// <summary>
        /// Se <see cref="_mainMenuScreens"/> non è assegnato in Inspector, risolve il <see cref="MainMenuScreens"/>
        /// presente in scena (es. sul prefab Menu — stesso componente che gestisce ESC).
        /// Fallback esplicito: non sostituisce un riferimento serializzato valido.
        /// </summary>
        private void TryBindMainMenuScreens()
        {
            if (_mainMenuScreens != null) return;
            _mainMenuScreens = UnityEngine.Object.FindFirstObjectByType<MainMenuScreens>(FindObjectsInactive.Include);
        }

        private void OnOptionsClicked()
        {
            TryBindMainMenuScreens();

            // Stesso comportamento di ESC: MainMenuScreens.Toggle() → mostra/nasconde Pages
            if (_mainMenuScreens != null)
            {
                _mainMenuScreens.ToggleMenuPage();
                return;
            }

            if (_optionsController != null)
            {
                _optionsController.gameObject.SetActive(true);
                return;
            }

            SporiumLogger.LogWarning(LogCategory.UI, "[CompactBottomBar] Assegna MainMenuScreens (Menu) per replicare ESC, oppure OptionsPopupController.");
        }

        private void OnSaveClicked()
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);

            if (_saveManager != null)
            {
                bool ok = _saveManager.SaveGame("default");
                if (_enableDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.UI, $"[CompactBottomBar] SaveGame result: {ok}");

                // Toast Foundation con spec registrate (SAVE-RESULT non esisteva nel resolver)
                if (ok)
                    foundation?.PostToastImmediate("SYS-003", null, NotificationSeverity.Success);
                else
                    foundation?.PostToastImmediate("SYS-004", null, NotificationSeverity.Warning);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[CompactBottomBar] SaveManager non disponibile.");
                foundation?.PostToastImmediate("SYS-004", null, NotificationSeverity.Warning);
            }
        }

        private void OnExitClicked()
        {
            if (_appRoot != null)
                _appRoot.QuitApplication();
            else
                Application.Quit();
        }

        /// <summary>
        /// Copia IT/EN per tooltip e etichetta posizione — indipendente dai <see cref="RoomAreaTag"/> in scena
        /// (così la lingua delle opzioni controlla il testo senza editare la scena).
        /// </summary>
        private static bool TryGetLocalizedRoomTooltip(string roomId, out string name, out string floor, out string desc)
        {
            name = floor = desc = string.Empty;
            if (string.IsNullOrEmpty(roomId))
                return false;

            switch (roomId.ToLowerInvariant())
            {
                case "dome":
                    name = NotificationLocalization.Pick("Cupola", "Dome");
                    floor = NotificationLocalization.Pick("Piano -1", "Floor -1");
                    desc = NotificationLocalization.Pick(
                        "Cuore biologico del Vault. Le piante ti obbediscono — o ti tradiscono.",
                        "Biological heart of the Vault. The plants obey—or betray—you.");
                    return true;
                case "lab":
                    name = NotificationLocalization.Pick("Laboratorio", "Lab");
                    floor = NotificationLocalization.Pick("Piano -1", "Floor -1");
                    desc = NotificationLocalization.Pick(
                        "Ricerca, analisi, protocolli. Ogni campione racconta una storia.",
                        "Research, analysis, protocols. Every sample tells a story.");
                    return true;
                case "kitchen":
                    name = NotificationLocalization.Pick("Cucina", "Kitchen");
                    floor = NotificationLocalization.Pick("Piano -2", "Floor -2");
                    desc = NotificationLocalization.Pick(
                        "Coltiva ciò che ti tiene in vita. Non tutto ciò che nutre è puro.",
                        "Grow what keeps you alive. Not everything that feeds is pure.");
                    return true;
                case "dormitory":
                    name = NotificationLocalization.Pick("Dormitorio", "Dormitory");
                    floor = NotificationLocalization.Pick("Piano -1", "Floor -1");
                    desc = NotificationLocalization.Pick(
                        "Riposo e recupero. Il corpo ricorda ciò che la mente vorrebbe dimenticare.",
                        "Rest and recovery. The body remembers what the mind would forget.");
                    return true;
                case "visitor":
                    name = NotificationLocalization.Pick("Sala visitatori", "Visitor Room");
                    floor = NotificationLocalization.Pick("Piano terra", "Ground floor");
                    desc = NotificationLocalization.Pick(
                        "Punto d'ingresso per i sopravvissuti. Ogni visita può cambiare l'equilibrio del Vault.",
                        "Entry point for survivors. Every visit can shift the Vault's balance.");
                    return true;
                case "storage":
                    name = NotificationLocalization.Pick("Deposito semi", "Seed Storage");
                    floor = NotificationLocalization.Pick("Piano terra", "Ground floor");
                    desc = NotificationLocalization.Pick(
                        "Archivio criogenico per semi e spore. Tutto dorme finché non serve.",
                        "Cryogenic archive for seeds and spores. Everything sleeps until needed.");
                    return true;
                case "restricted1":
                    name = NotificationLocalization.Pick("Zona riservata I", "Restricted Zone I");
                    floor = NotificationLocalization.Pick("Accesso limitato", "Restricted access");
                    desc = NotificationLocalization.Pick(
                        "Oltre questa soglia valgono protocolli di sicurezza diversi.",
                        "Beyond this threshold, different security protocols apply.");
                    return true;
                case "restricted2":
                    name = NotificationLocalization.Pick("Zona riservata II", "Restricted Zone II");
                    floor = NotificationLocalization.Pick("Accesso limitato", "Restricted access");
                    desc = NotificationLocalization.Pick(
                        "Livello di sicurezza elevato. Solo autorizzazione esplicita.",
                        "High security clearance. Explicit authorization only.");
                    return true;
                default:
                    return false;
            }
        }
    }
}
