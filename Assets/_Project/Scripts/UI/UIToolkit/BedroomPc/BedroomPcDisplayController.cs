using System;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using _Project.Systems.SeedStorage;
using Sporae.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.BedroomPc
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BedroomPcDisplayController : MonoBehaviour
    {
        /// <summary>Stesso livello di Dispensa/FoodRoom in modalità schermo pieno (1000), sotto VO overlay (1100).</summary>
        private const int SortingOrder = 1000;

        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Runtime Text")]
        [SerializeField] private string _terminalLabel = "TERMINALE 01";
        [SerializeField] private string _osLabel = "OS v2.7.4";
        [SerializeField] private string _userLabel = "UTENTE: OPERATORE";
        [SerializeField] private string _sessionLabel = "SESSIONE ATTIVA";
        [SerializeField] private string _connectionLabel = "CONNESSIONE: STABILE";
        [SerializeField] private bool _hideOnAwake = true;
        [SerializeField] private bool _cryoMachineOn = true;
        [SerializeField] private bool _compostProcessorOn = true;

        private const int CryoMachineDailyCost = 5;
        private const int CompostProcessorDailyCost = 3;

        public event Action ControlPanelRequested;
        public event Action ResearchCenterRequested;
        public event Action BlackMarketRequested;
        public event Action FaqBotRequested;

        /// <summary>PC visibile (Show).</summary>
        public event Action PcShown;

        /// <summary>PC nascosto (Hide / ESC).</summary>
        public event Action PcHidden;

        /// <summary>Pannello di controllo remoto aperto (lista macchinari).</summary>
        public event Action ControlPanelShown;

        /// <summary>Toggle Seed Storage dal pannello di controllo (dopo SetPower).</summary>
        public event Action<bool> SeedStoragePowerSetFromControlPanel;

        private VisualElement _root;
        private Label _terminal;
        private Label _os;
        private Label _time;
        private Label _user;
        private Label _session;
        private Label _connection;
        private VisualElement _home;
        private VisualElement _detail;
        private Label _detailTitle;
        private Label _detailStatus;
        private Label _detailPrimary;
        private Label _detailSecondary;
        private VisualElement _detailContent;
        private VisualElement _controlList;
        private Label _controlSeedState;
        private Label _controlSeedCost;
        private Label _controlFoodState;
        private Label _controlFoodCost;
        private Label _controlPantryState;
        private Label _controlPantryCost;
        private Label _controlCryoState;
        private Label _controlCryoCost;
        private Label _controlCompostState;
        private Label _controlCompostCost;
        private Label _controlTotalCost;
        private Button _controlPanelButton;
        private Button _researchButton;
        private Button _blackMarketButton;
        private Button _faqButton;
        private Button _backButton;
        private Button _controlSeedToggle;
        private Button _controlFoodToggle;
        private Button _controlPantryToggle;
        private Button _controlCryoToggle;
        private Button _controlCompostToggle;
        private GameManager _gameManager;
        private SeedStorageSystem _seedStorage;
        private FoodRoomSystem _foodRoom;
        private CryoMachineController _cryoMachine;
        private bool _bound;
        private bool _visible;
        private bool _controlPanelVisible;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = SortingOrder;

            BindUi();
            ApplyStaticText();

            if (_hideOnAwake)
                Hide();

            if (ServiceContainer.Instance != null)
                ServiceContainer.Instance.Register(this);
        }

        private void OnEnable()
        {
            BindUi();
            ApplyStaticText();
            RefreshClockLabel();
        }

        private void Update()
        {
            if (!_visible)
                return;

            RefreshClockLabel();
            if (_controlPanelVisible)
                RefreshControlPanel();

            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        private void OnDestroy()
        {
            UnbindClicks();
            if (_visible)
                GameplayUiModalLock.SetMachineModalState(false);
        }

        public bool IsPcVisible => _visible;

        public bool IsControlPanelVisible => _controlPanelVisible;

        public void Show()
        {
            BindUi();
            ApplyStaticText();
            RefreshClockLabel();
            ShowHomeView();

            if (_root != null)
                _root.style.display = DisplayStyle.Flex;

            _visible = true;
            GameplayUiModalLock.SetMachineModalState(true);
            PcShown?.Invoke();
        }

        public void Hide()
        {
            if (_root == null)
                BindUi();

            bool wasVisible = _visible;

            if (_root != null)
                _root.style.display = DisplayStyle.None;

            _visible = false;
            GameplayUiModalLock.SetMachineModalState(false);
            if (wasVisible)
                PcHidden?.Invoke();
        }

        private void BindUi()
        {
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
                return;

            var currentRoot = _uiDocument.rootVisualElement.Q<VisualElement>("bedroom-pc-display-root")
                ?? _uiDocument.rootVisualElement;

            if (_bound && ReferenceEquals(_root, currentRoot))
                return;

            UnbindClicks();
            _root = currentRoot;

            _terminal = _root.Q<Label>("bedroom-pc-terminal-label");
            _os = _root.Q<Label>("bedroom-pc-os-label");
            _time = _root.Q<Label>("bedroom-pc-time-label");
            _user = _root.Q<Label>("bedroom-pc-user-label");
            _session = _root.Q<Label>("bedroom-pc-session-label");
            _connection = _root.Q<Label>("bedroom-pc-connection-label");
            _home = _root.Q<VisualElement>("bedroom-pc-home");
            _detail = _root.Q<VisualElement>("bedroom-pc-detail");
            _detailTitle = _root.Q<Label>("bedroom-pc-detail-title");
            _detailStatus = _root.Q<Label>("bedroom-pc-detail-status");
            _detailPrimary = _root.Q<Label>("bedroom-pc-detail-primary");
            _detailSecondary = _root.Q<Label>("bedroom-pc-detail-secondary");
            _detailContent = _root.Q<VisualElement>("bedroom-pc-detail-content");
            _controlList = _root.Q<VisualElement>("bedroom-pc-control-list");
            _controlSeedState = _root.Q<Label>("bedroom-pc-control-seed-state");
            _controlSeedCost = _root.Q<Label>("bedroom-pc-control-seed-cost");
            _controlFoodState = _root.Q<Label>("bedroom-pc-control-food-state");
            _controlFoodCost = _root.Q<Label>("bedroom-pc-control-food-cost");
            _controlPantryState = _root.Q<Label>("bedroom-pc-control-pantry-state");
            _controlPantryCost = _root.Q<Label>("bedroom-pc-control-pantry-cost");
            _controlCryoState = _root.Q<Label>("bedroom-pc-control-cryo-state");
            _controlCryoCost = _root.Q<Label>("bedroom-pc-control-cryo-cost");
            _controlCompostState = _root.Q<Label>("bedroom-pc-control-compost-state");
            _controlCompostCost = _root.Q<Label>("bedroom-pc-control-compost-cost");
            _controlTotalCost = _root.Q<Label>("bedroom-pc-control-total-cost");

            _controlPanelButton = _root.Q<Button>("bedroom-pc-app-control");
            _researchButton = _root.Q<Button>("bedroom-pc-app-research");
            _blackMarketButton = _root.Q<Button>("bedroom-pc-app-blackmarket");
            _faqButton = _root.Q<Button>("bedroom-pc-app-faq");
            _backButton = _root.Q<Button>("bedroom-pc-back-button");
            _controlSeedToggle = _root.Q<Button>("bedroom-pc-control-seed-toggle");
            _controlFoodToggle = _root.Q<Button>("bedroom-pc-control-food-toggle");
            _controlPantryToggle = _root.Q<Button>("bedroom-pc-control-pantry-toggle");
            _controlCryoToggle = _root.Q<Button>("bedroom-pc-control-cryo-toggle");
            _controlCompostToggle = _root.Q<Button>("bedroom-pc-control-compost-toggle");

            if (_controlPanelButton != null) _controlPanelButton.clicked += HandleControlPanel;
            if (_researchButton != null) _researchButton.clicked += HandleResearchCenter;
            if (_blackMarketButton != null) _blackMarketButton.clicked += HandleBlackMarket;
            if (_faqButton != null) _faqButton.clicked += HandleFaqBot;
            if (_backButton != null) _backButton.clicked += ShowHomeView;
            if (_controlSeedToggle != null) _controlSeedToggle.clicked += ToggleSeedStoragePower;
            if (_controlFoodToggle != null) _controlFoodToggle.clicked += ToggleFoodSynthPower;
            if (_controlPantryToggle != null) _controlPantryToggle.clicked += TogglePantryPower;
            if (_controlCryoToggle != null) _controlCryoToggle.clicked += ToggleCryoMachinePower;
            if (_controlCompostToggle != null) _controlCompostToggle.clicked += ToggleCompostProcessorPower;

            _bound = true;
        }

        private void UnbindClicks()
        {
            if (!_bound)
                return;

            if (_controlPanelButton != null) _controlPanelButton.clicked -= HandleControlPanel;
            if (_researchButton != null) _researchButton.clicked -= HandleResearchCenter;
            if (_blackMarketButton != null) _blackMarketButton.clicked -= HandleBlackMarket;
            if (_faqButton != null) _faqButton.clicked -= HandleFaqBot;
            if (_backButton != null) _backButton.clicked -= ShowHomeView;
            if (_controlSeedToggle != null) _controlSeedToggle.clicked -= ToggleSeedStoragePower;
            if (_controlFoodToggle != null) _controlFoodToggle.clicked -= ToggleFoodSynthPower;
            if (_controlPantryToggle != null) _controlPantryToggle.clicked -= TogglePantryPower;
            if (_controlCryoToggle != null) _controlCryoToggle.clicked -= ToggleCryoMachinePower;
            if (_controlCompostToggle != null) _controlCompostToggle.clicked -= ToggleCompostProcessorPower;

            _bound = false;
        }

        private void ApplyStaticText()
        {
            if (_terminal != null) _terminal.text = _terminalLabel;
            if (_os != null) _os.text = _osLabel;
            if (_user != null) _user.text = _userLabel;
            if (_session != null) _session.text = _sessionLabel;
            if (_connection != null) _connection.text = _connectionLabel;
        }

        private void RefreshClockLabel()
        {
            if (_time == null)
                return;

            int day = 1;
            var dayCycle = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            if (dayCycle != null)
                day = Mathf.Max(1, dayCycle.CurrentDay);

            _time.text = $"GIORNO {day:00}   {DateTime.Now:HH:mm}";
        }

        private int GetCurrentDay()
        {
            var dayCycle = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            return dayCycle != null ? Mathf.Max(1, dayCycle.CurrentDay) : 1;
        }

        private void ShowHomeView()
        {
            if (_home != null)
                _home.style.display = DisplayStyle.Flex;
            if (_detail != null)
                _detail.style.display = DisplayStyle.None;
            if (_controlList != null)
                _controlList.style.display = DisplayStyle.None;
            _controlPanelVisible = false;
        }

        private void ShowDetailView(string title, string status, string primary, string secondary)
        {
            if (_home != null)
                _home.style.display = DisplayStyle.None;
            if (_detail != null)
                _detail.style.display = DisplayStyle.Flex;

            if (_detailTitle != null) _detailTitle.text = title;
            if (_detailStatus != null) _detailStatus.text = status;
            if (_detailPrimary != null) _detailPrimary.text = primary;
            if (_detailSecondary != null) _detailSecondary.text = secondary;
            _detailContent?.EnableInClassList("bedroom-pc-detail-content--control", false);
            if (_controlList != null)
                _controlList.style.display = DisplayStyle.None;
            _controlPanelVisible = false;
        }

        private void HandleControlPanel()
        {
            ShowControlPanelView();
            ControlPanelRequested?.Invoke();
        }

        private void ShowControlPanelView()
        {
            if (_home != null)
                _home.style.display = DisplayStyle.None;
            if (_detail != null)
                _detail.style.display = DisplayStyle.Flex;
            if (_controlList != null)
                _controlList.style.display = DisplayStyle.Flex;

            if (_detailTitle != null) _detailTitle.text = "PANNELLO DI CONTROLLO";
            if (_detailStatus != null) _detailStatus.text = "REMOTE";
            if (_detailPrimary != null) _detailPrimary.text = "PANORAMICA COSTI CRY / STATO REMOTO";
            if (_detailSecondary != null)
                _detailSecondary.text = "Lab e Dome non compaiono qui: non sono spegnibili da remoto in questa versione.";
            _detailContent?.EnableInClassList("bedroom-pc-detail-content--control", true);

            _controlPanelVisible = true;
            EnsureMachineServices();
            RefreshControlPanel();
            ControlPanelShown?.Invoke();
        }

        private void EnsureMachineServices()
        {
            _gameManager ??= ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            _seedStorage ??= _gameManager?.SeedStorageSystem;
            _foodRoom ??= _gameManager?.FoodRoomSystem;
            _cryoMachine ??= ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
        }

        private void RefreshControlPanel()
        {
            EnsureMachineServices();
            RefreshSeedStorageRow();
            RefreshFoodSynthRow();
            RefreshPantryRow();
            RefreshCryoMachineRow();
            RefreshCompostProcessorRow();
            RefreshControlPanelTotal();
        }

        private void RefreshSeedStorageRow()
        {
            if (_seedStorage == null)
            {
                ApplyUnavailablePowerVisual(_controlSeedToggle, _controlSeedState);
                if (_controlSeedCost != null)
                    _controlSeedCost.text = "0 CRY/giorno";
                return;
            }

            int tier1Occupied = 0;
            int tier2Occupied = 0;
            for (int i = 0; i < SeedStorageSystem.SlotCount; i++)
            {
                if (!_seedStorage.IsSlotUnlocked(i) || _seedStorage.SlotIsEmpty(i))
                    continue;
                if (i < SeedStorageSystem.Tier1SlotCount)
                    tier1Occupied++;
                else
                    tier2Occupied++;
            }

            int totalCost = _seedStorage.ComputeDailyCryCost();
            bool isOn = _seedStorage.IsOn;

            if (_controlSeedCost != null)
                _controlSeedCost.text = $"{totalCost} CRY/giorno";
            ApplyPowerVisual(_controlSeedToggle, _controlSeedState, isOn);
        }

        private void RefreshFoodSynthRow()
        {
            if (_foodRoom == null)
            {
                ApplyUnavailablePowerVisual(_controlFoodToggle, _controlFoodState);
                if (_controlFoodCost != null)
                    _controlFoodCost.text = "0 CRY/giorno";
                return;
            }

            bool isOn = _foodRoom.FoodSynthIsOn;
            int totalCost = _foodRoom.ComputeFoodSynthDailyCryCost();

            if (_controlFoodCost != null)
                _controlFoodCost.text = $"{totalCost} CRY/giorno";
            ApplyPowerVisual(_controlFoodToggle, _controlFoodState, isOn);
        }

        private void RefreshPantryRow()
        {
            if (_foodRoom == null)
            {
                ApplyUnavailablePowerVisual(_controlPantryToggle, _controlPantryState);
                if (_controlPantryCost != null)
                    _controlPantryCost.text = "0 CRY/giorno";
                return;
            }

            bool isOn = _foodRoom.PantryIsOn;
            int totalCost = _foodRoom.ComputePantryDailyCryCost();

            if (_controlPantryCost != null)
                _controlPantryCost.text = $"{totalCost} CRY/giorno";
            ApplyPowerVisual(_controlPantryToggle, _controlPantryState, isOn);
        }

        private void RefreshCryoMachineRow()
        {
            int totalCost = _cryoMachineOn ? CryoMachineDailyCost : 0;

            if (_controlCryoCost != null)
                _controlCryoCost.text = $"{totalCost} CRY/giorno";
            ApplyPowerVisual(_controlCryoToggle, _controlCryoState, _cryoMachineOn);
        }

        private void RefreshCompostProcessorRow()
        {
            int totalCost = _compostProcessorOn ? CompostProcessorDailyCost : 0;

            if (_controlCompostCost != null)
                _controlCompostCost.text = $"{totalCost} CRY/giorno";
            ApplyPowerVisual(_controlCompostToggle, _controlCompostState, _compostProcessorOn);
        }

        private void RefreshControlPanelTotal()
        {
            int total = GetSeedStorageCost() + GetFoodSynthCost() + GetPantryCost()
                + (_cryoMachineOn ? CryoMachineDailyCost : 0)
                + (_compostProcessorOn ? CompostProcessorDailyCost : 0);

            if (_controlTotalCost != null)
                _controlTotalCost.text = $"{total} CRY/GIORNO";
        }

        private void ToggleSeedStoragePower()
        {
            EnsureMachineServices();
            if (_seedStorage == null)
                return;
            _seedStorage.SetPower(!_seedStorage.IsOn);
            RefreshControlPanel();
            SeedStoragePowerSetFromControlPanel?.Invoke(_seedStorage.IsOn);
        }

        private void ToggleFoodSynthPower()
        {
            EnsureMachineServices();
            _foodRoom?.SetFoodSynthPower(!_foodRoom.FoodSynthIsOn);
            RefreshControlPanel();
        }

        private void TogglePantryPower()
        {
            EnsureMachineServices();
            _foodRoom?.SetPantryPower(!_foodRoom.PantryIsOn);
            RefreshControlPanel();
        }

        private void ToggleCryoMachinePower()
        {
            _cryoMachineOn = !_cryoMachineOn;
            RefreshControlPanel();
        }

        private void ToggleCompostProcessorPower()
        {
            _compostProcessorOn = !_compostProcessorOn;
            RefreshControlPanel();
        }

        private int GetSeedStorageCost()
        {
            EnsureMachineServices();
            return _seedStorage != null ? _seedStorage.ComputeDailyCryCost() : 0;
        }

        private int GetFoodSynthCost()
        {
            EnsureMachineServices();
            return _foodRoom != null ? _foodRoom.ComputeFoodSynthDailyCryCost() : 0;
        }

        private int GetPantryCost()
        {
            EnsureMachineServices();
            return _foodRoom != null ? _foodRoom.ComputePantryDailyCryCost() : 0;
        }

        private static string FormatPowerState(bool isOn)
        {
            return isOn ? "ACCESO" : "SPENTO";
        }

        private static void ApplyPowerVisual(Button toggle, Label stateLabel, bool isOn)
        {
            if (toggle != null)
            {
                toggle.SetEnabled(true);
                toggle.text = FormatPowerState(isOn);
                toggle.EnableInClassList("bedroom-pc-control-toggle--on", isOn);
                toggle.EnableInClassList("bedroom-pc-control-toggle--off", !isOn);
            }

            if (stateLabel != null)
            {
                stateLabel.text = FormatPowerState(isOn);
                stateLabel.EnableInClassList("bedroom-pc-control-value--on", isOn);
                stateLabel.EnableInClassList("bedroom-pc-control-value--off", !isOn);
            }
        }

        private static void ApplyUnavailablePowerVisual(Button toggle, Label stateLabel)
        {
            if (toggle != null)
            {
                toggle.text = "N/D";
                toggle.SetEnabled(false);
                toggle.EnableInClassList("bedroom-pc-control-toggle--on", false);
                toggle.EnableInClassList("bedroom-pc-control-toggle--off", false);
            }

            if (stateLabel != null)
            {
                stateLabel.text = "NON DISPONIBILE";
                stateLabel.EnableInClassList("bedroom-pc-control-value--on", false);
                stateLabel.EnableInClassList("bedroom-pc-control-value--off", false);
            }
        }

        private void HandleResearchCenter()
        {
            int day = GetCurrentDay();
            var wiki = ServiceContainer.Instance?.Get<WikiUnlockService>(suppressWarning: true);
            string previousResearch = "NESSUNA RICERCA NOTTURNA REGISTRATA";
            for (int d = day; d >= 1; d--)
            {
                if (wiki != null && wiki.TryGetNightResearchForDay(d, out var branch))
                {
                    previousResearch = $"GIORNO {d:00}: {branch.ToUpperInvariant()}";
                    break;
                }
            }

            ShowDetailView(
                "CENTRO DI RICERCA",
                "ARCHIVIO",
                $"ARCHIVIO WIKI .......... SINCRONIZZATO\nRICERCHE NOTTURNE ..... {previousResearch}\nDATABASE BOTANICO ..... ACCESSO LOCALE\nREGISTRO VAULT ........ LETTURA CONSENTITA",
                "Consulta qui lo stato della ricerca sbloccata. Il contenuto esteso resta agganciabile alla Wiki/Research UI esistente quando verra' portata su UI Toolkit.");
            ResearchCenterRequested?.Invoke();
        }

        private void HandleBlackMarket() => BlackMarketRequested?.Invoke();
        private void HandleFaqBot()
        {
            ShowDetailView(
                "FAQ - BOT",
                "ASSIST",
                "Q: COME FINISCO LA GIORNATA?\nA: USA IL BED.\n\nQ: DOVE VENDO RISORSE?\nA: APRI BLACK MARKET DAL PC.\n\nQ: DOVE CONTROLLO LA RICERCA?\nA: APRI CENTRO DI RICERCA.\n\nQ: POSSO USCIRE DAL PC?\nA: ESC CHIUDE IL DISPLAY.",
                "BOT IN MODALITA' LOCALE. Le risposte sono pensate per la demo Alpha e possono essere sostituite da contenuti narrativi o tutorial contestuali.");
            FaqBotRequested?.Invoke();
        }

    }
}
