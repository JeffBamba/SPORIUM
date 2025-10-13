using _Project.Sporae.Core;
using UnityEngine;

namespace _Project
{
    [RequireComponent(typeof(Interactable))]
    public class Pipette : Storage
    {
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
            _labMinigame.Show();
        }
        
        public override Inventory GetInventory()
        {
            return _inventory;
        }
    }
}