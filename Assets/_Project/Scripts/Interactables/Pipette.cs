using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.Lab;
using UnityEngine;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class Pipette : Storage
    {
        [Header("Lab UI — prefer Foundation UIToolkit")]
        [SerializeField] private LabFusionPanelController _labFusionPanel;
        [SerializeField] private LabPipette _labMinigame;
        
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
            if (_labFusionPanel != null)
                _labFusionPanel.Show();
            else if (_labMinigame != null)
                _labMinigame.Show();
        }
        
        public override Inventory GetInventory()
        {
            return _inventory;
        }
    }
}