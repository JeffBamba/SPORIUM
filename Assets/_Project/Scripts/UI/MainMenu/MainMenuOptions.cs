using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using _Project.Sporae.Core.Installers;
using _Project.UI.UIToolkit.MainMenu;

namespace _Project
{
    public class MainMenuOptions : MonoBehaviour
    {
        [SerializeField] private MainMenuScreens _menuScreens;

        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _hideButton;

        [SerializeField] private string _newGameSceneName;

        /// <summary>Nome della scena di gioco (usato da Nuova Partita e da Carica per entrare in partita).</summary>
        public string GameSceneName => _newGameSceneName;

        /// <summary>Espone il riferimento ai popup manager del menu.</summary>
        public MainMenuScreens MenuScreens => _menuScreens;

        private void Awake()
        {
            // Prima di qualsiasi Start (es. CompactBottomBar): il Toolkit deve esistere e aver costruito l’UI.
            EnsureMainMenuUiToolkitController();
        }

        private void Start()
        {
            EnsureMainMenuUiToolkitController();

            _newGameButton.onClick.AddListener(HandleNewGame);
            _continueButton?.onClick.AddListener(HandleHide);
            _optionsButton.onClick.AddListener(HandleOptions);
            _loadButton.onClick.AddListener(HandleLoad);
            if (_saveButton != null)
                _saveButton.onClick.AddListener(HandleSave);
            _quitButton.onClick.AddListener(HandleQuit);
            _hideButton?.onClick.AddListener(HandleHide);
        }

        private void EnsureMainMenuUiToolkitController()
        {
            if (GetComponent<MainMenuUIToolkitController>() == null)
                gameObject.AddComponent<MainMenuUIToolkitController>();
        }

        /// <summary>
        /// Il menu Toolkit può stare sullo stesso GO del Menu o su un host creato da HUD (CompactBottomBar).
        /// <see cref="GetComponent{T}"/> da solo punta solo al primo caso.
        /// </summary>
        private MainMenuUIToolkitController ResolveMainMenuToolkit()
        {
            var local = GetComponent<MainMenuUIToolkitController>();
            if (local != null && local.IsRuntimeReady)
                return local;

            var all = UnityEngine.Object.FindObjectsByType<MainMenuUIToolkitController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t != null && t.IsRuntimeReady && t.gameObject.activeInHierarchy)
                    return t;
            }
            foreach (var t in all)
            {
                if (t != null && t.IsRuntimeReady)
                    return t;
            }
            return local;
        }

        private void HandleSave()
        {
            var toolkit = ResolveMainMenuToolkit();
            if (toolkit != null && toolkit.IsRuntimeReady)
            {
                toolkit.OpenSaveSlotsOverlay();
                return;
            }

            if (_menuScreens.IsSlotsOpen)
                _menuScreens.HideActivePopup();
            else
                _menuScreens.ShowSlotsPopup(forSave: true);
        }

        private void HandleHide()
        {
            _menuScreens.Hide();
        }

        private void HandleLoad()
        {
            var toolkit = ResolveMainMenuToolkit();
            if (toolkit != null && toolkit.IsRuntimeReady)
            {
                toolkit.OpenLoadSlotsOverlay();
                return;
            }

            if (_menuScreens.IsSlotsOpen)
                _menuScreens.HideActivePopup();
            else
                _menuScreens.ShowSlotsPopup(forSave: false);
        }
        
        private void HandleOptions()
        {
            var toolkit = ResolveMainMenuToolkit();
            if (toolkit != null && toolkit.IsRuntimeReady)
            {
                toolkit.ShowInGameMenu();
                toolkit.OpenOptionsOverlay();
                return;
            }

            Debug.LogWarning("[MainMenuOptions] MainMenuUIToolkitController non pronto: impossibile aprire Opzioni.");
        }
        
        private void HandleNewGame()
        {
            GamePlayInstaller.SkipAutoLoad = true;
            SceneManager.LoadScene(_newGameSceneName);
        }

        private void HandleQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OpenLoadPopupFromExternalUI()
        {
            HandleLoad();
        }

        public void OpenOptionsPopupFromExternalUI()
        {
            HandleOptions();
        }

        public void QuitFromExternalUI()
        {
            HandleQuit();
        }

        public void SetLegacyButtonsVisible(bool visible)
        {
            SetButtonVisible(_newGameButton, visible);
            SetButtonVisible(_continueButton, visible);
            SetButtonVisible(_loadButton, visible);
            SetButtonVisible(_saveButton, visible);
            SetButtonVisible(_optionsButton, visible);
            SetButtonVisible(_quitButton, visible);
            SetButtonVisible(_hideButton, visible);
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
        }
    }
}
