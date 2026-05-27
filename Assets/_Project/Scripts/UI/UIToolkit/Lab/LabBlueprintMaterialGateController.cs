using System;
using System.Linq;
using _Project.Sporae.Core;
using _Project.Sporae.Core.Knowledge;
using _Project.Sporae.Core.LabBlueprint;
using Sporae.Core;
using Sporae.Core.Localization;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;
using UnityEngine;

namespace Sporae.UI.UIToolkit.Lab
{
    /// <summary>
    /// Gate post-scansione LAB 4.0: readiness → picker inventario (frutto XOR spora) → <see cref="LabBlueprintService.StartDraft"/>.
    /// Nessun consumo item fino a SIGILLA (Task successivi).
    /// </summary>
    public sealed class LabBlueprintMaterialGateController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryPanelController _playerInventoryPanel;

        private LabBlueprintReadinessService _readiness;
        private LabBlueprintService _blueprint;
        private KnowledgeProgressionService _knowledge;
        private GameManager _gameManager;
        private DayCycleSystem _dayCycle;
        private bool _pickerOpen;

        public event Action<LabBlueprintState> DraftStarted;
        public event Action MaterialSelectionCancelled;
        public event Action<LabBlueprintReadinessResult> MaterialNotReady;

        private void Awake()
        {
            ResolveServices();
        }

        private void ResolveServices()
        {
            var container = ServiceContainer.Instance;
            if (container == null)
                return;

            _readiness ??= container.Get<LabBlueprintReadinessService>(suppressWarning: true);
            _blueprint ??= container.Get<LabBlueprintService>(suppressWarning: true);
            _knowledge ??= container.Get<KnowledgeProgressionService>(suppressWarning: true);
            _dayCycle ??= container.Get<DayCycleSystem>(suppressWarning: true);
        }

        /// <summary>
        /// Avvia il gate materiale dopo SCANSIONA/APRI GENOSCRITTORE: check inventario, poi picker se idoneo.
        /// </summary>
        public void BeginMaterialSelection()
        {
            if (_pickerOpen)
                return;

            ResolveServices();
            EnsureGameManager();

            var inventory = _gameManager?.PlayerInventory;
            var readiness = _readiness != null
                ? _readiness.Evaluate(inventory, _blueprint)
                : new LabBlueprintReadinessResult(LabBlueprintReadinessStatus.NoInventory, 0, 0);

            if (!readiness.IsReady)
            {
                EmitReadinessFeedback(readiness);
                MaterialNotReady?.Invoke(readiness);
                return;
            }

            if (!TryOpenInventoryPicker(inventory))
            {
                MaterialNotReady?.Invoke(new LabBlueprintReadinessResult(LabBlueprintReadinessStatus.NoMaterial, 0, 0));
            }
        }

        private bool TryOpenInventoryPicker(Inventory inventory)
        {
            var panel = ResolveInventoryPanel();
            if (panel == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "[LabBlueprintMaterialGate] PlayerInventoryPanelController non disponibile.");
                return false;
            }

            var allowed = LabBlueprintReadinessService.BuildPickerAllowedTypeIds(inventory);
            if (allowed == null || allowed.Count == 0)
                return false;

            _pickerOpen = true;
            panel.ShowAsPicker(
                allowed,
                LocalizationManager.GetString("lab_blueprint.picker_title"),
                OnPickerItemSelected,
                OnPickerCancelled,
                filterSporeStage: null,
                pickerContext: "lab_blueprint_material",
                presentFullInventoryUi: false);

            return true;
        }

        private void OnPickerItemSelected(string typeId, SporeStage? sporeStage, Item pickedItem)
        {
            _pickerOpen = false;
            ResolveServices();
            EnsureGameManager();

            if (_blueprint == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "[LabBlueprintMaterialGate] LabBlueprintService non registrato.");
                return;
            }

            var inventory = _gameManager?.PlayerInventory;
            if (inventory == null)
                return;

            Item item = pickedItem;
            if (item == null && !string.IsNullOrWhiteSpace(typeId))
            {
                if (typeId == Items.SporeGeneric && sporeStage.HasValue)
                {
                    foreach (var slot in inventory.Items)
                    {
                        if (slot?.TypeId != Items.SporeGeneric)
                            continue;
                        item = slot.Items.FirstOrDefault(i => i.SporeStageValue == sporeStage);
                        break;
                    }
                }
                else
                {
                    item = inventory.PeekFirst(typeId);
                }
            }

            if (!LabBlueprintReadinessService.TryValidateSelectedItem(item, out var inputKind))
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[LabBlueprintMaterialGate] Selezione non idonea per blueprint.");
                return;
            }

            if (!InventoryStillContains(inventory, item))
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[LabBlueprintMaterialGate] Item non più in inventario.");
                return;
            }

            int day = Mathf.Max(1, _dayCycle?.CurrentDay ?? 1);
            try
            {
                var state = _blueprint.StartDraft(inputKind, item, _knowledge, day);
                DraftStarted?.Invoke(state);
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.UI, $"[LabBlueprintMaterialGate] StartDraft fallito: {ex.Message}");
            }
        }

        private void OnPickerCancelled()
        {
            _pickerOpen = false;
            MaterialSelectionCancelled?.Invoke();
        }

        private static bool InventoryStillContains(Inventory inventory, Item item)
        {
            if (inventory == null || item == null)
                return false;

            foreach (var slot in inventory.Items)
            {
                if (slot == null || slot.TypeId != item.TypeId || slot.Items == null)
                    continue;
                foreach (var candidate in slot.Items)
                {
                    if (candidate != null && candidate.ItemId == item.ItemId)
                        return true;
                }
            }

            return false;
        }

        private PlayerInventoryPanelController ResolveInventoryPanel()
        {
            if (_playerInventoryPanel != null)
                return _playerInventoryPanel;

            return ServiceContainer.Instance?.Get<PlayerInventoryPanelController>(suppressWarning: true);
        }

        private void EnsureGameManager()
        {
            if (_gameManager != null)
                return;

            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
        }

        private static void EmitReadinessFeedback(LabBlueprintReadinessResult readiness)
        {
            var key = LabBlueprintReadinessService.GetLocalizationKey(readiness.Status);
            if (string.IsNullOrEmpty(key))
                return;

            var message = LocalizationManager.GetString(key);
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null)
                foundation.PostToast("POT-AUTO-ERROR", new NotificationPayload().With("message", message));
            else
                SporiumLogger.LogWarning(LogCategory.UI, $"[LabBlueprintMaterialGate] {message}");
        }
    }
}
