using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project
{
    public class MainMenuOptions : MonoBehaviour
    {
        [SerializeField] private MainMenuScreens _menuScreens;
        
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;

        [SerializeField] private string _newGameSceneName;
        
        private void Start()
        {
            _newGameButton.onClick.AddListener(HandleNewGame);
            _optionsButton.onClick.AddListener(HandleOptions);
            _loadButton.onClick.AddListener(HandleLoad);
            _quitButton.onClick.AddListener(HandleQuit);
        }

        private void HandleLoad()
        {
            if (_menuScreens.IsSlotsOpen)
                _menuScreens.HideActivePopup();
            else 
                _menuScreens.ShowSlotsPopup();
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