using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;

namespace Sporae.UI.UIToolkit.Lab
{
    [RequireComponent(typeof(UIDocument))]
    public class LabFusionPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Pipette _pipette;
        [Tooltip("Componente unico inventario (picker). Se non assegnato, viene cercato in scena.")]
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        [Header("Config")]
        [SerializeField] private int _costAction = 1;
        [SerializeField] private int _sporesRequired = 2;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _inputText;
        private Label _outputText;
        private Button _btnSelectInput;
        private Button _btnFusion;
        private Button _btnRitira;
        private Button _btnClose;

        private GameManager _gameManager;
        private Inventory _storage;
        private int _outputPreSeedCount;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 400;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("LabFusionPanelController: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("lab-fus-overlay");
            _inputText = _root.Q<Label>("lab-fus-input-text");
            _outputText = _root.Q<Label>("lab-fus-output-text");
            _btnSelectInput = _root.Q<Button>("btn-select-input");
            _btnFusion = _root.Q<Button>("btn-fusion");
            _btnRitira = _root.Q<Button>("btn-ritira");
            _btnClose = _root.Q<Button>("btn-close");

            if (_playerInventoryPanel == null)
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
            if (_btnClose != null) _btnClose.clicked += Hide;
            if (_btnFusion != null) _btnFusion.clicked += OnFusionClicked;
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectInput != null) _btnSelectInput.clicked += OnSelectInputClicked;
        }

        private void Start()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (_pipette != null)
                _storage = _pipette.GetInventory();
            if (_storage != null)
                _storage.OnInventoryChanged += RefreshDisplay;
            Hide();
        }

        private void OnDestroy()
        {
            if (_storage != null)
                _storage.OnInventoryChanged -= RefreshDisplay;
        }

        public void Show()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }
            if (_root != null)
                _root.pickingMode = PickingMode.Position;
            gameObject.SetActive(true);
            RefreshDisplay();
        }

        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }
            if (_root != null)
                _root.pickingMode = PickingMode.Ignore;
            gameObject.SetActive(false);
        }

        private void RefreshDisplay()
        {
            if (_inputText != null)
            {
                int sporeQty = _storage != null && _storage.Has(Items.SporeGeneric)
                    ? _storage.Items.FirstOrDefault(s => s.TypeId == Items.SporeGeneric)?.Quantity ?? 0
                    : 0;
                _inputText.text = sporeQty > 0 ? $"{Items.SporeGeneric} x{sporeQty}" : "—";
            }

            if (_outputText != null)
                _outputText.text = _outputPreSeedCount > 0 ? $"Pre-Seed x{_outputPreSeedCount}" : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(_outputPreSeedCount > 0);

            if (_btnFusion != null)
            {
                int sporeQty = _storage != null && _storage.Has(Items.SporeGeneric)
                    ? _storage.Items.FirstOrDefault(s => s.TypeId == Items.SporeGeneric)?.Quantity ?? 0
                    : 0;
                bool canFuse = sporeQty >= _sporesRequired
                    && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                _btnFusion.SetEnabled(canFuse);
            }
        }

        private void OnSelectInputClicked()
        {
            if (_playerInventoryPanel == null)
            {
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
                if (_playerInventoryPanel == null) return;
            }
            var allowed = FusionAllowedTypes();
            _playerInventoryPanel.ShowAsPicker(
                allowed,
                "Seleziona spora matura da inserire nella Pipette (Fusione)",
                typeId =>
                {
                    if (_gameManager?.PlayerInventory == null || _storage == null) return;
                    if (_gameManager.PlayerInventory.Consume(typeId, 1))
                    {
                        _storage.Add(typeId);
                        RefreshDisplay();
                    }
                },
                () => { }
            );
        }

        private static HashSet<string> FusionAllowedTypes()
        {
            return new HashSet<string> { Items.SporeGeneric };
        }

        private void OnFusionClicked()
        {
            if (_storage == null || !_storage.Has(Items.SporeGeneric, _sporesRequired))
                return;
            if (_gameManager == null || _gameManager.ActionSystem == null || _gameManager.ActionSystem.ActionsLeft < _costAction)
                return;

            if (!_gameManager.TrySpendAction(_costAction))
                return;

            _storage.Consume(Items.SporeGeneric, _sporesRequired);
            _outputPreSeedCount += 1;
            RefreshDisplay();
        }

        private void OnRitiraClicked()
        {
            if (_outputPreSeedCount <= 0 || _gameManager?.PlayerInventory == null)
                return;

            _gameManager.PlayerInventory.Add(Items.SporeGeneric, _outputPreSeedCount);
            _outputPreSeedCount = 0;
            RefreshDisplay();

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToast("LAB-GRF-OK", new NotificationPayload().With("seedCode", Items.SporeGeneric));
        }
    }
}
