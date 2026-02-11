using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.SeedInventory;

namespace Sporae.UI.UIToolkit.PlayerInventory
{
    /// <summary>
    /// Componente unico e definitivo dell'inventario del giocatore.
    /// - Modalità view (tasto INV / Biologo): mostra tutti gli oggetti, nessuna selezione.
    /// - Modalità picker (Lab "Seleziona", ecc.): mostra tutti gli oggetti; quelli compatibili con il contesto sono selezionabili, gli altri disabilitati.
    /// Sincronizzato con il tasto INV nella sezione Biologo Player della HUD.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerInventoryPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _invTitle;
        private Label _invSubtitle;
        private ScrollView _list;
        private Button _btnClose;
        private Button _btnCancel;

        private Inventory _playerInventory;
        private HashSet<string> _pickerAllowedTypes;
        /// <summary>Ordine dei typeId ammessi (per mostrare per primi gli item adatti al macchinario in modalità picker).</summary>
        private List<string> _pickerAllowedTypesOrdered;
        private Action<string, SporeStage?> _onSelectedWithStage;
        private SporeStage? _pickerFilterSporeStage;
        private Action _onCancel;
        private string _pickerContext;
        private bool _uiBound;

        private VisualElement _invTooltip;
        private Label _invTooltipText;

        public event Action OnClosed;

        public bool IsVisible => _overlay != null && _overlay.style.display != DisplayStyle.None;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 450;

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root != null)
                TryBindUI();
            TryBindInventory();
        }

        /// <summary>Binding ritardato: dopo Hide() il GameObject è disattivato e l'albero viene ricreato; in ShowInternal() si fa SetActive(true) e poi TryBindUI() per aggiornare i riferimenti.</summary>
        private void TryBindUI()
        {
            if (_uiDocument != null)
            {
                var currentRoot = _uiDocument.rootVisualElement;
                if (currentRoot != null && currentRoot != _root)
                {
                    _root = currentRoot;
                    _invTooltip = null;
                    _invTooltipText = null;
                    _uiBound = false;
                }
            }
            if (_uiBound) return;
            if (_root == null && _uiDocument != null)
                _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _overlay = _root.Q<VisualElement>("inv-overlay");
            _invTitle = _root.Q<Label>("inv-title");
            _invSubtitle = _root.Q<Label>("inv-subtitle");
            _list = _root.Q<ScrollView>("inv-list");
            _btnClose = _root.Q<Button>("btn-close");
            _btnCancel = _root.Q<Button>("btn-cancel");

            if (_btnClose != null)
            {
                foreach (var child in _btnClose.Children())
                    child.pickingMode = PickingMode.Ignore;
                _btnClose.clicked += Hide;
            }
            if (_btnCancel != null)
                _btnCancel.clicked += OnCancelClicked;

            _uiBound = true;
        }

        private void OnEnable()
        {
            TryBindInventory();
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnDisable()
        {
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged -= OnInventoryChanged;
        }

        private void Start()
        {
            Hide();
        }

        private void TryBindInventory()
        {
            var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (gm == null)
                gm = FindObjectOfType<GameManager>();
            _playerInventory = gm != null ? gm.PlayerInventory : null;
        }

        private void OnInventoryChanged()
        {
            if (IsVisible)
                Rebuild();
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Hide();
        }

        /// <summary>Mostra l'inventario in modalità view (tasto INV). Nessuna selezione.</summary>
        public void Show()
        {
            _pickerAllowedTypes = null;
            _pickerAllowedTypesOrdered = null;
            _onSelectedWithStage = null;
            _pickerFilterSporeStage = null;
            _onCancel = null;
            ShowInternal();
            if (_invSubtitle != null)
                _invSubtitle.text = "Oggetti nel tuo inventario";
            if (_btnCancel != null)
                _btnCancel.style.display = DisplayStyle.None;
            Rebuild();
        }

        /// <summary>Toggle per sincronizzazione con tasto INV (Biologo Player).</summary>
        public void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Mostra l'inventario in modalità picker: solo i typeId in allowedTypeIds sono selezionabili; gli altri sono visibili ma non selezionabili.
        /// pickerContext: es. "extractor" per tooltip preview frutto.
        /// </summary>
        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string> onSelected, Action onCancel, string pickerContext = null)
        {
            ShowAsPicker(allowedTypeIds, subtitle, (id, _) => onSelected(id), onCancel, null, pickerContext);
        }

        /// <summary>
        /// Picker con callback (typeId, sporeStage) e filtro opzionale per spore. pickerContext: es. "extractor" per tooltip frutto.
        /// </summary>
        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string, SporeStage?> onSelectedWithStage, Action onCancel, SporeStage? filterSporeStage = null, string pickerContext = null)
        {
            _pickerAllowedTypes = allowedTypeIds != null ? new HashSet<string>(allowedTypeIds) : new HashSet<string>();
            _pickerAllowedTypesOrdered = allowedTypeIds != null ? new List<string>(allowedTypeIds) : new List<string>();
            _onSelectedWithStage = onSelectedWithStage;
            _pickerFilterSporeStage = filterSporeStage;
            _onCancel = onCancel;
            _pickerContext = pickerContext;
            ShowInternal();
            if (_invSubtitle != null)
                _invSubtitle.text = string.IsNullOrEmpty(subtitle) ? "Seleziona un item compatibile" : subtitle;
            if (_btnCancel != null)
                _btnCancel.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        private void EnsureInvTooltip()
        {
            if (_invTooltip != null || _root == null) return;
            _invTooltip = new VisualElement();
            _invTooltip.name = "inv-tooltip";
            _invTooltip.style.position = Position.Absolute;
            _invTooltip.style.display = DisplayStyle.None;
            _invTooltip.style.backgroundColor = new UnityEngine.Color(0.05f, 0.07f, 0.09f, 0.96f);
            _invTooltip.style.borderTopWidth = _invTooltip.style.borderRightWidth = _invTooltip.style.borderBottomWidth = _invTooltip.style.borderLeftWidth = 2f;
            _invTooltip.style.borderTopColor = _invTooltip.style.borderRightColor = _invTooltip.style.borderBottomColor = _invTooltip.style.borderLeftColor = new UnityEngine.Color(0.5f, 0.8f, 0.5f, 0.9f);
            _invTooltip.style.paddingTop = _invTooltip.style.paddingRight = _invTooltip.style.paddingBottom = _invTooltip.style.paddingLeft = 10f;
            _invTooltip.style.minWidth = 280f;
            _invTooltip.style.maxWidth = 320f;
            _invTooltip.pickingMode = PickingMode.Ignore;
            _invTooltipText = new Label();
            _invTooltipText.enableRichText = true;
            _invTooltipText.style.whiteSpace = WhiteSpace.Normal;
            _invTooltipText.style.color = new UnityEngine.Color(0.95f, 0.96f, 0.98f, 1f);
            _invTooltipText.style.fontSize = 12f;
            _invTooltip.Add(_invTooltipText);
            _root.Add(_invTooltip);
        }

        public void Hide()
        {
            if (_invTooltip != null)
                _invTooltip.style.display = DisplayStyle.None;
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }
            if (_root != null)
                _root.pickingMode = PickingMode.Ignore;
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }

        private void ShowInternal()
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
        }

        private void Rebuild()
        {
            if (_list == null) return;
            _list.Clear();

            if (_playerInventory == null)
            {
                var empty = new Label("Inventario non disponibile.");
                empty.AddToClassList("inv-empty");
                _list.Add(empty);
                return;
            }

            if (_playerInventory.IsEmpty)
            {
                var empty = new Label("Nessun oggetto in inventario.");
                empty.AddToClassList("inv-empty");
                _list.Add(empty);
                return;
            }

            bool isPicker = _pickerAllowedTypes != null && _pickerAllowedTypes.Count > 0;

            IEnumerable<InventorySlot> slotsToShow = _playerInventory.Items.Where(s => s.Items.Count > 0);
            if (isPicker && _pickerAllowedTypesOrdered != null && _pickerAllowedTypesOrdered.Count > 0)
            {
                var byType = _playerInventory.Items.Where(s => s.Items.Count > 0).ToDictionary(s => s.TypeId, s => s);
                var ordered = new List<InventorySlot>();
                foreach (var typeId in _pickerAllowedTypesOrdered)
                    if (byType.TryGetValue(typeId, out var slot))
                        ordered.Add(slot);
                foreach (var slot in _playerInventory.Items)
                    if (slot.Items.Count > 0 && !_pickerAllowedTypes.Contains(slot.TypeId))
                        ordered.Add(slot);
                slotsToShow = ordered;
            }

            foreach (var slot in slotsToShow)
            {
                string typeId = slot.TypeId;

                if (typeId == Items.SporeGeneric)
                {
                    AddSporeRowsByStage(slot, isPicker, _pickerFilterSporeStage);
                    continue;
                }
                if (IsFruitType(typeId))
                {
                    AddFruitRows(slot, isPicker);
                    continue;
                }

                int qty = slot.Quantity;
                string displayName = GetItemDisplayName(typeId, slot.Items.FirstOrDefault());
                string subText = GetSporeInfoText(slot);

                bool selectable = !isPicker || _pickerAllowedTypes.Contains(typeId);

                var row = new VisualElement();
                row.AddToClassList("inv-row");
                if (!selectable && isPicker)
                    row.AddToClassList("inv-row-disabled");

                var left = new VisualElement();
                left.style.flexDirection = FlexDirection.Column;
                left.style.alignItems = Align.FlexStart;
                var nameRow = new VisualElement();
                nameRow.style.flexDirection = FlexDirection.Row;
                nameRow.style.alignItems = Align.Center;
                var nameLabel = new Label(displayName);
                nameLabel.AddToClassList("inv-row-name");
                var qtyLabel = new Label($"x{qty}");
                qtyLabel.AddToClassList("inv-row-qty");
                nameRow.Add(nameLabel);
                nameRow.Add(qtyLabel);
                left.Add(nameRow);
                if (!string.IsNullOrEmpty(subText))
                {
                    var subLabel = new Label(subText);
                    subLabel.AddToClassList("inv-row-sub");
                    left.Add(subLabel);
                }

                var right = new VisualElement();
                right.AddToClassList("inv-row-right");

                if (isPicker && selectable)
                {
                    var selectBtn = new Button(() =>
                    {
                        _onSelectedWithStage?.Invoke(typeId, null);
                        Hide();
                    }) { text = "Seleziona" };
                    selectBtn.AddToClassList("inv-select");
                    right.Add(selectBtn);
                }

                string tooltipContent = typeId == Items.PreSeed
                    ? BuildPreSeedItemTooltip(slot.Items.FirstOrDefault())
                    : BuildGenericItemTooltip(typeId, displayName, qty, slot.Items.FirstOrDefault());
                RegisterRowTooltip(row, tooltipContent);

                row.Add(left);
                row.Add(right);
                _list.Add(row);
            }
        }

        private static bool IsFruitType(string typeId)
        {
            return typeId == Items.Fruits || typeId == Items.FruitsKnown;
        }

        /// <summary>Per i frutti: una riga per singolo item (mai cumulati), con tooltip per item.</summary>
        private void AddFruitRows(InventorySlot slot, bool isPicker)
        {
            if (slot == null || slot.Items.Count == 0)
                return;

            string typeId = slot.TypeId;
            bool selectable = !isPicker || _pickerAllowedTypes.Contains(typeId);
            string displayName = typeId == Items.FruitsKnown ? "Frutto conosciuto" : "Frutto";

            foreach (var fruit in slot.Items)
            {
                bool unknown = Lab.ExtractorTooltipTexts.IsUnknownFruit(fruit);
                string subText;
                string tooltipContent;
                if (typeId == Items.FruitsKnown)
                {
                    subText = unknown ? "Artic Hask" : Lab.ExtractorTooltipTexts.GetFruitDisplayName(fruit);
                    tooltipContent = unknown ? Lab.ExtractorTooltipTexts.BuildFruitKnownDemoTooltip() : Lab.ExtractorTooltipTexts.BuildFruitPreviewTooltip(fruit);
                }
                else
                {
                    subText = unknown ? "Sconosciuto" : Lab.ExtractorTooltipTexts.GetFruitDisplayName(fruit);
                    tooltipContent = unknown ? Lab.ExtractorTooltipTexts.BuildFruitUnknownPreviewTooltip(fruit) : Lab.ExtractorTooltipTexts.BuildFruitPreviewTooltip(fruit);
                }

                var row = new VisualElement();
                row.AddToClassList("inv-row");
                if (!selectable && isPicker)
                    row.AddToClassList("inv-row-disabled");

                var left = new VisualElement();
                left.style.flexDirection = FlexDirection.Column;
                left.style.alignItems = Align.FlexStart;
                var nameRow = new VisualElement();
                nameRow.style.flexDirection = FlexDirection.Row;
                nameRow.style.alignItems = Align.Center;
                var nameLabel = new Label(displayName);
                nameLabel.AddToClassList("inv-row-name");
                var qtyLabel = new Label("x1");
                qtyLabel.AddToClassList("inv-row-qty");
                nameRow.Add(nameLabel);
                nameRow.Add(qtyLabel);
                left.Add(nameRow);
                if (!string.IsNullOrEmpty(subText))
                {
                    var subLabel = new Label(subText);
                    subLabel.AddToClassList("inv-row-sub");
                    left.Add(subLabel);
                }

                var right = new VisualElement();
                right.AddToClassList("inv-row-right");
                if (isPicker && selectable)
                {
                    var selectBtn = new Button(() =>
                    {
                        _onSelectedWithStage?.Invoke(typeId, null);
                        Hide();
                    }) { text = "Seleziona" };
                    selectBtn.AddToClassList("inv-select");
                    right.Add(selectBtn);
                }

                RegisterRowTooltip(row, tooltipContent);
                row.Add(left);
                row.Add(right);
                _list.Add(row);
            }
        }

        private void RegisterRowTooltip(VisualElement row, string tooltipContent)
        {
            EnsureInvTooltip();
            if (_invTooltip == null || _invTooltipText == null) return;
            row.RegisterCallback<MouseEnterEvent>(evt =>
            {
                _invTooltipText.text = tooltipContent;
                _invTooltip.style.display = DisplayStyle.Flex;
                _invTooltip.BringToFront();
                PositionTooltipAtMouse(row, evt.mousePosition);
            });
            row.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                _invTooltip.style.display = DisplayStyle.None;
            });
            row.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (_invTooltip.style.display != DisplayStyle.Flex) return;
                PositionTooltipAtMouse(row, evt.mousePosition);
            });
        }

        /// <summary>Posiziona il tooltip vicino al mouse. mousePosPanel è in coordinate pannello (= spazio locale di _root). Tooltip è figlio di _root.</summary>
        private void PositionTooltipAtMouse(VisualElement row, UnityEngine.Vector2 mousePosPanel)
        {
            if (_invTooltip == null || _root == null) return;
            // evt.mousePosition è in panel coordinates = _root local space (root è il rootVisualElement)
            float x = mousePosPanel.x + 16f;
            float y = mousePosPanel.y + 12f;
            const float tw = 300f;
            float th = _invTooltip.resolvedStyle.height;
            var bounds = _root.contentRect;
            if (x + tw > bounds.width) x = mousePosPanel.x - tw - 8f;
            if (y + th > bounds.height) y = mousePosPanel.y - th - 8f;
            if (y < 0f) y = 8f;
            if (x < 0f) x = 8f;
            _invTooltip.style.left = x;
            _invTooltip.style.top = y;
        }

        /// <summary>Per spore-generic: una riga per singola spora (mai cumulate). Se filterStage è valorizzato, mostra solo le righe con quello stage.</summary>
        private void AddSporeRowsByStage(InventorySlot slot, bool isPicker, SporeStage? filterStage = null)
        {
            foreach (var item in slot.Items)
            {
                if (filterStage.HasValue && item.SporeStageValue != filterStage)
                    continue;

                string displayName = "Spora";
                string subText = GetSporeSubText(item.SporeStageValue, item.GeneticTypeValue);
                bool selectable = !isPicker || _pickerAllowedTypes.Contains(Items.SporeGeneric);

                var row = new VisualElement();
                row.AddToClassList("inv-row");
                if (!selectable && isPicker)
                    row.AddToClassList("inv-row-disabled");

                var left = new VisualElement();
                left.style.flexDirection = FlexDirection.Column;
                left.style.alignItems = Align.FlexStart;
                var nameRow = new VisualElement();
                nameRow.style.flexDirection = FlexDirection.Row;
                nameRow.style.alignItems = Align.Center;
                var nameLabel = new Label(displayName);
                nameLabel.AddToClassList("inv-row-name");
                var qtyLabel = new Label("x1");
                qtyLabel.AddToClassList("inv-row-qty");
                nameRow.Add(nameLabel);
                nameRow.Add(qtyLabel);
                left.Add(nameRow);
                if (!string.IsNullOrEmpty(subText))
                {
                    var subLabel = new Label(subText);
                    subLabel.AddToClassList("inv-row-sub");
                    left.Add(subLabel);
                }

                var right = new VisualElement();
                right.AddToClassList("inv-row-right");
                if (isPicker && selectable)
                {
                    var stage = item.SporeStageValue;
                    var selectBtn = new Button(() =>
                    {
                        _onSelectedWithStage?.Invoke(Items.SporeGeneric, stage);
                        Hide();
                    }) { text = "Seleziona" };
                    selectBtn.AddToClassList("inv-select");
                    right.Add(selectBtn);
                }

                string sporeTooltipContent = BuildSporeItemTooltip(displayName, item);
                RegisterRowTooltip(row, sporeTooltipContent);

                row.Add(left);
                row.Add(right);
                _list.Add(row);
            }
        }

        private static string GetSporeSubText(SporeStage? stage, GeneticType? genetic)
        {
            var parts = new List<string>();
            if (stage.HasValue)
                parts.Add(stage.Value == SporeStage.Raw ? "Raw" : "Maturata");
            if (genetic.HasValue)
                parts.Add(Lab.ExtractorTooltipTexts.GeneticTypeToTrattiLabel(genetic));
            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        /// <summary>Nome leggibile per un typeId (semi da PlantData, Pre-Seed, altri da typeId). Se item ha CustomPlantName (seme da Incubatore) restituisce "Seme di Pianta {nome}".</summary>
        public static string GetItemDisplayName(string typeId, Item item = null)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.CustomPlantName))
                return "Seme di Pianta " + item.CustomPlantName;
            return GetItemDisplayNameInternal(typeId);
        }

        private static string GetItemDisplayNameInternal(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return typeId;
            if (typeId == Items.PreSeed) return "Pre-Seed";
            if (typeId == Items.FruitsKnown) return "Frutto conosciuto";
            if (PlantDatabase.Instance != null)
            {
                var plantData = PlantDatabase.Instance.GetPlantDataBySeedTypeId(typeId);
                if (plantData != null)
                    return SeedInventoryMenu.GetSeedDisplayName(typeId);
            }
            return typeId;
        }

        private static string Tv(string value) => Lab.ExtractorTooltipTexts.WrapValue(value ?? "—");

        /// <summary>Tooltip Pre-Seed: Tratti (Fissi/Stabili/Instabili), Famiglie sorgente, Tratti compatibili.</summary>
        private static string BuildPreSeedItemTooltip(Item item)
        {
            if (item == null) return Tv("Pre-Seed");
            string trattiLabel = Lab.ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            string fa = string.IsNullOrWhiteSpace(item.ParentFamilyA) ? "—" : item.ParentFamilyA;
            string fb = string.IsNullOrWhiteSpace(item.ParentFamilyB) ? "—" : item.ParentFamilyB;
            string famiglie = $"{fa} + {fb}";
            string trattiCompat = string.IsNullOrWhiteSpace(item.CandidateTraitsCsv) ? "—" : item.CandidateTraitsCsv;
            var lines = new List<string>
            {
                $"Tratti (fissati Step 3): {Tv(trattiLabel)}",
                $"Famiglie sorgente: {Tv(famiglie)}",
                $"Tratti compatibili: {Tv(trattiCompat)}"
            };
            return string.Join("\n", lines);
        }

        /// <summary>Testo tooltip generico per qualsiasi item (inventario view o picker lab).</summary>
        private static string BuildGenericItemTooltip(string typeId, string displayName, int qty, Item firstItem)
        {
            var lines = new List<string>
            {
                Tv(displayName),
                $"Tipo: {Tv(typeId)}",
                $"Quantità: {Tv(qty.ToString())}"
            };
            if (firstItem != null)
            {
                if (firstItem.GeneticTypeValue.HasValue)
                {
                    string tratti = Lab.ExtractorTooltipTexts.GeneticTypeToTrattiLabel(firstItem.GeneticTypeValue);
                    lines.Add($"Tratti: {Tv(tratti)}");
                    lines.Add($"% di mutare: {Tv(Lab.ExtractorTooltipTexts.GeneticTypeToPercentMutare(firstItem.GeneticTypeValue))}");
                }
                if (!string.IsNullOrWhiteSpace(firstItem.FamilyMetadata))
                    lines.Add($"Famiglia: {Tv(firstItem.FamilyMetadata)}");
                if (!string.IsNullOrWhiteSpace(firstItem.SelectedTraitsCsv))
                    lines.Add($"Tratti selezionati: {Tv(firstItem.SelectedTraitsCsv)}");
                if (firstItem.TraitPowerPercent > 0 && firstItem.TraitPowerPercent < 100)
                    lines.Add($"Potenza tratti: {Tv(firstItem.TraitPowerPercent.ToString() + "%")}");
            }
            return string.Join("\n", lines);
        }

        /// <summary>Tooltip spora concordato: Tratti (Fissi/Stabili/Instabili), % di mutare, Famiglia, Stato.</summary>
        private static string BuildSporeItemTooltip(string displayName, Item item)
        {
            if (item == null)
                return Tv(displayName ?? "Spora");

            string tratti = Lab.ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            string percentMutare = Lab.ExtractorTooltipTexts.GeneticTypeToPercentMutare(item.GeneticTypeValue);
            string family = string.IsNullOrWhiteSpace(item.FamilyMetadata) ? "—" : item.FamilyMetadata;
            bool isRaw = item.SporeStageValue == SporeStage.Raw;
            string stato = isRaw ? "Raw (non combinabile)" : "Matura ✓ (pronta per fusione)";

            var lines = new List<string>
            {
                $"Tratti: {Tv(tratti)}",
                $"% di mutare: {Tv(percentMutare)}",
                $"Famiglia: {Tv(family)}",
                $"Stato: {Tv(stato)}"
            };
            return string.Join("\n", lines);
        }

        /// <summary>Testo info spora (stadio + tipo genetico) per slot di tipo spore-generic.</summary>
        private static string GetSporeInfoText(InventorySlot slot)
        {
            if (slot == null || slot.TypeId != Items.SporeGeneric || slot.Items.Count == 0) return "";
            var first = slot.Items.FirstOrDefault();
            if (first == null) return "";
            var parts = new List<string>();
            if (first.SporeStageValue.HasValue)
                parts.Add(first.SporeStageValue.Value == SporeStage.Raw ? "Raw" : "Maturata");
            if (first.GeneticTypeValue.HasValue)
                parts.Add(Lab.ExtractorTooltipTexts.GeneticTypeToTrattiLabel(first.GeneticTypeValue));
            return parts.Count > 0 ? string.Join(", ", parts) : "Spora generica";
        }
    }
}
