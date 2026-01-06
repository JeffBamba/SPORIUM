using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit.AdditiveSelector
{
    [RequireComponent(typeof(UIDocument))]
    public class AdditiveSelectorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private ScrollView _list;
        private Button _btnClose;
        private Button _btnCancel;

        private Inventory _playerInventory;

        public event Action<string> OnAdditiveSelected;
        public event Action OnCancelled;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            // DEBUG_SAFE_FIX: Imposta sortingOrder sia su UIDocument che su Canvas parent (se presente)
            // Selector modali devono stare sopra tutto, incluso PlantCard (300)
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 500;
                
                // Se c'è un Canvas parent, imposta anche il suo sortingOrder
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 500;
                }
            }

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("AdditiveSelectorController: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("addsel-overlay");
            _list = _root.Q<ScrollView>("addsel-list");
            _btnClose = _root.Q<Button>("btn-close");
            _btnCancel = _root.Q<Button>("btn-cancel");

            if (_btnClose != null) _btnClose.clicked += Cancel;
            if (_btnCancel != null) _btnCancel.clicked += Cancel;

            TryBindInventory();
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

        public bool IsVisible => _overlay != null && _overlay.style.display != DisplayStyle.None;

        public void Show()
        {
            Rebuild();
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
        }

        private void Cancel()
        {
            OnCancelled?.Invoke();
            Hide();
        }

        private void Rebuild()
        {
            if (_list == null)
                return;

            _list.Clear();

            if (_playerInventory == null)
            {
                AddEmptyRow("NO INVENTORY", "GameManager/Inventory not available");
                return;
            }

            // DEBUG: Log per diagnosticare il problema
            Debug.Log($"[AdditiveSelector] Rebuild - Inventory items count: {_playerInventory.Items.Count}");
            foreach (var slot in _playerInventory.Items)
            {
                if (slot != null)
                {
                    Debug.Log($"[AdditiveSelector] Inventory slot: TypeId='{slot.TypeId}', Qty={slot.Quantity}");
                }
            }

            // Compat: se il giocatore ha ancora lo spray legacy STR-004, lo trattiamo come Additivo Basico
            int qtyBasic = GetQty(Items.AdditiveBasic);
            int qtyLegacySpray = GetQty(Items.SprayAntifungal);
            int qtyAcid = GetQty(Items.AdditiveAcid);
            
            Debug.Log($"[AdditiveSelector] Qty Basic: {qtyBasic}, Qty Legacy Spray: {qtyLegacySpray}, Qty Acid: {qtyAcid}");
            Debug.Log($"[AdditiveSelector] Looking for: AdditiveBasic='{Items.AdditiveBasic}', AdditiveAcid='{Items.AdditiveAcid}'");
            
            string basicTypeIdToConsume = qtyBasic > 0 ? Items.AdditiveBasic : Items.SprayAntifungal;
            int basicQtyToShow = qtyBasic + qtyLegacySpray;

            var entries = new List<(string typeId, string name, string desc, string nameClass, int qty)>
            {
                (basicTypeIdToConsume, "ADDITIVO BASICO", "pH +5 • Riduce muffe", "addsel-name-basic", basicQtyToShow),
                (Items.AdditiveAcid,    "ADDITIVO ACIDO",  "pH -5 • Aumenta muffe", "addsel-name-acid",  qtyAcid),
            };

            // Mostra solo quelli disponibili
            int shown = 0;
            foreach (var e in entries)
            {
                Debug.Log($"[AdditiveSelector] Entry: {e.name}, Qty: {e.qty}, Will show: {e.qty > 0}");
                if (e.qty <= 0)
                    continue;

                _list.Add(BuildRow(e.typeId, e.name, e.desc, e.nameClass, e.qty));
                shown++;
            }

            if (shown == 0)
            {
                AddEmptyRow("NO ADDITIVES", "No additive available");
            }
        }

        private int GetQty(string typeId)
        {
            if (_playerInventory == null || string.IsNullOrEmpty(typeId))
                return 0;

            foreach (var slot in _playerInventory.Items)
            {
                if (slot != null)
                {
                    // DEBUG: Log per diagnosticare matching
                    bool matches = slot.TypeId == typeId;
                    if (typeId == Items.AdditiveAcid || typeId == Items.AdditiveBasic)
                    {
                        Debug.Log($"[AdditiveSelector] GetQty checking: slot.TypeId='{slot.TypeId}' vs typeId='{typeId}', matches={matches}");
                    }
                    
                    if (matches)
                        return slot.Quantity;
                }
            }

            return 0;
        }

        private VisualElement BuildRow(string typeId, string name, string desc, string nameClass, int qty)
        {
            var row = new VisualElement();
            row.AddToClassList("addsel-row");

            var iconBox = new VisualElement();
            iconBox.AddToClassList("addsel-row-iconbox");
            var iconGlyph = new VisualElement();
            iconGlyph.AddToClassList("addsel-row-iconglyph");
            iconBox.Add(iconGlyph);

            var main = new VisualElement();
            main.AddToClassList("addsel-row-main");

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("addsel-name");
            if (!string.IsNullOrEmpty(nameClass))
                nameLabel.AddToClassList(nameClass);

            var descLabel = new Label(desc);
            descLabel.AddToClassList("addsel-desc");

            main.Add(nameLabel);
            main.Add(descLabel);

            var right = new VisualElement();
            right.AddToClassList("addsel-right");

            var qtyLabel = new Label($"x{qty}");
            qtyLabel.AddToClassList("addsel-qty");

            var selectBtn = new Button(() =>
            {
                OnAdditiveSelected?.Invoke(typeId);
                Hide();
            });
            selectBtn.AddToClassList("addsel-select");
            var selectText = new Label("APPLY →");
            selectText.AddToClassList("addsel-select-text");
            selectBtn.Add(selectText);

            right.Add(qtyLabel);
            right.Add(selectBtn);

            row.Add(iconBox);
            row.Add(main);
            row.Add(right);

            return row;
        }

        private void AddEmptyRow(string title, string subtitle)
        {
            var row = new VisualElement();
            row.AddToClassList("addsel-row");

            var iconBox = new VisualElement();
            iconBox.AddToClassList("addsel-row-iconbox");
            var iconGlyph = new VisualElement();
            iconGlyph.AddToClassList("addsel-row-iconglyph");
            iconBox.Add(iconGlyph);

            var main = new VisualElement();
            main.AddToClassList("addsel-row-main");
            var nameLabel = new Label(title);
            nameLabel.AddToClassList("addsel-name");
            var sub = new Label(subtitle);
            sub.AddToClassList("addsel-subtitle");
            main.Add(nameLabel);
            main.Add(sub);

            row.Add(iconBox);
            row.Add(main);
            _list?.Add(row);
        }
    }
}


