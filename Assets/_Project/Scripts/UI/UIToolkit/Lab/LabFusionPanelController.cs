using System.Collections;
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
        [SerializeField] private float _fusionDurationSeconds = 120f;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _slot1Text;
        private Label _slot2Text;
        private Label _progressText;
        private Label _outputText;
        private VisualElement _outputSlotRow;
        private Button _btnSelectSlot1;
        private Button _btnSelectSlot2;
        private Button _btnFusion;
        private Button _btnRitira;
        private Button _btnClose;
        private VisualElement _outputTooltip;
        private Label _outputTooltipText;

        private GameManager _gameManager;
        private Inventory _storage;
        private readonly List<Item> _outputPreSeeds = new();
        private bool _uiBound;
        private bool _fusionInProgress;
        private float _fusionProgress;
        private Coroutine _fusionCoroutine;
        private Item _pendingFusionResult;

        private const string FusionProgressToastKey = "pipette-fusion-progress";
        private const string FusionDoneToastKey = "pipette-fusion-done";

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
            {
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

        private void TryBindUI()
        {
            if (_uiDocument != null)
            {
                var currentRoot = _uiDocument.rootVisualElement;
                if (currentRoot != null && currentRoot != _root)
                {
                    _root = currentRoot;
                    _outputTooltip = null;
                    _outputTooltipText = null;
                    _uiBound = false;
                }
            }
            if (_uiBound) return;
            if (_root == null && _uiDocument != null)
                _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _overlay = _root.Q<VisualElement>("lab-fus-overlay");
            _slot1Text = _root.Q<Label>("lab-fus-slot1-text");
            _slot2Text = _root.Q<Label>("lab-fus-slot2-text");
            _progressText = _root.Q<Label>("lab-fus-progress-text");
            _outputText = _root.Q<Label>("lab-fus-output-text");
            _outputSlotRow = _root.Q<VisualElement>("lab-fus-output-row");
            _btnSelectSlot1 = _root.Q<Button>("btn-select-slot1");
            _btnSelectSlot2 = _root.Q<Button>("btn-select-slot2");
            _btnFusion = _root.Q<Button>("btn-fusion");
            _btnRitira = _root.Q<Button>("btn-ritira");
            _btnClose = _root.Q<Button>("btn-close");
            if (_playerInventoryPanel == null)
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();

            EnsureOutputTooltip();

            if (_btnClose != null)
            {
                foreach (var child in _btnClose.Children())
                    child.pickingMode = PickingMode.Ignore;
                _btnClose.clicked += OnCloseClicked;
                _btnClose.RegisterCallback<ClickEvent>(evt => { OnCloseClicked(); evt.StopPropagation(); }, TrickleDown.TrickleDown);
            }
            if (_btnFusion != null) _btnFusion.clicked += OnFusionClicked;
            if (_btnRitira != null) _btnRitira.clicked += OnRitiraClicked;
            if (_btnSelectSlot1 != null) _btnSelectSlot1.clicked += () => OnSelectSlotClicked(1);
            if (_btnSelectSlot2 != null) _btnSelectSlot2.clicked += () => OnSelectSlotClicked(2);
            _uiBound = true;
        }

        private void OnCloseClicked() => Hide();

        private void EnsureOutputTooltip()
        {
            if (_outputTooltip != null || _root == null) return;
            _outputTooltip = new VisualElement();
            _outputTooltip.name = "lab-fus-output-tooltip";
            _outputTooltip.style.position = Position.Absolute;
            _outputTooltip.style.display = DisplayStyle.None;
            _outputTooltip.style.backgroundColor = new Color(0.05f, 0.07f, 0.09f, 0.96f);
            _outputTooltip.style.borderTopWidth = _outputTooltip.style.borderRightWidth = _outputTooltip.style.borderBottomWidth = _outputTooltip.style.borderLeftWidth = 2f;
            _outputTooltip.style.borderTopColor = _outputTooltip.style.borderRightColor = _outputTooltip.style.borderBottomColor = _outputTooltip.style.borderLeftColor = new Color(0.5f, 0.8f, 0.5f, 0.9f);
            _outputTooltip.style.paddingTop = _outputTooltip.style.paddingRight = _outputTooltip.style.paddingBottom = _outputTooltip.style.paddingLeft = 10f;
            _outputTooltip.style.minWidth = 260f;
            _outputTooltip.style.maxWidth = 340f;
            _outputTooltip.pickingMode = PickingMode.Ignore;
            _outputTooltipText = new Label();
            _outputTooltipText.enableRichText = true;
            _outputTooltipText.style.whiteSpace = WhiteSpace.Normal;
            _outputTooltipText.style.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            _outputTooltipText.style.fontSize = 12f;
            _outputTooltip.Add(_outputTooltipText);
            _root.Add(_outputTooltip);

            if (_outputSlotRow != null)
            {
                _outputSlotRow.RegisterCallback<MouseEnterEvent>(OnOutputSlotHoverEnter);
                _outputSlotRow.RegisterCallback<MouseLeaveEvent>(OnOutputSlotHoverExit);
                _outputSlotRow.RegisterCallback<MouseMoveEvent>(OnOutputSlotHoverMove);
            }
        }

        private void OnOutputSlotHoverEnter(MouseEnterEvent evt)
        {
            if (_outputTooltip == null || _outputTooltipText == null || _outputPreSeeds.Count <= 0) return;
            _outputTooltipText.text = BuildOutputTooltipText();
            _outputTooltip.style.display = DisplayStyle.Flex;
            _outputTooltip.BringToFront();
            PositionOutputTooltipAtMouse(evt.mousePosition);
        }

        private void OnOutputSlotHoverExit(MouseLeaveEvent evt)
        {
            if (_outputTooltip != null)
                _outputTooltip.style.display = DisplayStyle.None;
        }

        private void OnOutputSlotHoverMove(MouseMoveEvent evt)
        {
            if (_outputTooltip == null || _outputTooltip.style.display != DisplayStyle.Flex) return;
            PositionOutputTooltipAtMouse(evt.mousePosition);
        }

        private void PositionOutputTooltipAtMouse(Vector2 mousePosPanel)
        {
            if (_outputTooltip == null || _root == null) return;
            float x = mousePosPanel.x + 16f;
            float y = mousePosPanel.y + 12f;
            const float tw = 320f;
            float th = _outputTooltip.resolvedStyle.height;
            var bounds = _root.contentRect;
            if (x + tw > bounds.width) x = mousePosPanel.x - tw - 8f;
            if (y + th > bounds.height) y = mousePosPanel.y - th - 8f;
            if (y < 0f) y = 8f;
            if (x < 0f) x = 8f;
            _outputTooltip.style.left = x;
            _outputTooltip.style.top = y;
        }

        private string BuildOutputTooltipText()
        {
            var first = _outputPreSeeds[0];
            string tratti = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(first.GeneticTypeValue);
            if (string.IsNullOrEmpty(tratti) || tratti == "—") tratti = "Stabili";
            string fa = string.IsNullOrWhiteSpace(first.ParentFamilyA) ? "STANDARD" : first.ParentFamilyA;
            string fb = string.IsNullOrWhiteSpace(first.ParentFamilyB) ? "STANDARD" : first.ParentFamilyB;
            var lines = new List<string>
            {
                $"Pre-Seed x{ExtractorTooltipTexts.WrapValue(_outputPreSeeds.Count.ToString())}",
                $"Tratti (fissati Step 3): {ExtractorTooltipTexts.WrapValue(tratti)}",
                $"Famiglie sorgente: {ExtractorTooltipTexts.WrapValue(fa)} + {ExtractorTooltipTexts.WrapValue(fb)}"
            };
            if (!string.IsNullOrWhiteSpace(first.CandidateTraitsCsv))
                lines.Add($"Tratti compatibili: {ExtractorTooltipTexts.WrapValue(first.CandidateTraitsCsv)}");
            return string.Join("\n", lines);
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
            if (_fusionCoroutine != null)
                StopCoroutine(_fusionCoroutine);
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy && _fusionInProgress)
                RefreshDisplay();
            if (gameObject.activeInHierarchy && _outputPreSeeds.Count > 0)
            {
                var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundation != null && foundation.Enabled)
                    foundation.UpsertToast(FusionDoneToastKey, "LAB-FUS-DONE", new NotificationPayload().With("count", _outputPreSeeds.Count.ToString()));
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
            if (!_fusionInProgress && _outputPreSeeds.Count == 0)
                gameObject.SetActive(false);
        }

        private int GetMaturedCount()
        {
            if (_storage == null || !_storage.Has(Items.SporeGeneric))
                return 0;
            var slot = _storage.Items.FirstOrDefault(s => s.TypeId == Items.SporeGeneric);
            if (slot == null) return 0;
            return slot.Items.Count(i => i.SporeStageValue == SporeStage.Matured);
        }

        private void RefreshDisplay()
        {
            int count = GetMaturedCount();

            if (_slot1Text != null)
                _slot1Text.text = count >= 1 ? "Spora Maturata" : "—";
            if (_slot2Text != null)
                _slot2Text.text = count >= 2 ? "Spora Maturata" : "—";

            if (_progressText != null)
            {
                if (_fusionInProgress)
                {
                    int pct = Mathf.RoundToInt(_fusionProgress * 100f);
                    _progressText.text = $"Fusione in corso.. {pct}%";
                    _progressText.style.display = DisplayStyle.Flex;
                }
                else
                    _progressText.style.display = DisplayStyle.None;
            }

            if (_outputText != null)
                _outputText.text = _outputPreSeeds.Count > 0 ? $"Pre-Seed x{_outputPreSeeds.Count}" : "—";

            if (_btnRitira != null)
                _btnRitira.SetEnabled(_outputPreSeeds.Count > 0);

            if (_btnSelectSlot1 != null)
                _btnSelectSlot1.SetEnabled(!_fusionInProgress && count < 1);
            if (_btnSelectSlot2 != null)
                _btnSelectSlot2.SetEnabled(!_fusionInProgress && count == 1);

            if (_btnFusion != null)
            {
                bool canFuse = !_fusionInProgress && count >= 2
                    && _gameManager != null && _gameManager.ActionSystem != null && _gameManager.ActionSystem.ActionsLeft >= _costAction;
                _btnFusion.SetEnabled(canFuse);
            }
        }

        private void OnSelectSlotClicked(int slotNumber)
        {
            int count = GetMaturedCount();
            if (slotNumber == 1 && count >= 1) return;
            if (slotNumber == 2 && count != 1) return;

            if (_playerInventoryPanel == null)
            {
                _playerInventoryPanel = FindObjectOfType<PlayerInventoryPanelController>();
                if (_playerInventoryPanel == null) return;
            }
            var allowed = new HashSet<string> { Items.SporeGeneric };
            _playerInventoryPanel.ShowAsPicker(
                allowed,
                "Seleziona una Spora Maturata (obbligatoria per questo slot)",
                (typeId, stage) =>
                {
                    if (typeId != Items.SporeGeneric || stage != SporeStage.Matured) return;
                    if (_gameManager?.PlayerInventory == null || _storage == null) return;
                    if (!_gameManager.PlayerInventory.TryRemoveFirstSporeByStage(SporeStage.Matured, out var pickedSpore)) return;
                    _storage.Add(pickedSpore);
                    RefreshDisplay();
                },
                () => { },
                SporeStage.Matured
            );
        }

        private void OnFusionClicked()
        {
            if (_storage == null) return;
            if (GetMaturedCount() < 2) return;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (_gameManager == null || _gameManager.ActionSystem == null || _gameManager.ActionSystem.ActionsLeft < _costAction)
            {
                if (foundation != null && foundation.Enabled)
                    foundation.PostToastImmediate("ACT-050");
                return;
            }
            if (!_gameManager.TrySpendAction(_costAction))
                return;
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordLabAction("Fusion");
            if (!_storage.TryRemoveFirstSporeByStage(SporeStage.Matured, out var sporeA))
                return;
            if (!_storage.TryRemoveFirstSporeByStage(SporeStage.Matured, out var sporeB))
            {
                _storage.Add(sporeA);
                return;
            }
            _pendingFusionResult = ItemFabric.CreatePreSeedFromSpores(sporeA, sporeB);
            if (_pendingFusionResult == null)
            {
                _storage.Add(sporeA);
                _storage.Add(sporeB);
                return;
            }

            _fusionInProgress = true;
            if (foundation != null && foundation.Enabled)
                foundation.UpsertToast(FusionProgressToastKey, "LAB-FUS-PROGRESS", new NotificationPayload().With("percent", "0"));
            _fusionCoroutine = StartCoroutine(RunFusion());
            RefreshDisplay();
            // Non chiudiamo il pannello qui: la coroutine RunFusion gira su questo GameObject e si interromperebbe se lo disattiviamo.
        }

        private IEnumerator RunFusion()
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            float elapsed = 0f;
            while (elapsed < _fusionDurationSeconds)
            {
                elapsed += Time.deltaTime;
                _fusionProgress = Mathf.Clamp01(elapsed / _fusionDurationSeconds);
                if (foundation != null && foundation.Enabled)
                {
                    int pct = Mathf.RoundToInt(_fusionProgress * 100f);
                    foundation.UpsertToast(FusionProgressToastKey, "LAB-FUS-PROGRESS", new NotificationPayload().With("percent", pct.ToString()));
                }
                yield return null;
            }
            _fusionProgress = 1f;
            _fusionInProgress = false;
            _fusionCoroutine = null;
            if (_pendingFusionResult != null)
                _outputPreSeeds.Add(_pendingFusionResult);
            _pendingFusionResult = null;
            if (foundation != null && foundation.Enabled)
            {
                foundation.RemoveToast(FusionProgressToastKey);
                foundation.UpsertToast(FusionDoneToastKey, "LAB-FUS-DONE", new NotificationPayload().With("count", _outputPreSeeds.Count.ToString()));
            }
            RefreshDisplay();
        }

        private void OnRitiraClicked()
        {
            if (_outputPreSeeds.Count <= 0 || _gameManager?.PlayerInventory == null)
                return;

            int amount = _outputPreSeeds.Count;
            foreach (var preSeed in _outputPreSeeds)
                _gameManager.PlayerInventory.Add(preSeed);
            _outputPreSeeds.Clear();
            RefreshDisplay();

            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
            {
                foundation.RemoveToast(FusionDoneToastKey);
                foundation.PostToast("LAB-FUS-RITIRA", new NotificationPayload().With("count", amount.ToString()));
            }
        }
    }
}
