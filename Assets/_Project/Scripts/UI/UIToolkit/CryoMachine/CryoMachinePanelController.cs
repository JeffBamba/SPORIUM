using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using _Project.Sporae.Core;
using Sporae.Core.Localization;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.CryoMachine
{
    /// <summary>
    /// Controller del pannello HUD della Cryo Machine.
    /// Mostra lo stato dei 3 slot passivi (occupato / vuoto, pianta, livello, potere passivo).
    /// Si apre tramite CryoMachineOpener (Interactable.OnInteract) e si chiude con il pulsante X o ESC.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CryoMachinePanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label         _footer;
        private Button        _btnClose;
        private bool          _uiBound;
        private bool          _isOpen;

        private const int SLOT_COUNT = 3;

        private struct SlotElements
        {
            public Label  IdLabel;
            public Label  Badge;
            public Label  PlantLabel;
            public VisualElement InfoBlock;
            public Label  Level;
            public Label  Family;
            public Label  Power;
        }

        private readonly SlotElements[] _slotEls = new SlotElements[SLOT_COUNT];

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument != null)
            {
                // Copia PanelSettings se mancante (stesso pattern dei Lab panel)
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
                _uiDocument.sortingOrder = 450;
            }

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();

            // Pannello nascosto all'avvio
            SetVisible(false);
        }

        private void OnEnable()
        {
            GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            if (_isOpen)
                RefreshSlots();
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        // ── Binding UI ───────────────────────────────────────────────────────

        private void TryBindUI()
        {
            if (_uiBound) return;
            if (_uiDocument == null) return;

            var currentRoot = _uiDocument.rootVisualElement;
            if (currentRoot == null) return;
            _root = currentRoot;

            _overlay  = _root.Q<VisualElement>("cryo-overlay");
            _footer   = _root.Q<Label>("cryo-footer");
            _btnClose = _root.Q<Button>("btn-close");

            if (_btnClose != null)
                _btnClose.clicked += Hide;

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                string idx = i.ToString();
                _slotEls[i] = new SlotElements
                {
                    IdLabel    = _root.Q<Label>($"cryo-slot-{idx}-id"),
                    Badge      = _root.Q<Label>($"cryo-slot-{idx}-badge"),
                    PlantLabel = _root.Q<Label>($"cryo-slot-{idx}-plant"),
                    InfoBlock  = _root.Q<VisualElement>($"cryo-slot-{idx}-info"),
                    Level      = _root.Q<Label>($"cryo-slot-{idx}-level"),
                    Family     = _root.Q<Label>($"cryo-slot-{idx}-family"),
                    Power      = _root.Q<Label>($"cryo-slot-{idx}-power"),
                };
            }

            _uiBound = true;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Show()
        {
            TryBindUI();
            RefreshSlots();
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            _isOpen = visible;

            if (_overlay != null)
                _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            else if (_root != null)
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshSlots()
        {
            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            if (cryo == null)
            {
                if (_footer != null)
                    _footer.text = LocalizationManager.GetString("cryo.footer_unavailable");
                return;
            }

            var slots = cryo.GetPassiveSlotsSnapshot();
            int occupied = cryo.OccupiedCount();
            int total    = slots?.Count ?? 0;

            if (_footer != null)
                _footer.text = LocalizationManager.GetString("cryo.slots_occupied", new Dictionary<string, string>
                {
                    ["n"] = occupied.ToString(),
                    ["total"] = total.ToString()
                });

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                ref var el = ref _slotEls[i];

                if (slots == null || i >= slots.Count || slots[i] == null)
                {
                    // Slot non configurato
                    SetSlotEmpty(ref el, $"CRYO-{(i + 1):D2}", "N/A");
                    continue;
                }

                var slot = slots[i];

                if (el.IdLabel != null)
                    el.IdLabel.text = slot.SlotId;

                if (!slot.IsOccupied)
                {
                    SetSlotEmpty(ref el, slot.SlotId, LocalizationManager.GetString("cryo.badge_empty"));
                }
                else
                {
                    SetSlotOccupied(ref el, slot.Payload);
                }
            }
        }

        private void SetSlotEmpty(ref SlotElements el, string slotId, string badgeText)
        {
            if (el.Badge != null)
            {
                el.Badge.text = badgeText;
                el.Badge.RemoveFromClassList("cryo-badge-active");
                el.Badge.AddToClassList("cryo-badge-empty");
            }
            if (el.PlantLabel != null)
            {
                el.PlantLabel.text = LocalizationManager.GetString("cryo.slot_free");
                el.PlantLabel.style.display = DisplayStyle.Flex;
            }
            if (el.InfoBlock != null)
                el.InfoBlock.style.display = DisplayStyle.None;
        }

        private void SetSlotOccupied(ref SlotElements el, CryoPlantPayload p)
        {
            if (el.Badge != null)
            {
                el.Badge.text = LocalizationManager.GetString("cryo.badge_active");
                el.Badge.RemoveFromClassList("cryo-badge-empty");
                el.Badge.AddToClassList("cryo-badge-active");
            }

            string plantName = !string.IsNullOrWhiteSpace(p.CustomPlantName) ? p.CustomPlantName : p.PlantCode;
            string tags = "";
            if (p.IsHybrid)  tags += " [IBR]";
            if (p.IsMutated) tags += " [MUT]";

            if (el.PlantLabel != null)
            {
                el.PlantLabel.text = $"{plantName}{tags}";
                el.PlantLabel.style.display = DisplayStyle.Flex;
            }

            if (el.InfoBlock != null)
                el.InfoBlock.style.display = DisplayStyle.Flex;

            if (el.Level != null)
                el.Level.text = LocalizationManager.GetString("cryo.level", new Dictionary<string, string> { ["n"] = p.PlantLevel.ToString() });

            if (el.Family != null)
                el.Family.text = !string.IsNullOrWhiteSpace(p.PlantFamilyMetadata) ? p.PlantFamilyMetadata : "—";

            if (el.Power != null)
                el.Power.text = !string.IsNullOrWhiteSpace(p.PassivePowerLabel) ? p.PassivePowerLabel : "—";
        }
    }
}
