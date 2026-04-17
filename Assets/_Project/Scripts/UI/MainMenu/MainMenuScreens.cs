using UnityEngine;

namespace _Project
{
    public class MainMenuScreens : MonoBehaviour
    {
        [SerializeField] private GameObject _optionsPopup;
        [SerializeField] private GameObject _slotsPopup;

        [SerializeField] private GameObject _page;
        [SerializeField] private bool _handleEscapeInput = false;
        
        private GameObject _activePopup = null;
        private SaveSlotsPopupController _slotsController;

        public bool IsOptionsOpen => _optionsPopup.activeSelf;
        public bool IsSlotsOpen => _slotsPopup.activeSelf;

        /// <summary>Riferimento al popup Opzioni (per aggiungere OptionsPopupController o altro).</summary>
        public GameObject OptionsPopup => _optionsPopup;

        public void ShowOptionsPopup() => ShowPopup(_optionsPopup);
        /// <param name="forSave">true = scegli slot e salva, false = scegli slot e carica</param>
        public void ShowSlotsPopup(bool forSave = false)
        {
            EnsureSlotsController();
            _slotsController?.SetSaveMode(forSave);
            ShowPopup(_slotsPopup);
        }

        private void EnsureSlotsController()
        {
            if (_slotsController != null) return;
            _slotsController = _slotsPopup.GetComponent<SaveSlotsPopupController>();
            if (_slotsController == null && _slotsPopup != null)
                _slotsController = _slotsPopup.AddComponent<SaveSlotsPopupController>();
        }

        public void HideActivePopup()
        {
            _activePopup?.SetActive(false);
        }

        private void ShowPopup(GameObject popup)
        {
            _activePopup?.SetActive(false);
            _activePopup = popup;
            _activePopup.SetActive(true);
            if (popup == _slotsPopup)
                _slotsController?.RefreshSlots();
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

        /// <summary>Stesso effetto del tasto ESC: mostra/nasconde il pannello menu in-game (Pages).</summary>
        public void ToggleMenuPage()
        {
            Toggle();
        }

        public void SetEscapeHandlingEnabled(bool enabled)
        {
            _handleEscapeInput = enabled;
        }
        
        private void Update()
        {
            if (_handleEscapeInput && Input.GetKeyDown(KeyCode.Escape))
                Toggle();
        }
    }
}