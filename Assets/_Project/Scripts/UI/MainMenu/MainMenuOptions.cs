using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using _Project.Sporae.Core.Installers;

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

        private void Start()
        {
            _newGameButton.onClick.AddListener(HandleNewGame);
            _continueButton?.onClick.AddListener(HandleHide);
            _optionsButton.onClick.AddListener(HandleOptions);
            _loadButton.onClick.AddListener(HandleLoad);
            if (_saveButton != null)
                _saveButton.onClick.AddListener(HandleSave);
            _quitButton.onClick.AddListener(HandleQuit);
            _hideButton?.onClick.AddListener(HandleHide);
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
    }
}