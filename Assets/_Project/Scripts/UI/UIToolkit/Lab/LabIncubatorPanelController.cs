using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;

namespace Sporae.UI.UIToolkit.Lab
{
    public enum ReagentChoice { None, X, Y }

    [RequireComponent(typeof(UIDocument))]
    public class LabIncubatorPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [Tooltip("Componente unico inventario (picker). Se non assegnato, viene cercato in scena.")]
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        [Header("Config")]
        [SerializeField] private int _costAction = 1;
        [SerializeField] private string _outputSeedTypeId = "seed-001";

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _preseedText;
        private Label _outputText;
        private Button _btnSelectPreseed;
        private Button _btnReagentNone;
        private Button _btnReagentX;
        private Button _btnReagentY;
        private Button _btnAvvia;
        private Button _btnRitira;
        private Button _btnClose;

        private GameManager _gameManager;
        private DayCycleSystem _dayCycleSystem;
        private ReagentChoice _reagentChoice = ReagentChoice.None;
        private int _outputSeedCount;
        private bool _incubationLaunched;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 400;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("LabIncubatorPanelController: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("lab-inc-overlay");
            _preseedText = _root.Q<Label>("lab-inc-preseed-text");
            _outputText = _root.Q<Label>("lab-inc-output-text");
            _btnSelectPreseed = _root.Q<Button>("btn-select-preseed");
            _btnReagentNone = _root.Q<Button>("btn-reagent-none");
            _btnReagentX = _root.Q<Button>("btn-reagent-x");
            _btnReagentY = _root.Q<Button>("btn-reagent-y");
            _btnAvvia = _root.Q<Button>("btn-avvia");
            _btnRitira = _root.Q<Button>("btn-ritira");
            _btnClose = _root.Q<Button>("btn-close");

            if (_playerInventoryPanel == null)
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
            if (_btnClose != null) _btnClose.clicked += Hide;
            if (_btnAvvia != null) _btnAvvia.clicked += OnAvviaClicked;
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectPreseed != null) _btnSelectPreseed.clicked += OnSelectPreseedClicked;
            if (_btnReagentNone != null) _btnReagentNone.clicked += () => SetReagent(ReagentChoice.None);
            if (_btnReagentX != null) _btnReagentX.clicked += () => SetReagent(ReagentChoice.X);
            if (_btnReagentY != null) _btnReagentY.clicked += () => SetReagent(ReagentChoice.Y);
        }

        private void Start()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;
            Hide();
        }

        private void OnDestroy()
        {
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

        private void OnSelectPreseedClicked()
        {
            if (_playerInventoryPanel == null)
            {
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
                if (_playerInventoryPanel == null) return;
            }
            var allowed = IncubatorAllowedTypes();
            _playerInventoryPanel.ShowAsPicker(
                allowed,
                "Seleziona Pre-Seed per l'Incubatore",
                typeId =>
                {
                    // L'Incubatore usa direttamente l'inventario del giocatore; la selezione serve solo a confermare/disporre il picker
                    RefreshDisplay();
                },
                () => { }
            );
        }

        private static HashSet<string> IncubatorAllowedTypes()
        {
            return new HashSet<string> { Items.PreSeed };
        }

        private void SetReagent(ReagentChoice choice)
        {
            _reagentChoice = choice;
            RefreshDisplay();
        }

        private void HandleDayChanged(int day)
        {
            if (_incubationLaunched)
            {
                _incubationLaunched = false;
                _outputSeedCount += 1;
            }
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            bool hasPreseed = _gameManager?.PlayerInventory != null && _gameManager.PlayerInventory.Has(Items.PreSeed);
            if (_preseedText != null)
                _preseedText.text = hasPreseed ? "Pre-Seed (1)" : "—";

            if (_outputText != null)
                _outputText.text = _outputSeedCount > 0 ? $"{_outputSeedTypeId} x{_outputSeedCount}" : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(_outputSeedCount > 0);

            if (_btnAvvia != null)
            {
                bool canAvvia = !_incubationLaunched && hasPreseed
                    && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                _btnAvvia.SetEnabled(canAvvia);
            }

            UpdateReagentButtons();
        }

        private void UpdateReagentButtons()
        {
            if (_btnReagentNone != null) _btnReagentNone.EnableInClassList("lab-inc-reagent-selected", _reagentChoice == ReagentChoice.None);
            if (_btnReagentX != null) _btnReagentX.EnableInClassList("lab-inc-reagent-selected", _reagentChoice == ReagentChoice.X);
            if (_btnReagentY != null) _btnReagentY.EnableInClassList("lab-inc-reagent-selected", _reagentChoice == ReagentChoice.Y);
        }

        private void OnAvviaClicked()
        {
            if (_gameManager?.PlayerInventory == null || !_gameManager.PlayerInventory.Has(Items.PreSeed))
                return;
            if (_gameManager.ActionSystem == null || _gameManager.ActionSystem.ActionsLeft < _costAction)
                return;
            if (_incubationLaunched)
                return;

            if (!_gameManager.TrySpendAction(_costAction))
                return;

            _gameManager.PlayerInventory.Consume(Items.PreSeed, 1);
            _incubationLaunched = true;
            RefreshDisplay();
        }

        private void OnRitiraClicked()
        {
            if (_outputSeedCount <= 0 || _gameManager?.PlayerInventory == null)
                return;

            int count = _outputSeedCount;
            _gameManager.PlayerInventory.Add(_outputSeedTypeId, count);
            _outputSeedCount = 0;
            RefreshDisplay();

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToast("LAB-INC-OK", new NotificationPayload().With("count", count.ToString()));
        }
    }
}
