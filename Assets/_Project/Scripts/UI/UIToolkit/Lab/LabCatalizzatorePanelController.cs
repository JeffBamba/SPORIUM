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
        /// <summary>Per ogni slot: 0=vuoto, 1=giorno1, 2=giorno2, 3=pronto da ritirare. Fino a 3 processi in parallelo.</summary>
        private readonly int[] _slotStates = new int[3];
        private bool _uiBound;

        private static string CatalyserProgressToastKey(int slot) => $"catalizzatore-progress-{slot}";
        private static string CatalyserDoneToastKey(int slot) => $"catalizzatore-done-{slot}";

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 400;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();
        }

        /// <summary>Binding ritardato: UIDocument.rootVisualElement può essere null in Awake; viene eseguito in Show() al primo utilizzo. Se il GameObject è stato disattivato (Hide), l'albero viene ricreato e i riferimenti vanno aggiornati.</summary>
        private void TryBindUI()
        {
            if (_uiDocument != null)
            {
                var currentRoot = _uiDocument.rootVisualElement;
                if (currentRoot != null && currentRoot != _root)
                {
                    _root = currentRoot;
                    _uiBound = false;
                }
            }
            if (_uiBound) return;
            if (_root == null && _uiDocument != null)
                _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

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

            if (_btnClose != null)
            {
                foreach (var child in _btnClose.Children())
                    child.pickingMode = PickingMode.Ignore;
                _btnClose.clicked += OnCloseClicked;
                _btnClose.RegisterCallback<ClickEvent>(evt => { OnCloseClicked(); evt.StopPropagation(); }, TrickleDown.TrickleDown);
            }
            if (_btnAvvia != null) _btnAvvia.clicked += OnAvviaClicked;
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectInput != null) _btnSelectInput.clicked += OnSelectInputClicked;
            _uiBound = true;
        }

        private void OnCloseClicked() => Hide();

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

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation == null || !foundation.Enabled) return;
            int ready = ReadyCount();
            for (int i = 0; i < 3; i++)
            {
                if (_slotStates[i] == 1 || _slotStates[i] == 2)
                    foundation.UpsertToast(CatalyserProgressToastKey(i), "LAB-CAT-PROGRESS", new NotificationPayload().With("day", _slotStates[i].ToString()));
                else if (_slotStates[i] == 3)
                    foundation.UpsertToast(CatalyserDoneToastKey(i), "LAB-CAT-DONE", new NotificationPayload().With("count", ready.ToString()));
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            TryBindUI();
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }
            if (_root != null)
                _root.pickingMode = PickingMode.Position;
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
            bool anyInProgress = _slotStates[0] == 1 || _slotStates[0] == 2 || _slotStates[1] == 1 || _slotStates[1] == 2 || _slotStates[2] == 1 || _slotStates[2] == 2;
            bool anyReady = _slotStates[0] == 3 || _slotStates[1] == 3 || _slotStates[2] == 3;
            if (!anyInProgress && !anyReady)
                gameObject.SetActive(false);
        }

        private void HandleDayChanged(int day)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            for (int i = 0; i < 3; i++)
            {
                if (_slotStates[i] == 1)
                {
                    _slotStates[i] = 2;
                    if (foundation != null && foundation.Enabled)
                        foundation.UpsertToast(CatalyserProgressToastKey(i), "LAB-CAT-PROGRESS", new NotificationPayload().With("day", "2"));
                }
                else if (_slotStates[i] == 2)
                {
                    _slotStates[i] = 3;
                    if (foundation != null && foundation.Enabled)
                    {
                        foundation.RemoveToast(CatalyserProgressToastKey(i));
                        foundation.UpsertToast(CatalyserDoneToastKey(i), "LAB-CAT-DONE", new NotificationPayload().With("count", ReadyCount().ToString()));
                    }
                }
            }
            RefreshDisplay();
        }

        private int ReadyCount()
        {
            int n = 0;
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 3) n++;
            return n;
        }

        private bool AnySlotInProgress()
        {
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 1 || _slotStates[i] == 2) return true;
            return false;
        }

        private int FreeSlotIndex()
        {
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 0) return i;
            return -1;
        }

        private void RefreshDisplay()
        {
            int ready = ReadyCount();
            bool inProgress = AnySlotInProgress();
            int inProgressCount = 0;
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 1 || _slotStates[i] == 2) inProgressCount++;

            if (_statusLabel != null)
            {
                if (inProgressCount > 0)
                    _statusLabel.text = $"Stato: {inProgressCount} maturazione/i in corso (fino a 3)";
                else if (ready > 0)
                    _statusLabel.text = "Stato: Pronto — ritira spora/e maturata/e";
                else
                    _statusLabel.text = "Stato: In attesa di input (spora Raw). Fino a 3 in parallelo.";
            }

            if (_operationLabel != null)
                _operationLabel.style.display = inProgress ? DisplayStyle.Flex : DisplayStyle.None;

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
                _outputText.text = ready > 0 ? $"Spora maturata x{ready}" : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(ready > 0);

            bool hasFreeSlot = FreeSlotIndex() >= 0;
            if (_btnAvvia != null)
            {
                bool canAvvia = hasFreeSlot && _storage != null && _storage.Has(Items.SporeGeneric)
                    && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                _btnAvvia.SetEnabled(canAvvia);
            }

            if (_btnSelectInput != null)
                _btnSelectInput.SetEnabled(true);
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
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (_gameManager == null || _gameManager.ActionSystem == null || _gameManager.ActionSystem.ActionsLeft < _costAction)
            {
                if (foundation != null && foundation.Enabled)
                    foundation.PostToastImmediate("ACT-050");
                return;
            }
            int idx = FreeSlotIndex();
            if (idx < 0)
                return;

            if (!_gameManager.TrySpendAction(_costAction))
                return;

            _storage.Consume(Items.SporeGeneric, 1);
            _slotStates[idx] = 1;
            if (foundation != null && foundation.Enabled)
                foundation.UpsertToast(CatalyserProgressToastKey(idx), "LAB-CAT-PROGRESS", new NotificationPayload().With("day", "1"));
            RefreshDisplay();
        }

        private void OnRitiraClicked()
        {
            int ready = ReadyCount();
            if (ready <= 0 || _gameManager?.PlayerInventory == null)
                return;

            _gameManager.PlayerInventory.AddSporeMatured(ready);
            for (int i = 0; i < 3; i++)
            {
                if (_slotStates[i] == 3)
                {
                    _slotStates[i] = 0;
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                        foundation.RemoveToast(CatalyserDoneToastKey(i));
                }
            }
            RefreshDisplay();

            var foundationRitira = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundationRitira != null && foundationRitira.Enabled)
                foundationRitira.PostToast("LAB-CAT-RITIRA", new NotificationPayload().With("amount", ready.ToString()));
        }
    }
}
