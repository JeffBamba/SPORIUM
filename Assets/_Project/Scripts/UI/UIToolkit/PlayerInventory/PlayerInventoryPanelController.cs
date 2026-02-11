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
        private Action<string, SporeStage?> _onSelectedWithStage;
        private SporeStage? _pickerFilterSporeStage;
        private Action _onCancel;
        private bool _uiBound;

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
        /// </summary>
        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string> onSelected, Action onCancel)
        {
            ShowAsPicker(allowedTypeIds, subtitle, (id, _) => onSelected(id), onCancel, null);
        }

        /// <summary>
        /// Picker con callback (typeId, sporeStage) e filtro opzionale per spore: solo le righe con lo stage indicato sono mostrate/selezionabili.
        /// </summary>
        public void ShowAsPicker(IEnumerable<string> allowedTypeIds, string subtitle, Action<string, SporeStage?> onSelectedWithStage, Action onCancel, SporeStage? filterSporeStage = null)
        {
            _pickerAllowedTypes = allowedTypeIds != null ? new HashSet<string>(allowedTypeIds) : new HashSet<string>();
            _onSelectedWithStage = onSelectedWithStage;
            _pickerFilterSporeStage = filterSporeStage;
            _onCancel = onCancel;
            ShowInternal();
            if (_invSubtitle != null)
                _invSubtitle.text = string.IsNullOrEmpty(subtitle) ? "Seleziona un item compatibile" : subtitle;
            if (_btnCancel != null)
                _btnCancel.style.display = DisplayStyle.Flex;
            Rebuild();
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

            foreach (var slot in _playerInventory.Items)
            {
                if (slot.Items.Count == 0) continue;
                string typeId = slot.TypeId;

                if (typeId == Items.SporeGeneric)
                {
                    AddSporeRowsByStage(slot, isPicker, _pickerFilterSporeStage);
                    continue;
                }

                int qty = slot.Quantity;
                string displayName = GetItemDisplayName(typeId);
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
                string sporeInfo = GetSporeInfoText(slot);
                if (!string.IsNullOrEmpty(sporeInfo))
                {
                    var subLabel = new Label(sporeInfo);
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

                row.Add(left);
                row.Add(right);
                _list.Add(row);
            }
        }

        /// <summary>Per spore-generic: una riga per variante (Raw / Maturata). Se filterStage è valorizzato, mostra solo le righe con quello stage.</summary>
        private void AddSporeRowsByStage(InventorySlot slot, bool isPicker, SporeStage? filterStage = null)
        {
            var byStage = new Dictionary<(SporeStage?, GeneticType?), int>();
            foreach (var item in slot.Items)
            {
                var key = (item.SporeStageValue, item.GeneticTypeValue);
                if (!byStage.ContainsKey(key))
                    byStage[key] = 0;
                byStage[key]++;
            }
            foreach (var kv in byStage)
            {
                var key = kv.Key;
                if (filterStage.HasValue && key.Item1 != filterStage)
                    continue;
                int qty = kv.Value;
                string displayName = key.Item1 == SporeStage.Matured ? "Spora Maturata" : "Spora Raw";
                string subText = GetSporeSubText(key.Item1, key.Item2);
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
                    var stage = key.Item1;
                    var selectBtn = new Button(() =>
                    {
                        _onSelectedWithStage?.Invoke(Items.SporeGeneric, stage);
                        Hide();
                    }) { text = "Seleziona" };
                    selectBtn.AddToClassList("inv-select");
                    right.Add(selectBtn);
                }
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
            {
                parts.Add(genetic.Value switch
                {
                    GeneticType.Fixed => "Fissa",
                    GeneticType.Stable => "Stabile",
                    GeneticType.Unstable => "Instabile",
                    _ => genetic.Value.ToString()
                });
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        /// <summary>Nome leggibile per un typeId (semi da PlantData, Pre-Seed, altri da typeId).</summary>
        public static string GetItemDisplayName(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return typeId;
            if (typeId == Items.PreSeed) return "Pre-Seed";
            if (PlantDatabase.Instance != null)
            {
                var plantData = PlantDatabase.Instance.GetPlantDataBySeedTypeId(typeId);
                if (plantData != null)
                    return SeedInventoryMenu.GetSeedDisplayName(typeId);
            }
            return typeId;
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
            {
                parts.Add(first.GeneticTypeValue.Value switch
                {
                    GeneticType.Fixed => "Fissa",
                    GeneticType.Stable => "Stabile",
                    GeneticType.Unstable => "Instabile",
                    _ => first.GeneticTypeValue.Value.ToString()
                });
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "Spora generica";
        }
    }
}
