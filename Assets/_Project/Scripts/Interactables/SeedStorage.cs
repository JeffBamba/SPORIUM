using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;

namespace _Project
{
    public class SeedStorage : Storage
    {
        [SerializeField] private HUDInventory _inventoryUI;
        [SerializeField] private SeedStorageUI _seedStorageUI;
       
        private readonly Inventory _inventory = new();
        private Interactable _interactable;
        
        public Inventory Storage => _inventory;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
        }
        
        private void HandleInteract() {
            _inventoryUI.Show();
            _seedStorageUI.Show();
        }
        
        public override Inventory GetInventory()
        {
            return _inventory;
        }
    }
}