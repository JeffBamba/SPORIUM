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
    public class LabExtractorPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Extractor _extractor;
        [Tooltip("Componente unico inventario (picker). Se non assegnato, viene cercato in scena.")]
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        [Header("Config")]
        [SerializeField] private int _costAction = 1;
        [Tooltip("Config Lab upgrade (modulo Cellule Staminali). Se non assegnato, caricato da Resources/LabUpgradesConfig")]
        [SerializeField] private LabUpgradesConfig _labUpgradesConfig;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _inputText;
        private Label _progressText;
        private Label _outputText;
        private Button _btnSelectInput;
        private Button _btnAvvia;
        private Button _btnRitira;
        private Button _btnClose;

        private GameManager _gameManager;
        private Inventory _storage;
        private bool _uiBound;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
                // UIDocument senza Panel Settings non renderizza; copia da un altro UIDocument in scena se mancante
                if (_uiDocument.panelSettings == null)
                {
                    var all = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                    foreach (var other in all)
                    {
                        if (other != _uiDocument && other.panelSettings != null)
                        {
                            _uiDocument.panelSettings = other.panelSettings;
                            break;
                        }
                    }
                }
                _uiDocument.sortingOrder = 400;
            }

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

            _overlay = _root.Q<VisualElement>("lab-ext-overlay");
            _inputText = _root.Q<Label>("lab-ext-input-text");
            _progressText = _root.Q<Label>("lab-ext-progress-text");
            _outputText = _root.Q<Label>("lab-ext-output-text");
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
            if (_btnAvvia != null)
            {
                _btnAvvia.SetEnabled(false);
                _btnAvvia.clicked += OnAvviaClicked;
            }
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectInput != null) _btnSelectInput.clicked += OnSelectInputClicked;
            _uiBound = true;
        }

        private void OnCloseClicked() => Hide();

        private void Start()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (_extractor != null)
                _storage = _extractor.GetInventory();
            if (_storage != null)
                _storage.OnInventoryChanged += RefreshDisplay;
            if (_labUpgradesConfig == null)
                _labUpgradesConfig = Resources.Load<LabUpgradesConfig>("LabUpgradesConfig");
            Hide();
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy && _extractor != null && _extractor.State == ExtractorProcessState.InProgress)
                RefreshDisplay();
        }

        private bool HasStemCellModule =>
            (_labUpgradesConfig != null && _labUpgradesConfig.HasStemCellModule) ||
            (_gameManager != null && _gameManager.IsStemCellModuleUnlocked);

        private void OnDestroy()
        {
            if (_storage != null)
                _storage.OnInventoryChanged -= RefreshDisplay;
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
            gameObject.SetActive(false);
        }

        private void RefreshDisplay()
        {
            string inputDesc = "—";
            bool canAvvia = false;
            if (_storage != null)
            {
                if (_storage.Has(Items.Fruits)) { inputDesc = $"{Items.Fruits} x{_storage.Items.FirstOrDefault(s => s.TypeId == Items.Fruits)?.Quantity ?? 0}"; canAvvia = true; }
                else if (HasStemCellModule && _storage.Has(Items.WholePlant)) { inputDesc = $"{Items.WholePlant} x{_storage.Items.FirstOrDefault(s => s.TypeId == Items.WholePlant)?.Quantity ?? 0}"; canAvvia = true; }
                else if (HasStemCellModule && _storage.Has(Items.OrganicScrap001)) { inputDesc = $"{Items.OrganicScrap001} x{_storage.Items.FirstOrDefault(s => s.TypeId == Items.OrganicScrap001)?.Quantity ?? 0}"; canAvvia = true; }
                else if (HasStemCellModule && _storage.Has(Items.ProteinResidue)) { inputDesc = $"{Items.ProteinResidue} x{_storage.Items.FirstOrDefault(s => s.TypeId == Items.ProteinResidue)?.Quantity ?? 0}"; canAvvia = true; }
            }
            if (_inputText != null) _inputText.text = inputDesc;

            bool inProgress = _extractor != null && _extractor.AnySlotInProgress();
            bool completed = _extractor != null && _extractor.CompletedCount() > 0;
            if (_progressText != null)
            {
                if (inProgress)
                {
                    int pct = Mathf.RoundToInt(_extractor.ExtractionProgress * 100f);
                    _progressText.text = $"Estrazione in Corso.. {pct}%";
                    _progressText.style.display = DisplayStyle.Flex;
                }
                else if (completed)
                {
                    _progressText.text = "Estrazione completata";
                    _progressText.style.display = DisplayStyle.Flex;
                }
                else
                    _progressText.style.display = DisplayStyle.None;
            }

            int outSpore = _extractor != null ? _extractor.PendingSporeCount : 0;
            int outC1 = _extractor != null ? _extractor.PendingCell001 : 0;
            int outC2 = _extractor != null ? _extractor.PendingCell002 : 0;
            int outC3 = _extractor != null ? _extractor.PendingCell003 : 0;
            var outParts = new System.Collections.Generic.List<string>();
            if (outSpore > 0) outParts.Add($"Spora Raw x{outSpore}");
            if (outC1 > 0) outParts.Add($"{Items.StemCellVegetable} x{outC1}");
            if (outC2 > 0) outParts.Add($"{Items.StemCellFungus} x{outC2}");
            if (outC3 > 0) outParts.Add($"{Items.StemCellAnimal} x{outC3}");
            if (_outputText != null) _outputText.text = outParts.Count > 0 ? string.Join(", ", outParts) : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(completed && (outSpore > 0 || outC1 > 0 || outC2 > 0 || outC3 > 0));

            bool hasFreeSlot = _extractor != null && _extractor.FreeSlotIndex() >= 0;
            if (_btnAvvia != null)
            {
                bool enableAvvia = hasFreeSlot && canAvvia && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                _btnAvvia.SetEnabled(enableAvvia);
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
            var allowed = ExtractorAllowedTypes();
            _playerInventoryPanel.ShowAsPicker(
                allowed,
                "Seleziona item da inserire nell'Extractor",
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

        private HashSet<string> ExtractorAllowedTypes()
        {
            var set = new HashSet<string> { Items.Fruits };
            if (HasStemCellModule)
            {
                set.Add(Items.WholePlant);
                set.Add(Items.OrganicScrap001);
                set.Add(Items.ProteinResidue);
            }
            return set;
        }

        private void OnAvviaClicked()
        {
            if (_extractor == null) return;
            if (_extractor.TryStartExtraction())
                RefreshDisplay();
        }

        private void OnRitiraClicked()
        {
            if (_gameManager?.PlayerInventory == null || _extractor == null) return;
            int count = _extractor.CompletedCount();
            _extractor.CollectOutput(_gameManager.PlayerInventory);
            RefreshDisplay();

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.PostToast("LAB-EXT-RITIRA", new NotificationPayload().With("count", count.ToString()));
        }
    }
}
