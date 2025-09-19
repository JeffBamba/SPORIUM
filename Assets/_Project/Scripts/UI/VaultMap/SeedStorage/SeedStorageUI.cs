using System.Linq;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    [RequireComponent(typeof(HUDItemContainer))]
    public class SeedStorageUI : MonoBehaviour
    {
        [SerializeField] private SeedStorage _seedStorage;
        [SerializeField] private TextMeshProUGUI _capacityLabel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private HUDInventory _playerInventory;
        
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;

        private void Awake()
        {
            _hudItemContainer = GetComponent<HUDItemContainer>();
            _storage = _seedStorage.Storage;
        }

        private void Start()
        {
            _playerInventory.OnClose += HandleClose;
            _closeButton.onClick.AddListener(HandleClose);
            _storage.OnInventoryChanged += UpdateStorage;
        }

        private void HandleClose()
        {
            Hide();
        }

        private void OnDestroy()
        {
            _storage.OnInventoryChanged -= UpdateStorage;
            _playerInventory.OnClose -= HandleClose;
        }

        private void UpdateStorage()
        {
            _capacityLabel.text = $"{_storage.UniqueItems}/{_hudItemContainer.Capacity}";
            
            _hudItemContainer.DisableAllSlots();
            
            for (var i = 0; i < _storage.UniqueItems; i++)
            {
                var item = _storage.Items.ElementAt(i);
                _hudItemContainer.SetItemData(i, item.Id, item.Quantity);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateStorage();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}