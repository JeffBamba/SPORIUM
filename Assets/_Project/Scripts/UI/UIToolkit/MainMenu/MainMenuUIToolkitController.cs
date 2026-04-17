using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using _Project.Sporae.Core.Installers;

namespace _Project.UI.UIToolkit.MainMenu
{
    /// <summary>
    /// UI Toolkit front-end per il menu principale.
    /// Riusa i controller/popup esistenti per Load e Opzioni.
    /// </summary>
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

        private bool _isLoading;
        private bool _isMainMenuScene;
        public bool IsRuntimeReady => _root != null;

        private void Awake()
        {
            if (_mainMenuOptions == null)
                _mainMenuOptions = GetComponent<MainMenuOptions>();
            if (_mainMenuScreens == null)
                _mainMenuScreens = GetComponent<MainMenuScreens>();
        }

        public void InjectRuntimeReferences(MainMenuOptions mainMenuOptions, MainMenuScreens mainMenuScreens)
        {
            _mainMenuOptions = mainMenuOptions;
            _mainMenuScreens = mainMenuScreens;
        }

        private void Start()
        {
            if (_mainMenuOptions == null)
                return;

            _isMainMenuScene = SceneManager.GetActiveScene().name == MainMenuSceneName;
            BuildMenuUiToolkit();
            HookButtons();

            // Nasconde i vecchi bottoni uGUI, ma lascia i popup esistenti disponibili.
            _mainMenuOptions.SetLegacyButtonsVisible(false);

            // Disattiva ESC legacy (vecchio menu uGUI Pages).
            _mainMenuScreens?.SetEscapeHandlingEnabled(false);

            // In gioco il nuovo menu parte chiuso e si apre con ESC/Settings.
            if (!_isMainMenuScene)
                HideInGameMenu();
        }

        private void Update()
        {
            if (_isMainMenuScene || _isLoading || _root == null)
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
            _document.sortingOrder = 500;

            _root = _document.rootVisualElement;
            _loadingOverlay = _root.Q<VisualElement>("loading-overlay");
            _loadingFill = _root.Q<VisualElement>("loading-progress-fill");
            _loadingText = _root.Q<Label>("loading-text");
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
            btnExit?.RegisterCallback<ClickEvent>(_ => _mainMenuOptions.QuitFromExternalUI());
            btnSettings?.RegisterCallback<ClickEvent>(_ =>
            {
                if (_isMainMenuScene)
                    _mainMenuOptions.OpenOptionsPopupFromExternalUI();
                else
                    ToggleInGameMenu();
            });

            if (btnDemo != null)
                btnDemo.SetEnabled(false);
            if (btnCredits != null)
                btnCredits.SetEnabled(false);
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

        private void StartNewGameLoad()
        {
            if (_isLoading) return;
            StartCoroutine(LoadNewGameAsync());
        }

        private IEnumerator LoadNewGameAsync()
        {
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
