using System;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit.PotActionsMenu
{
    [RequireComponent(typeof(UIDocument))]
    public class PotActionsMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Sporae.UI.UIToolkit.SeedInventory.SeedInventoryMenu _seedInventoryMenu;
        [SerializeField] private Sporae.UI.UIToolkit.IrrigationDialog.IrrigationDialog _irrigationDialog;
        [SerializeField] private Sporae.UI.UIToolkit.PlantCard.PlantCardV2Opener _plantCardOpener;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _panel;
        private VisualElement _list;
        private Label _statusLabel;
        private Button _btnClose;
        private Button _btnPlant;
        private Button _btnInspect;
        private Button _btnHarvest;
        private Button _btnRemove;

        private PotSlot _currentPot;
        private string _pendingSeedTypeId;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder sia su UIDocument che su Canvas parent (se presente)
            // PotActionsMenu deve stare sopra PlantCard (300) ma sotto selector modali (500)
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 400;
                
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 400;
                }
            }

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("PotActionsMenu: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("pot-ops-overlay");
            _panel = _root.Q<VisualElement>("pot-ops-panel");
            _list = _root.Q<VisualElement>("pot-ops-list");
            _statusLabel = _root.Q<Label>("pot-ops-status-label");
            _btnClose = _root.Q<Button>("btn-close");
            _btnPlant = _root.Q<Button>("btn-plant");
            _btnInspect = _root.Q<Button>("btn-inspect");
            _btnHarvest = _root.Q<Button>("btn-harvest");
            _btnRemove = _root.Q<Button>("btn-remove");

            if (_btnClose != null) _btnClose.clicked += Hide;
            if (_btnPlant != null) _btnPlant.clicked += OnPlantClicked;
            if (_btnInspect != null) _btnInspect.clicked += OnInspectClicked;
            if (_btnHarvest != null) _btnHarvest.clicked += OnHarvestClicked;
            if (_btnRemove != null) _btnRemove.clicked += OnRemoveClicked;
        }

        private void Start()
        {
            Hide();
        }

        public void ShowForPot(PotSlot pot)
        {
            if (pot == null)
                return;

            _currentPot = pot;
            _pendingSeedTypeId = null;

            var potActions = pot.PotActions;
            bool hasPlant = potActions != null && potActions.HasPlant;
            bool isEmpty = potActions != null && potActions.PotState != null && potActions.PotState.IsEmpty;

            if (_statusLabel != null)
                _statusLabel.text = hasPlant ? "○ OCCUPIED" : "○ EMPTY";

            // PLANT: solo se vuoto
            if (_btnPlant != null)
            {
                _btnPlant.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
                _btnPlant.SetEnabled(isEmpty && potActions != null && potActions.CanPlant());
            }

            // INSPECT/HARVEST/REMOVE:
            // - EMPTY: nascosti (solo PLANT)
            // - OCCUPIED: mostrati e abilitati secondo gating
            if (_btnInspect != null)
            {
                _btnInspect.style.display = hasPlant ? DisplayStyle.Flex : DisplayStyle.None;
                _btnInspect.SetEnabled(hasPlant);
            }

            if (_btnRemove != null)
            {
                _btnRemove.style.display = hasPlant ? DisplayStyle.Flex : DisplayStyle.None;
                _btnRemove.SetEnabled(hasPlant && potActions != null && potActions.CanUproot());
            }

            if (_btnHarvest != null)
            {
                _btnHarvest.style.display = hasPlant ? DisplayStyle.Flex : DisplayStyle.None;
                _btnHarvest.SetEnabled(hasPlant && potActions != null && potActions.CanHarvest());
            }

            // Classe styling per EMPTY (centratura + box compatto)
            if (_panel != null)
            {
                if (isEmpty)
                    _panel.AddToClassList("potops-empty");
                else
                    _panel.RemoveFromClassList("potops-empty");
            }

            // Ordine richiesto:
            // - EMPTY: PLANT (solo) in cima (ordine UXML ok)
            // - OCCUPIED: INSPECT, HARVEST, REMOVE
            if (hasPlant)
            {
                EnsureButtonOrder(_btnInspect, _btnHarvest, _btnRemove);
            }

            ShowInternal();
        }

        private void ShowInternal()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }

            if (_root != null)
                _root.pickingMode = PickingMode.Position;

            // Abilita Canvas se presente (pattern coerente con PlantCardV2Controller)
            if (_uiDocument != null)
            {
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvas.enabled = true;
            }
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

            if (_uiDocument != null)
            {
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvas.enabled = false;
            }

            _currentPot = null;
            _pendingSeedTypeId = null;
        }

        private void OnPlantClicked()
        {
            if (_currentPot == null)
                return;
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null && _currentPot.PotActions != null)
                dayActivityLog.RecordDomeActionStarted(_currentPot.PotId);
            if (_seedInventoryMenu == null)
            {
                Debug.LogWarning("PotActionsMenu: SeedInventoryMenu non assegnato!");
                return;
            }

            // Subscribe one-shot
            _seedInventoryMenu.OnSeedSelected -= OnSeedSelected;
            _seedInventoryMenu.OnSeedSelected += OnSeedSelected;
            _seedInventoryMenu.OnCancelled -= OnSeedSelectionCancelled;
            _seedInventoryMenu.OnCancelled += OnSeedSelectionCancelled;

            _seedInventoryMenu.Show();
        }

        private void OnSeedSelected(string seedTypeId)
        {
            if (_currentPot == null)
                return;

            _pendingSeedTypeId = seedTypeId;

            if (_irrigationDialog == null)
            {
                Debug.LogWarning("PotActionsMenu: IrrigationDialog non assegnato!");
                return;
            }

            string seedDisplayName = _seedInventoryMenu != null
                ? _seedInventoryMenu.GetDisplayNameForSeed(seedTypeId)
                : seedTypeId;

            _irrigationDialog.Show(seedDisplayName, OnIrrigationResult);
        }

        private void OnSeedSelectionCancelled()
        {
            _pendingSeedTypeId = null;
        }

        private void OnIrrigationResult(bool irrigate)
        {
            if (_currentPot == null)
                return;

            var potActions = _currentPot.PotActions;
            if (potActions == null)
                return;

            bool success = potActions.DoPlant(_pendingSeedTypeId, irrigate);
            if (!success)
                return;

            // Salva riferimento al pot prima di Hide(), perché Hide() azzera _currentPot
            var pot = _currentPot;

            // Close all on success
            if (_irrigationDialog != null) _irrigationDialog.Hide();
            if (_seedInventoryMenu != null) _seedInventoryMenu.Hide();
            Hide();

            // DEBUG_SAFE_FIX: Apri automaticamente PlantCardV2 dopo il planting per velocizzare il flow
            // L'utente non deve riaprire Pot Ops menu per vedere la pianta appena piantata
            if (_plantCardOpener != null)
            {
                _plantCardOpener.OpenForInspect(pot);
            }
            else
            {
                // Fallback: tenta a trovare un opener in scena
                var opener = FindObjectOfType<Sporae.UI.UIToolkit.PlantCard.PlantCardV2Opener>();
                if (opener != null)
                    opener.OpenForInspect(pot);
            }
        }

        private void OnInspectClicked()
        {
            if (_currentPot == null)
                return;

            // Salva riferimento prima di Hide(), perché Hide() azzera _currentPot
            var pot = _currentPot;
            Hide();

            if (_plantCardOpener != null)
            {
                _plantCardOpener.OpenForInspect(pot);
            }
            else
            {
                // Fallback: tenta a trovare un opener in scena
                var opener = FindObjectOfType<Sporae.UI.UIToolkit.PlantCard.PlantCardV2Opener>();
                if (opener != null)
                    opener.OpenForInspect(pot);
            }
        }

        private void OnHarvestClicked()
        {
            if (_currentPot == null)
                return;

            var potActions = _currentPot.PotActions;
            if (potActions == null)
                return;

            bool success = potActions.DoHarvest();
            if (success)
                Hide();
        }

        private void OnRemoveClicked()
        {
            if (_currentPot == null)
                return;

            var potActions = _currentPot.PotActions;
            if (potActions == null)
                return;

            bool success = potActions.DoUproot();
            if (success)
                Hide();
        }

        private void EnsureButtonOrder(params VisualElement[] orderedElements)
        {
            if (_list == null || orderedElements == null || orderedElements.Length == 0)
                return;

            // Re-inserisce gli elementi nella sequenza richiesta (idempotente).
            for (int i = 0; i < orderedElements.Length; i++)
            {
                var el = orderedElements[i];
                if (el == null)
                    continue;

                if (el.parent == _list)
                    _list.Remove(el);

                _list.Insert(i, el);
            }
        }
    }
}


