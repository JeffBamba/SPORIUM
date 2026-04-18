using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using _Project.Sporae.Core;
using _Project.Sporae.Core.Installers;

namespace _Project.UI.UIToolkit.MainMenu
{
    /// <summary>
    /// UI Toolkit front-end per il menu principale.
    /// Slot salvataggi/caricamento in UI Toolkit (stesso USS del menu); Opzioni ancora su popup uGUI legacy.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public class MainMenuUIToolkitController : MonoBehaviour
    {
        private const string MainMenuSceneName = "SCN_MainMenu";
        private const string VisualTreeResourcePath = "UI/UIToolkit/MainMenu/MainMenu";
        private const string PanelSettingsResourcePath = "UI/UIToolkit/MainMenu/MainMenuPanelSettings";

        [Header("Runtime References")]
        [SerializeField] private MainMenuOptions _mainMenuOptions;
        [SerializeField] private MainMenuScreens _mainMenuScreens;

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _loadingOverlay;
        private VisualElement _loadingFill;
        private Label _loadingText;

        private VisualElement _saveSlotsOverlay;
        private Label _saveSlotsTitle;
        private Label _saveSlotsSubtitle;
        private bool _saveSlotsVisible;
        private bool _saveSlotsModeIsSave;

        private bool _isLoading;
        private bool _isMainMenuScene;
        /// <summary>
        /// Popup Opzioni uGUI sotto al layer UI Toolkit: nascondiamo il root finché il popup è aperto.
        /// </summary>
        private bool _toolkitSuppressedForLegacyPopup;

        public bool IsRuntimeReady => _root != null;

        private void Awake()
        {
            if (_mainMenuOptions == null)
                _mainMenuOptions = GetComponent<MainMenuOptions>();
            if (_mainMenuScreens == null)
                _mainMenuScreens = GetComponent<MainMenuScreens>();
            TryBuildUiFromMenuContext();
        }

        public void InjectRuntimeReferences(MainMenuOptions mainMenuOptions, MainMenuScreens mainMenuScreens)
        {
            _mainMenuOptions = mainMenuOptions;
            _mainMenuScreens = mainMenuScreens;
            TryBuildUiFromMenuContext();
            ApplyMenuBootstrapAfterBuild();
        }

        /// <summary>
        /// Costruzione UI: in Awake se <see cref="MainMenuOptions"/> è sullo stesso GO; dopo <see cref="InjectRuntimeReferences"/> per l’host creato da HUD.
        /// </summary>
        private void TryBuildUiFromMenuContext()
        {
            if (_root != null || _mainMenuOptions == null)
                return;

            _isMainMenuScene = SceneManager.GetActiveScene().name == MainMenuSceneName;
            BuildMenuUiToolkit();
            HookButtons();
        }

        private void ApplyMenuBootstrapAfterBuild()
        {
            if (_root == null || _mainMenuOptions == null)
                return;
            _mainMenuOptions.SetLegacyButtonsVisible(false);
            _mainMenuScreens?.SetEscapeHandlingEnabled(false);
            if (!_isMainMenuScene)
                HideInGameMenu();
        }

        private void Start()
        {
            TryBuildUiFromMenuContext();
            ApplyMenuBootstrapAfterBuild();
        }

        private void Update()
        {
            if (_root != null && _toolkitSuppressedForLegacyPopup && _mainMenuScreens != null)
            {
                if (!_mainMenuScreens.IsSlotsOpen && !_mainMenuScreens.IsOptionsOpen)
                {
                    _root.style.display = DisplayStyle.Flex;
                    _toolkitSuppressedForLegacyPopup = false;
                }
            }

            if (_root == null || _isLoading)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) && _saveSlotsVisible)
            {
                HideSaveSlotsOverlay();
                return;
            }

            if (_isMainMenuScene)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleInGameMenu();
        }

        private void BuildMenuUiToolkit()
        {
            var vta = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (vta == null)
            {
                Debug.LogError($"[MainMenuUIToolkit] VisualTreeAsset non trovato: {VisualTreeResourcePath}");
                return;
            }

            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettings == null)
            {
                Debug.LogError($"[MainMenuUIToolkit] PanelSettings non trovato: {PanelSettingsResourcePath}");
                return;
            }

            var uiGo = new GameObject("MainMenu_UIToolkit", typeof(UIDocument));
            uiGo.transform.SetParent(transform, false);
            _document = uiGo.GetComponent<UIDocument>();
            _document.panelSettings = panelSettings;
            _document.visualTreeAsset = vta;
            // Sopra TopBar/CompactBottomBar (200), Foundation (150), DomeStatus (55), PlantCard terminal (600); sotto EoD (2500) e modali full (es. 1000).
            _document.sortingOrder = 700;

            _root = _document.rootVisualElement;
            _loadingOverlay = _root.Q<VisualElement>("loading-overlay");
            _loadingFill = _root.Q<VisualElement>("loading-progress-fill");
            _loadingText = _root.Q<Label>("loading-text");

            _saveSlotsOverlay = _root.Q<VisualElement>("save-slots-overlay");
            _saveSlotsTitle = _root.Q<Label>("save-slots-title");
            _saveSlotsSubtitle = _root.Q<Label>("save-slots-subtitle");
            if (_saveSlotsOverlay != null)
                _saveSlotsOverlay.style.display = DisplayStyle.None;
        }

        private void HookButtons()
        {
            if (_root == null) return;

            var btnNewGame = _root.Q<Button>("btn-new-game");
            var btnLoadGame = _root.Q<Button>("btn-load-game");
            var btnDemo = _root.Q<Button>("btn-demo");
            var btnCredits = _root.Q<Button>("btn-credits");
            var btnExit = _root.Q<Button>("btn-exit");
            var btnSettings = _root.Q<Button>("btn-settings");

            btnNewGame?.RegisterCallback<ClickEvent>(_ => StartNewGameLoad());
            btnLoadGame?.RegisterCallback<ClickEvent>(_ => _mainMenuOptions.OpenLoadPopupFromExternalUI());
            btnDemo?.RegisterCallback<ClickEvent>(_ => StartDemoLoad());
            btnExit?.RegisterCallback<ClickEvent>(_ => _mainMenuOptions.QuitFromExternalUI());
            btnSettings?.RegisterCallback<ClickEvent>(_ =>
                OpenLegacyMainMenuPopup(_mainMenuOptions.OpenOptionsPopupFromExternalUI));

            if (btnCredits != null)
                btnCredits.SetEnabled(false);

            WireSaveSlotsButtons();
        }

        private void WireSaveSlotsButtons()
        {
            var close = _root.Q<Button>("btn-save-slots-close");
            close?.RegisterCallback<ClickEvent>(_ => HideSaveSlotsOverlay());

            for (var i = 0; i < SaveManager.SlotNames.Length; i++)
            {
                var index = i;
                var primary = _root.Q<Button>($"save-slot-primary-{i}");
                var del = _root.Q<Button>($"save-slot-delete-{i}");
                primary?.RegisterCallback<ClickEvent>(_ => OnSaveSlotPrimary(index));
                del?.RegisterCallback<ClickEvent>(_ => OnSaveSlotDelete(index));
            }
        }

        /// <summary>Carica partita — UI Toolkit (stesso menu).</summary>
        public void OpenLoadSlotsOverlay() => ShowSaveSlotsOverlay(false);

        /// <summary>Salva su slot — UI Toolkit.</summary>
        public void OpenSaveSlotsOverlay() => ShowSaveSlotsOverlay(true);

        private void ShowSaveSlotsOverlay(bool forSave)
        {
            if (_saveSlotsOverlay == null)
                return;

            if (!_isMainMenuScene && _root != null && _root.style.display == DisplayStyle.None)
                ShowInGameMenu();

            _saveSlotsModeIsSave = forSave;
            if (_saveSlotsTitle != null)
                _saveSlotsTitle.text = forSave ? "SALVA PARTITA" : "CARICA PARTITA";
            if (_saveSlotsSubtitle != null)
            {
                _saveSlotsSubtitle.text = forSave
                    ? "Scegli uno slot — i dati esistenti verranno sovrascritti."
                    : "Seleziona uno slot con salvataggio valido.";
            }

            RefreshSaveSlotsUi();
            _saveSlotsOverlay.style.display = DisplayStyle.Flex;
            _saveSlotsVisible = true;
        }

        private void HideSaveSlotsOverlay()
        {
            if (_saveSlotsOverlay == null)
                return;
            _saveSlotsOverlay.style.display = DisplayStyle.None;
            _saveSlotsVisible = false;
        }

        private void RefreshSaveSlotsUi()
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null)
                return;

            for (var i = 0; i < SaveManager.SlotNames.Length; i++)
            {
                var slotName = SaveManager.SlotNames[i];
                var summaryLabel = _root.Q<Label>($"save-slot-summary-{i}");
                var primary = _root.Q<Button>($"save-slot-primary-{i}");
                var primaryLbl = _root.Q<Label>($"save-slot-primary-label-{i}");
                var deleteBtn = _root.Q<Button>($"save-slot-delete-{i}");

                string displayName = SaveManager.GetSlotDisplayName(slotName);
                var summary = saveManager.GetSaveSummary(slotName);
                bool hasSave = summary.HasValue;

                if (summaryLabel != null)
                {
                    if (hasSave)
                    {
                        var s = summary.Value;
                        summaryLabel.text =
                            $"{displayName} — Giorno {s.day}, Piante in Dome {s.plantsInDome}, CRY {s.cry} — {s.timestamp}";
                    }
                    else
                    {
                        summaryLabel.text = _saveSlotsModeIsSave
                            ? $"{displayName} — Vuoto (salva qui)"
                            : $"{displayName} — Nessun salvataggio";
                    }
                }

                if (primary != null)
                {
                    if (_saveSlotsModeIsSave)
                    {
                        primary.style.display = DisplayStyle.Flex;
                        primary.SetEnabled(true);
                        if (primaryLbl != null)
                            primaryLbl.text = "Salva";
                    }
                    else
                    {
                        if (hasSave)
                        {
                            primary.style.display = DisplayStyle.Flex;
                            primary.SetEnabled(true);
                            if (primaryLbl != null)
                                primaryLbl.text = "Carica";
                        }
                        else
                        {
                            primary.style.display = DisplayStyle.None;
                        }
                    }
                }

                if (deleteBtn != null)
                {
                    var showDel = !_saveSlotsModeIsSave && hasSave;
                    deleteBtn.style.display = showDel ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void OnSaveSlotPrimary(int index)
        {
            if (index < 0 || index >= SaveManager.SlotNames.Length)
                return;
            var slotName = SaveManager.SlotNames[index];

            if (_saveSlotsModeIsSave)
                CommitSaveToSlot(slotName);
            else
                LoadFromSlot(slotName);
        }

        private void OnSaveSlotDelete(int index)
        {
            if (index < 0 || index >= SaveManager.SlotNames.Length)
                return;
            var slotName = SaveManager.SlotNames[index];
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;
            saveManager.DeleteSave(slotName);
            RefreshSaveSlotsUi();
        }

        private void CommitSaveToSlot(string slotName)
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;
            if (!saveManager.SaveGame(slotName))
                return;

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToast("SYS-003", new NotificationPayload());

            RefreshSaveSlotsUi();
        }

        private void LoadFromSlot(string slotName)
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;
            if (!saveManager.SaveExists(slotName)) return;

            SaveManager.SlotToLoadOnNextScene = slotName;
            string gameSceneName = _mainMenuOptions != null ? _mainMenuOptions.GameSceneName : null;
            if (string.IsNullOrEmpty(gameSceneName))
            {
                SaveManager.SlotToLoadOnNextScene = null;
                return;
            }

            HideSaveSlotsOverlay();
            SceneManager.LoadScene(gameSceneName);
        }

        public void ShowInGameMenu()
        {
            if (_root == null || _isMainMenuScene) return;
            _root.style.display = DisplayStyle.Flex;
        }

        public void HideInGameMenu()
        {
            if (_root == null || _isMainMenuScene) return;
            _root.style.display = DisplayStyle.None;
        }

        public void ToggleInGameMenu()
        {
            if (_root == null || _isMainMenuScene)
                return;

            if (_root.resolvedStyle.display == DisplayStyle.None || _root.style.display == DisplayStyle.None)
                ShowInGameMenu();
            else
                HideInGameMenu();
        }

        /// <summary>
        /// Popup Opzioni uGUI sotto al layer UI Toolkit: nascondiamo il Toolkit finché il popup è aperto.
        /// </summary>
        private void OpenLegacyMainMenuPopup(Action openAction)
        {
            if (openAction == null)
                return;

            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _toolkitSuppressedForLegacyPopup = true;
            }

            openAction();
        }

        private void StartNewGameLoad()
        {
            if (_isLoading) return;
            DemoSessionState.StartNextSessionAsDemo = false;
            StartCoroutine(LoadNewGameAsync());
        }

        private void StartDemoLoad()
        {
            if (_isLoading) return;
            DemoSessionState.StartNextSessionAsDemo = true;
            StartCoroutine(LoadNewGameAsync());
        }

        private IEnumerator LoadNewGameAsync()
        {
            HideSaveSlotsOverlay();

            _isLoading = true;
            SetMainMenuButtonsEnabled(false);

            if (_loadingOverlay != null)
                _loadingOverlay.style.display = DisplayStyle.Flex;

            var targetScene = string.IsNullOrWhiteSpace(_mainMenuOptions.GameSceneName)
                ? "SCN_VaultMap"
                : _mainMenuOptions.GameSceneName;

            GamePlayInstaller.SkipAutoLoad = true;
            var operation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);

            while (!operation.isDone)
            {
                var normalized = Mathf.Clamp01(operation.progress / 0.9f);
                var percentage = Mathf.RoundToInt(normalized * 100f);

                if (_loadingFill != null)
                    _loadingFill.style.width = Length.Percent(percentage);

                if (_loadingText != null)
                    _loadingText.text = $"Caricamento {percentage}%";

                yield return null;
            }
        }

        private void SetMainMenuButtonsEnabled(bool enabled)
        {
            if (_root == null) return;
            var names = new[]
            {
                "btn-new-game", "btn-load-game", "btn-demo", "btn-credits", "btn-exit", "btn-settings"
            };
            foreach (var n in names)
            {
                var b = _root.Q<Button>(n);
                if (b != null)
                    b.SetEnabled(enabled);
            }
        }
    }
}
