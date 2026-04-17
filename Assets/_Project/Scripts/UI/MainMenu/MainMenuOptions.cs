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

        private void Start()
        {
            EnsureOptionsPopupController();
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

        private void EnsureOptionsPopupController()
        {
            if (_menuScreens?.OptionsPopup == null) return;
            if (_menuScreens.OptionsPopup.GetComponent<OptionsPopupController>() == null)
                _menuScreens.OptionsPopup.AddComponent<OptionsPopupController>();
        }

        private void HandleSave()
        {
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
            if (_menuScreens.IsSlotsOpen)
                _menuScreens.HideActivePopup();
            else
                _menuScreens.ShowSlotsPopup(forSave: false);
        }
        
        private void HandleOptions()
        {
            if (_menuScreens.IsOptionsOpen)
                _menuScreens.HideActivePopup();
            else
                _menuScreens.ShowOptionsPopup();
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
