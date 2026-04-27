using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Sporae.Core;
using Sporae.Core.Localization;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using _Project.Sporae.Core;
using _Project.Sporae.Core.Installers;

namespace _Project.UI.UIToolkit.MainMenu
{
    /// <summary>
    /// UI Toolkit front-end per il menu principale.
    /// Slot salvataggi/caricamento e Opzioni in UI Toolkit (stesso USS del menu).
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
        private Label _loadingContinueHint;

        private VisualElement _saveSlotsOverlay;
        private Label _saveSlotsTitle;
        private Label _saveSlotsSubtitle;
        private bool _saveSlotsVisible;
        private bool _saveSlotsModeIsSave;

        private VisualElement _optionsOverlay;
        private Label _optionsTitle;
        private Label _optionsSubtitle;
        private Label _optionsLanguageTitle;
        private Label _optionsLanguageDesc;
        private Label _optionsAudioTitle;
        private Label _optionsAudioDesc;
        private Button _btnOptionsClose;
        private Button _btnLanguageAuto;
        private Button _btnLanguageIt;
        private Button _btnLanguageEn;
        private Button _btnOpenLegacyAudio;
        private bool _optionsVisible;

        private bool _isLoading;
        private bool _isMainMenuScene;

        public bool IsRuntimeReady => _root != null;

        private void Awake()
        {
            if (_mainMenuOptions == null)
                _mainMenuOptions = GetComponent<MainMenuOptions>();
            if (_mainMenuScreens == null)
                _mainMenuScreens = GetComponent<MainMenuScreens>();
            TryBuildUiFromMenuContext();
        }

        private void OnEnable()
        {
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            ApplyLocalizedStaticText();
            if (_saveSlotsVisible)
                ShowSaveSlotsOverlay(_saveSlotsModeIsSave);
            if (_optionsVisible)
                RefreshOptionsOverlay();
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
            if (_root == null || _isLoading)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) && _optionsVisible)
            {
                HideOptionsOverlay();
                return;
            }

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
            _loadingContinueHint = _root.Q<Label>("loading-continue-hint");
            if (_loadingOverlay != null)
                _loadingOverlay.style.display = DisplayStyle.None;
            if (_loadingContinueHint != null)
                _loadingContinueHint.text = string.Empty;

            _saveSlotsOverlay = _root.Q<VisualElement>("save-slots-overlay");
            _saveSlotsTitle = _root.Q<Label>("save-slots-title");
            _saveSlotsSubtitle = _root.Q<Label>("save-slots-subtitle");
            if (_saveSlotsOverlay != null)
                _saveSlotsOverlay.style.display = DisplayStyle.None;

            _optionsOverlay = _root.Q<VisualElement>("options-overlay");
            _optionsTitle = _root.Q<Label>("options-title");
            _optionsSubtitle = _root.Q<Label>("options-subtitle");
            _optionsLanguageTitle = _root.Q<Label>("options-language-title");
            _optionsLanguageDesc = _root.Q<Label>("options-language-desc");
            _optionsAudioTitle = _root.Q<Label>("options-audio-title");
            _optionsAudioDesc = _root.Q<Label>("options-audio-desc");
            _btnOptionsClose = _root.Q<Button>("btn-options-close");
            _btnLanguageAuto = _root.Q<Button>("btn-language-auto");
            _btnLanguageIt = _root.Q<Button>("btn-language-it");
            _btnLanguageEn = _root.Q<Button>("btn-language-en");
            _btnOpenLegacyAudio = _root.Q<Button>("btn-open-legacy-audio");
            if (_optionsOverlay != null)
                _optionsOverlay.style.display = DisplayStyle.None;
            ApplyLocalizedStaticText();
        }

        private void ApplyLocalizedStaticText()
        {
            if (_root == null) return;

            SetMenuActionLabel("btn-new-game", "menu.new_game");
            SetMenuActionLabel("btn-load-game", "menu.load_game");
            SetMenuActionLabel("btn-demo", "menu.play_demo");
            SetMenuActionLabel("btn-settings-main", "menu.settings");
            SetMenuActionLabel("btn-save-main", "menu.save");
            SetMenuActionLabel("btn-exit", "menu.exit_sporium");
            RefreshMainMenuSaveButtonState();

            if (!_saveSlotsVisible)
            {
                if (_saveSlotsTitle != null)
                    _saveSlotsTitle.text = LocalizationManager.GetString("save.title.default");
                if (_saveSlotsSubtitle != null)
                    _saveSlotsSubtitle.text = LocalizationManager.GetString("save.subtitle.default");
            }

            var close = _root.Q<Button>("btn-save-slots-close");
            if (close != null)
                close.text = LocalizationManager.GetString("save.action.close");

            if (_optionsTitle != null)
                _optionsTitle.text = LocalizationManager.GetString("options.title");
            if (_optionsSubtitle != null)
                _optionsSubtitle.text = LocalizationManager.GetString("options.subtitle");
            if (_optionsLanguageTitle != null)
                _optionsLanguageTitle.text = LocalizationManager.GetString("options.language");
            if (_optionsLanguageDesc != null)
                _optionsLanguageDesc.text = LocalizationManager.GetString("options.language.description");
            if (_btnOptionsClose != null)
                _btnOptionsClose.text = LocalizationManager.GetString("save.action.close");
            if (_btnLanguageAuto != null)
                _btnLanguageAuto.text = LocalizationManager.GetString("options.language.auto");
            if (_btnLanguageIt != null)
                _btnLanguageIt.text = LocalizationManager.GetString("options.language.it");
            if (_btnLanguageEn != null)
                _btnLanguageEn.text = LocalizationManager.GetString("options.language.en");
            if (_optionsAudioTitle != null)
                _optionsAudioTitle.text = LocalizationManager.GetString("options.audio.title");
            if (_optionsAudioDesc != null)
                _optionsAudioDesc.text = LocalizationManager.GetString("options.audio.description");
            if (_btnOpenLegacyAudio != null)
                _btnOpenLegacyAudio.text = LocalizationManager.GetString("options.audio.open");
            RefreshLanguageSelection();

            for (var i = 0; i < SaveManager.SlotNames.Length; i++)
            {
                var del = _root.Q<Button>($"save-slot-delete-{i}");
                if (del != null)
                    del.text = LocalizationManager.GetString("save.action.delete");
            }
        }

        private void SetMenuActionLabel(string buttonName, string key)
        {
            var button = _root.Q<Button>(buttonName);
            var label = button?.Q<Label>(className: "menu-action-label");
            if (label != null)
                label.text = LocalizationManager.GetString(key);
        }

        private void HookButtons()
        {
            if (_root == null) return;

            var btnNewGame = _root.Q<Button>("btn-new-game");
            var btnLoadGame = _root.Q<Button>("btn-load-game");
            var btnDemo = _root.Q<Button>("btn-demo");
            var btnSettingsMain = _root.Q<Button>("btn-settings-main");
            var btnSaveMain = _root.Q<Button>("btn-save-main");
            var btnExit = _root.Q<Button>("btn-exit");
            var btnSettings = _root.Q<Button>("btn-settings");

            btnNewGame?.RegisterCallback<ClickEvent>(_ => StartNewGameLoad());
            btnLoadGame?.RegisterCallback<ClickEvent>(_ => _mainMenuOptions.OpenLoadPopupFromExternalUI());
            btnDemo?.RegisterCallback<ClickEvent>(_ => StartDemoLoad());
            btnSettingsMain?.RegisterCallback<ClickEvent>(_ => OpenOptionsOverlay());
            btnSaveMain?.RegisterCallback<ClickEvent>(_ => OpenSaveSlotsOverlay());
            btnExit?.RegisterCallback<ClickEvent>(_ => _mainMenuOptions.QuitFromExternalUI());
            btnSettings?.RegisterCallback<ClickEvent>(_ => OpenOptionsOverlay());

            WireSaveSlotsButtons();
            WireOptionsButtons();
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

        public void OpenOptionsOverlay()
        {
            if (_optionsOverlay == null)
                return;

            if (!_isMainMenuScene && _root != null && _root.style.display == DisplayStyle.None)
                ShowInGameMenu();

            HideSaveSlotsOverlay();
            RefreshOptionsOverlay();
            _optionsOverlay.style.display = DisplayStyle.Flex;
            _optionsVisible = true;
        }

        private void HideOptionsOverlay()
        {
            if (_optionsOverlay == null)
                return;
            _optionsOverlay.style.display = DisplayStyle.None;
            _optionsVisible = false;
        }

        private void RefreshOptionsOverlay()
        {
            ApplyLocalizedStaticText();
            RefreshLanguageSelection();
        }

        private void WireOptionsButtons()
        {
            _btnOptionsClose?.RegisterCallback<ClickEvent>(_ => HideOptionsOverlay());
            _btnLanguageAuto?.RegisterCallback<ClickEvent>(_ => SetLanguage(GameLanguage.Auto));
            _btnLanguageIt?.RegisterCallback<ClickEvent>(_ => SetLanguage(GameLanguage.Italian));
            _btnLanguageEn?.RegisterCallback<ClickEvent>(_ => SetLanguage(GameLanguage.English));
            _btnOpenLegacyAudio?.RegisterCallback<ClickEvent>(_ => OpenLegacyAudioPopup());
        }

        private void OpenLegacyAudioPopup()
        {
            if (_mainMenuScreens != null)
                _mainMenuScreens.ShowOptionsPopup();
        }

        private void RefreshMainMenuSaveButtonState()
        {
            if (_root == null)
                return;

            var btnSaveMain = _root.Q<Button>("btn-save-main");
            if (btnSaveMain == null)
                return;

            bool hasExistingSave = SaveManager.SlotNames.Any(slot => SaveManager.Instance != null && SaveManager.Instance.SaveExists(slot));
            bool canUseMainSave = !_isMainMenuScene || hasExistingSave;
            btnSaveMain.SetEnabled(canUseMainSave);
        }

        private void SetLanguage(GameLanguage language)
        {
            GameLanguageSettings.CurrentLanguage = language;
            NotificationLocalization.OverrideLanguage = language switch
            {
                GameLanguage.Italian => NotificationLanguage.It,
                GameLanguage.English => NotificationLanguage.En,
                _ => NotificationLanguage.Auto
            };
            RefreshOptionsOverlay();
        }

        private void RefreshLanguageSelection()
        {
            var current = GameLanguageSettings.CurrentLanguage;
            SetLanguageButtonSelected(_btnLanguageAuto, current == GameLanguage.Auto);
            SetLanguageButtonSelected(_btnLanguageIt, current == GameLanguage.Italian);
            SetLanguageButtonSelected(_btnLanguageEn, current == GameLanguage.English);
        }

        private static void SetLanguageButtonSelected(Button button, bool selected)
        {
            if (button == null)
                return;
            button.EnableInClassList("options-language-button--selected", selected);
        }

        private void ShowSaveSlotsOverlay(bool forSave)
        {
            if (_saveSlotsOverlay == null)
                return;

            if (!_isMainMenuScene && _root != null && _root.style.display == DisplayStyle.None)
                ShowInGameMenu();

            HideOptionsOverlay();
            _saveSlotsModeIsSave = forSave;
            if (_saveSlotsTitle != null)
                _saveSlotsTitle.text = LocalizationManager.GetString(forSave ? "save.title.save" : "save.title.load");
            if (_saveSlotsSubtitle != null)
            {
                _saveSlotsSubtitle.text = LocalizationManager.GetString(forSave ? "save.subtitle.save" : "save.subtitle.load");
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
                        summaryLabel.text = LocalizationManager.GetString("save.summary.filled", new Dictionary<string, string>
                        {
                            { "slot", displayName },
                            { "day", s.day.ToString() },
                            { "plants", s.plantsInDome.ToString() },
                            { "cry", s.cry.ToString() },
                            { "timestamp", s.timestamp }
                        });
                    }
                    else
                    {
                        summaryLabel.text = LocalizationManager.GetString(
                            _saveSlotsModeIsSave ? "save.summary.empty_for_save" : "save.summary.empty_for_load",
                            new Dictionary<string, string> { { "slot", displayName } });
                    }
                }

                if (primary != null)
                {
                    if (_saveSlotsModeIsSave)
                    {
                        primary.style.display = DisplayStyle.Flex;
                        primary.SetEnabled(true);
                        if (primaryLbl != null)
                            primaryLbl.text = LocalizationManager.GetString("save.action.save");
                    }
                    else
                    {
                        if (hasSave)
                        {
                            primary.style.display = DisplayStyle.Flex;
                            primary.SetEnabled(true);
                            if (primaryLbl != null)
                                primaryLbl.text = LocalizationManager.GetString("save.action.load");
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

            string gameSceneName = _mainMenuOptions != null ? _mainMenuOptions.GameSceneName : null;
            if (string.IsNullOrEmpty(gameSceneName))
            {
                return;
            }

            StartCoroutine(LoadSceneWithProgressAsync(gameSceneName, slotName));
        }

        public void ShowInGameMenu()
        {
            if (_root == null || _isMainMenuScene) return;
            GameplayUiModalLock.SetHideFixedHud(true);
            _root.style.display = DisplayStyle.Flex;
        }

        public void HideInGameMenu()
        {
            if (_root == null || _isMainMenuScene) return;
            GameplayUiModalLock.SetHideFixedHud(false);
            _root.style.display = DisplayStyle.None;
        }

        public void ToggleInGameMenu()
        {
            if (_root == null || _isMainMenuScene)
                return;

            if (_root.resolvedStyle.display == DisplayStyle.None || _root.style.display == DisplayStyle.None)
                ShowInGameMenu();
            else
            {
                HideSaveSlotsOverlay();
                HideOptionsOverlay();
                HideInGameMenu();
            }
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
            if (_loadingContinueHint != null)
                _loadingContinueHint.text = string.Empty;

            var targetScene = string.IsNullOrWhiteSpace(_mainMenuOptions.GameSceneName)
                ? "SCN_VaultMap"
                : _mainMenuOptions.GameSceneName;

            GamePlayInstaller.SkipAutoLoad = true;
            yield return LoadSceneWithProgressAsync(targetScene, null);
        }

        private IEnumerator LoadSceneWithProgressAsync(string targetScene, string slotToLoad)
        {
            _isLoading = true;
            SetMainMenuButtonsEnabled(false);
            HideSaveSlotsOverlay();
            HideOptionsOverlay();

            if (_loadingOverlay != null)
                _loadingOverlay.style.display = DisplayStyle.Flex;
            if (_loadingContinueHint != null)
                _loadingContinueHint.text = string.Empty;

            bool isLoadFromSave = !string.IsNullOrWhiteSpace(slotToLoad);
            if (isLoadFromSave)
            {
                SaveManager.SlotToLoadOnNextScene = slotToLoad;
                GamePlayInstaller.SkipAutoLoad = false;
            }
            else
            {
                GamePlayInstaller.SkipAutoLoad = true;
            }

            VaultMapEntryFade.RequestFadeInOnNextLoad = true;
            var operation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                UpdateLoadingProgress(Mathf.Clamp01(operation.progress / 0.9f), true);
                yield return null;
            }

            UpdateLoadingProgress(1f, false);
            if (_loadingContinueHint != null)
                _loadingContinueHint.text = LocalizationManager.GetString("menu.loading_continue");

            yield return WaitForAnyInput();
            operation.allowSceneActivation = true;
        }

        private void UpdateLoadingProgress(float normalized, bool inProgress)
        {
            var percentage = Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f);
            if (_loadingFill != null)
                _loadingFill.style.width = Length.Percent(percentage);

            if (_loadingText != null)
            {
                var key = inProgress ? "menu.loading_progress" : "menu.loading_ready";
                _loadingText.text = LocalizationManager.GetString(key,
                    new Dictionary<string, string> { { "percent", percentage.ToString() } });
            }
        }

        private static IEnumerator WaitForAnyInput()
        {
            while (true)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                    yield break;
                yield return null;
            }
        }

        private void SetMainMenuButtonsEnabled(bool enabled)
        {
            if (_root == null) return;
            var names = new[]
            {
                "btn-new-game", "btn-load-game", "btn-demo", "btn-exit", "btn-settings"
                , "btn-settings-main", "btn-save-main"
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
