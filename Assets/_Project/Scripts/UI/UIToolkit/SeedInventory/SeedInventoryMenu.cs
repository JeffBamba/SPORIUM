using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using Sporae.UI.Icons;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.UI.UIToolkit.SeedInventory
{
    [RequireComponent(typeof(UIDocument))]
    public class SeedInventoryMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private ScrollView _list;
        private Button _btnClose;
        private Button _btnCancel;

        private Inventory _playerInventory;
        private readonly List<SeedEntry> _entries = new();

        public event Action<string> OnSeedSelected;
        public event Action OnCancelled;

        private class SeedEntry
        {
            public string SeedTypeId;
            public int Quantity;
            public PlantData PlantData;
        }

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder sia su UIDocument che su Canvas parent (se presente)
            // Selector modali devono stare sopra tutto, incluso PlantCard (300)
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 500;
                
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 500;
                }
            }

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("SeedInventoryMenu: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("seedinv-overlay");
            _list = _root.Q<ScrollView>("seedinv-list");
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
            // Preferiamo ServiceContainer (come UISeedSelector), fallback a FindObjectOfType
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
        }

        public string GetDisplayNameForSeed(string seedTypeId)
        {
            if (string.IsNullOrEmpty(seedTypeId))
                return seedTypeId;

            // Prova prima dalle entries (più veloce se già caricate)
            var entry = _entries.FirstOrDefault(e => e.SeedTypeId == seedTypeId);
            if (entry?.PlantData != null && !string.IsNullOrEmpty(entry.PlantData.PlantCode))
                return GetPlantDisplayName(entry.PlantData);

            // Fallback: usa PlantDatabase direttamente
            var plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(seedTypeId);
            if (plantData != null)
                return GetPlantDisplayName(plantData);

            // Ultimo fallback: typeId
            return seedTypeId;
        }
        
        /// <summary>
        /// Funzione statica helper per convertire seedTypeId in nome leggibile.
        /// Può essere usata da altri script senza istanza di SeedInventoryMenu.
        /// </summary>
        public static string GetSeedDisplayName(string seedTypeId)
        {
            if (string.IsNullOrEmpty(seedTypeId))
                return seedTypeId;

            var plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(seedTypeId);
            if (plantData != null)
                return GetPlantDisplayName(plantData);

            return seedTypeId;
        }

        private void Cancel()
        {
            OnCancelled?.Invoke();
            Hide();
        }

        private void Rebuild()
        {
            _entries.Clear();

            if (_list == null)
                return;

            _list.Clear();

            if (_playerInventory == null)
            {
                AddEmptyRow("NO INVENTORY", "GameManager/Inventory not available");
                return;
            }

            foreach (var slot in _playerInventory.Items)
            {
                if (slot.Items.Count == 0)
                    continue;

                var firstItem = slot.Items.ElementAt(0);
                if (firstItem?.ItemConfig == null || !firstItem.ItemConfig.IsSeed)
                    continue;

                var plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(slot.TypeId);
                _entries.Add(new SeedEntry
                {
                    SeedTypeId = slot.TypeId,
                    Quantity = slot.Quantity,
                    PlantData = plantData
                });
            }

            // Sort: PURE, STANDARD, EVIL, poi nome
            _entries.Sort((a, b) =>
            {
                int fa = FamilyOrder(a.PlantData);
                int fb = FamilyOrder(b.PlantData);
                int cmp = fa.CompareTo(fb);
                if (cmp != 0) return cmp;
                string na = GetPlantDisplayName(a.PlantData) ?? a.SeedTypeId;
                string nb = GetPlantDisplayName(b.PlantData) ?? b.SeedTypeId;
                return string.CompareOrdinal(na, nb);
            });

            if (_entries.Count == 0)
            {
                AddEmptyRow("NO SEEDS", "No seed available");
                return;
            }

            foreach (var e in _entries)
            {
                _list.Add(BuildRow(e));
            }
        }

        private VisualElement BuildRow(SeedEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("seedinv-row");

            var iconBox = new VisualElement();
            iconBox.AddToClassList("seedinv-row-iconbox");
            // Placeholder icona (no emoji per evitare missing glyph)
            var iconGlyph = new VisualElement();
            iconGlyph.AddToClassList("seedinv-row-iconglyph");
            ApplyIconToElement(iconGlyph, GlobalIconResolver.GetItemIcon(entry.SeedTypeId));
            iconBox.Add(iconGlyph);

            var main = new VisualElement();
            main.AddToClassList("seedinv-row-main");

            string displayName = entry.PlantData != null ? GetPlantDisplayName(entry.PlantData) : entry.SeedTypeId;
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("seedinv-seedname");
            nameLabel.AddToClassList(GetNameColorClass(entry.PlantData));

            var badges = new VisualElement();
            badges.AddToClassList("seedinv-badges");

            string familyText = entry.PlantData != null ? entry.PlantData.Family.ToString().ToUpperInvariant() : "UNKNOWN";
            var badge = new VisualElement();
            badge.AddToClassList("seedinv-badge");
            badge.AddToClassList(FamilyBadgeClass(entry.PlantData));
            var badgeText = new Label(familyText);
            badgeText.AddToClassList("seedinv-badge-text");
            badge.Add(badgeText);

            var qty = new Label($"x{entry.Quantity}");
            qty.AddToClassList("seedinv-qty");

            badges.Add(badge);

            main.Add(nameLabel);
            main.Add(badges);

            var right = new VisualElement();
            right.AddToClassList("seedinv-right");
            right.Add(qty);

            var selectBtn = new Button(() =>
            {
                OnSeedSelected?.Invoke(entry.SeedTypeId);
                Hide();
            });
            selectBtn.AddToClassList("seedinv-select");
            var selectText = new Label("SELECT →");
            selectText.AddToClassList("seedinv-select-text");
            selectBtn.Add(selectText);

            row.Add(iconBox);
            row.Add(main);
            right.Add(selectBtn);
            row.Add(right);

            return row;
        }

        private void AddEmptyRow(string title, string subtitle)
        {
            var row = new VisualElement();
            row.AddToClassList("seedinv-row");

            var iconBox = new VisualElement();
            iconBox.AddToClassList("seedinv-row-iconbox");
            var iconGlyph = new VisualElement();
            iconGlyph.AddToClassList("seedinv-row-iconglyph");
            ApplyIconToElement(iconGlyph, GlobalIconResolver.GetItemIcon(Items.Seed001));
            iconBox.Add(iconGlyph);

            var main = new VisualElement();
            main.AddToClassList("seedinv-row-main");
            var nameLabel = new Label(title);
            nameLabel.AddToClassList("seedinv-seedname");
            var sub = new Label(subtitle);
            sub.AddToClassList("seedinv-subtitle");
            main.Add(nameLabel);
            main.Add(sub);

            row.Add(iconBox);
            row.Add(main);
            _list?.Add(row);
        }

        private static int FamilyOrder(PlantData plantData)
        {
            if (plantData == null) return 99;
            return plantData.Family switch
            {
                PlantFamily.Pure => 0,
                PlantFamily.Standard => 1,
                PlantFamily.Evil => 2,
                _ => 99
            };
        }

        private static void ApplyIconToElement(VisualElement target, Sprite icon)
        {
            if (target == null || icon == null) return;
            target.style.backgroundImage = new StyleBackground(icon);
            target.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        private static string FamilyBadgeClass(PlantData plantData)
        {
            if (plantData == null) return "seedinv-badge-standard";
            return plantData.Family switch
            {
                PlantFamily.Pure => "seedinv-badge-pure",
                PlantFamily.Evil => "seedinv-badge-evil",
                PlantFamily.Standard => "seedinv-badge-standard",
                _ => "seedinv-badge-standard"
            };
        }

        private static string GetNameColorClass(PlantData plantData)
        {
            if (plantData == null) return string.Empty;
            return plantData.Family switch
            {
                PlantFamily.Pure => "seedinv-name-pure",
                PlantFamily.Standard => "seedinv-name-standard",
                PlantFamily.Evil => "seedinv-name-evil",
                _ => string.Empty
            };
        }

        private static string GetPlantDisplayName(PlantData plantData)
        {
            if (plantData == null)
                return "Unknown";

            // Copia light della mappatura usata in PlantCardV2DataBinder, per avere nomi leggibili.
            string baseName = plantData.PlantCode switch
            {
                "PLT-STD-001" => "Ferric Fern",
                "PLT-PURE-001" => "Arctic Hask",
                "PLT-EVIL-001" => "Glasscap Fungus",
                _ => plantData.name.Replace("PLT-", "").Replace("-", " ")
            };
            
            return $"{baseName} Seed";
        }
    }
}


