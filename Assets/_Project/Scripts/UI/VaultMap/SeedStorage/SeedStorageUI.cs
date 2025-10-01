using System.Linq;

using _Project.Sporae.Core;

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

        [SerializeField] private string _incorrectItemMessage;
        
        private DragDropUI _dragDropUI;
        private Inventory _storage;
        private HUDItemContainer _hudItemContainer;
        private UINotification _notification;
        
        private void Awake()
        {
            _notification = FindObjectOfType<UINotification>();
            _dragDropUI = GetComponent<DragDropUI>();
            _hudItemContainer = GetComponent<HUDItemContainer>();
            _storage = _seedStorage.Storage;
        }

        private void Start()
        {
            _dragDropUI.OnInCorrectItem += HandleIncorrectItem;
            _dragDropUI.OnConfirm += HandleConfirm;
            _playerInventory.OnClose += HandleClose;
            _storage.OnInventoryChanged += UpdateStorage;
            _closeButton.onClick.AddListener(HandleClose);
        }

        private void HandleIncorrectItem(string obj)
        {
            _notification.ShowNotification(_incorrectItemMessage, 2, Color.red);
        }

        private void HandleConfirm()
        {
            Hide();
        }

        private void HandleClose()
        {
            Hide();
        }

        private void OnDestroy()
        {
            _dragDropUI.OnInCorrectItem -= HandleIncorrectItem;
            _dragDropUI.OnConfirm -= HandleConfirm;
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
                _hudItemContainer.SetItemData(i, item.TypeId, item.Quantity);
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