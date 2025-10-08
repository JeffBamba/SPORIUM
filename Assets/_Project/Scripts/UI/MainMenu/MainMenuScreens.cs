using UnityEngine;

namespace _Project
{
    public class MainMenuScreens : MonoBehaviour
    {
        [SerializeField] private GameObject _optionsPopup;
        [SerializeField] private GameObject _slotsPopup;

        [SerializeField] private GameObject _page;
        
        private GameObject _activePopup = null;
        
        public bool IsOptionsOpen => _optionsPopup.activeSelf;
        public bool IsSlotsOpen => _slotsPopup.activeSelf;

        public void ShowOptionsPopup() => ShowPopup(_optionsPopup);
        public void ShowSlotsPopup() => ShowPopup(_slotsPopup);
        
        public void HideActivePopup()
        {
            _activePopup?.SetActive(false);
        }

        private void ShowPopup(GameObject popup)
        {
            _activePopup?.SetActive(false);
            _activePopup = popup;
            _activePopup.SetActive(true);   
        }

        private void Show()
        {
            _page.SetActive(true);
        }

        public void Hide()
        {
            _page.SetActive(false);
        }

        private void Toggle()
        {
            if (_page.activeInHierarchy)
                Hide();
            else 
                Show();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
        }
    }
}