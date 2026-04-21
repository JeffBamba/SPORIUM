using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.UI.UIToolkit.SeedStorage;
using UnityEngine;

namespace _Project
{
    /// <summary>Interactable Seed Storage Vault — stato in <see cref="_Project.Systems.SeedStorage.SeedStorageSystem"/>.</summary>
    public class SeedStorage : Storage
    {
        private readonly Inventory _legacyEmptyInventory = new();
        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            if (_interactable != null)
                _interactable.OnInteract -= HandleInteract;
        }

        private void HandleInteract()
        {
            SeedStoragePanelController.EnsureInstance()?.Show();
        }

        /// <summary>Legacy API — contenuto reale è in SeedStorageSystem.</summary>
        public override Inventory GetInventory() => _legacyEmptyInventory;
    }
}
