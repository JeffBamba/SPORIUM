using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.Lab;
using UnityEngine;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class Catalizzatore : Storage
    {
        [Header("Lab UI — prefer Foundation UIToolkit")]
        [SerializeField] private LabCatalizzatorePanelController _labCatalizzatorePanel;
        [SerializeField] private LabCatalizzatore _labMiniGame;
        
        private readonly Inventory _inventory = new();
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
            if (_labCatalizzatorePanel != null)
                _labCatalizzatorePanel.Show();
            else if (_labMiniGame != null)
                _labMiniGame.Show();
        }
        
        public override Inventory GetInventory()
        {
            return _inventory;
        }
    }
}