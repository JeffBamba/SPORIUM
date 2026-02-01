using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;

namespace Sporae.UI.UIToolkit.Lab
{
    [RequireComponent(typeof(UIDocument))]
    public class LabCatalizzatorePanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Catalizzatore _catalizzatore;
        [Tooltip("Componente unico inventario (picker). Se non assegnato, viene cercato in scena.")]
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        [Header("Config")]
        [SerializeField] private int _costAction = 1;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _statusLabel;
        private VisualElement _operationLabel;
        private Label _inputText;
        private Label _outputText;
        private Button _btnSelectInput;
        private Button _btnAvvia;
        private Button _btnRitira;
        private Button _btnClose;

        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        private Inventory _storage;
        private int _outputMaturedCount;
        private int _maturationState; // 0 Idle, 1 Day1, 2 Day2, 3 Ready

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 400;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("LabCatalizzatorePanelController: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("lab-cat-overlay");
            _statusLabel = _root.Q<Label>("lab-cat-status");
            _operationLabel = _root.Q<VisualElement>("lab-cat-operation-label");
            if (_operationLabel == null)
                _operationLabel = _root.Q<Label>("lab-cat-operation-label");
            _inputText = _root.Q<Label>("lab-cat-input-text");
            _outputText = _root.Q<Label>("lab-cat-output-text");
            _btnSelectInput = _root.Q<Button>("btn-select-input");
            _btnAvvia = _root.Q<Button>("btn-avvia");
            _btnRitira = _root.Q<Button>("btn-ritira");
            _btnClose = _root.Q<Button>("btn-close");

            if (_playerInventoryPanel == null)
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
            if (_btnClose != null) _btnClose.clicked += Hide;
            if (_btnAvvia != null) _btnAvvia.clicked += OnAvviaClicked;
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectInput != null) _btnSelectInput.clicked += OnSelectInputClicked;
        }

        private void Start()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
            if (_catalizzatore != null)
                _storage = _catalizzatore.GetInventory();
            if (_storage != null)
                _storage.OnInventoryChanged += RefreshDisplay;
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;
            Hide();
        }

        private void OnDestroy()
        {
            if (_storage != null)
                _storage.OnInventoryChanged -= RefreshDisplay;
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= HandleDayChanged;
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

        private void HandleDayChanged(int day)
        {
            if (_maturationState == 1)
                _maturationState = 2;
            else if (_maturationState == 2)
            {
                _maturationState = 0;
                _outputMaturedCount += 1;
            }
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_statusLabel != null)
            {
                if (_maturationState == 1)
                    _statusLabel.text = "Stato: Maturazione in corso (giorno 1)";
                else if (_maturationState == 2)
                    _statusLabel.text = "Stato: Maturazione in corso (giorno 2)";
                else if (_outputMaturedCount > 0)
                    _statusLabel.text = "Stato: Pronto — ritira spora maturata";
                else
                    _statusLabel.text = "Stato: In attesa di input (spora Raw)";
            }

            if (_operationLabel != null)
                _operationLabel.style.display = (_maturationState == 1 || _maturationState == 2) ? DisplayStyle.Flex : DisplayStyle.None;

            if (_inputText != null)
            {
                bool hasSpore = _storage != null && _storage.Has(Items.SporeGeneric);
                if (hasSpore && _storage.Items.Count > 0)
                {
                    var slot = _storage.Items.FirstOrDefault(s => s.TypeId == Items.SporeGeneric);
                    _inputText.text = slot != null ? $"{slot.TypeId} x{slot.Quantity}" : "—";
                }
                else
                    _inputText.text = "—";
            }

            if (_outputText != null)
                _outputText.text = _outputMaturedCount > 0 ? $"{Items.SporeGeneric} (maturata) x{_outputMaturedCount}" : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(_outputMaturedCount > 0);

            if (_btnAvvia != null)
            {
                bool canAvvia = _maturationState == 0 && _storage != null && _storage.Has(Items.SporeGeneric)
                    && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                _btnAvvia.SetEnabled(canAvvia);
            }
        }

        private void OnSelectInputClicked()
        {
            if (_playerInventoryPanel == null)
            {
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
                if (_playerInventoryPanel == null) return;
            }
            var allowed = CatalizzatoreAllowedTypes();
            _playerInventoryPanel.ShowAsPicker(
                allowed,
                "Seleziona spora Raw da inserire nel Catalizzatore",
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

        private static HashSet<string> CatalizzatoreAllowedTypes()
        {
            return new HashSet<string> { Items.SporeGeneric };
        }

        private void OnAvviaClicked()
        {
            if (_storage == null || !_storage.Has(Items.SporeGeneric))
                return;
            if (_gameManager == null || _gameManager.ActionSystem == null || _gameManager.ActionSystem.ActionsLeft < _costAction)
                return;
            if (_maturationState != 0)
                return;

            if (!_gameManager.TrySpendAction(_costAction))
                return;

            _storage.Consume(Items.SporeGeneric, 1);
            _maturationState = 1;
            RefreshDisplay();
        }

        private void OnRitiraClicked()
        {
            if (_outputMaturedCount <= 0 || _gameManager?.PlayerInventory == null)
                return;

            _gameManager.PlayerInventory.Add(Items.SporeGeneric, _outputMaturedCount);
            _outputMaturedCount = 0;
            RefreshDisplay();

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToast("INV-SPR", new NotificationPayload().With("amount", "1"));
        }
    }
}
