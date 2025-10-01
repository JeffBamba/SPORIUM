using UnityEngine;
using UnityEngine.UI;

namespace _Project.BlackMarket
{
    public class UIBlackMarket : MonoBehaviour
    {
        [SerializeField] private UIBlackMarketTab _sellTab;
        [SerializeField] private UIBlackMarketTab _buyTab;

        [SerializeField] private GameObject _sellSection;
        [SerializeField] private GameObject _buySection;

        [SerializeField] private Button _closeButton;
        
        private UIBlackMarketTab _currentTab;
        
        private void Awake()
        {
            _closeButton.onClick.AddListener(HandleClose);
            
            _sellTab.OnClick += HandleSellTab;
            _buyTab.OnClick += HandleBuyTab;
        }

        private void OnDestroy()
        {
            _sellTab.OnClick -= HandleSellTab;
            _buyTab.OnClick -= HandleBuyTab;
        }

        private void Start()
        {
            HandleSellTab();
        }

        private void HandleClose()
        {
            Hide();
        }
        
        private void HandleBuyTab()
        {
            _sellSection.SetActive(false);
            _buySection.SetActive(true);
            
            SelectTab(_buyTab);
        }

        private void HandleSellTab()
        {
            _sellSection.SetActive(true);
            _buySection.SetActive(false);
            
            SelectTab(_sellTab);
        }

        private void SelectTab(UIBlackMarketTab tab)
        {
            _currentTab?.Deselect();
            _currentTab = tab;
            _currentTab.Select();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}