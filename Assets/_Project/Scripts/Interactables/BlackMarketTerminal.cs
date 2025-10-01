using _Project.BlackMarket;
using UnityEngine;

namespace _Project
{
    public class BlackMarketTerminal : MonoBehaviour
    {
        [SerializeField] private UIBlackMarket _blackMarketUI;
        
        private Interactable _interactable;
        
        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
        }
        
        private void HandleInteract()
        {
            _blackMarketUI.Show();
        }
    }
}