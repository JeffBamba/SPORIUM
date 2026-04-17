using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Riga orizzontale di Collection Box nella Compact Bottom Bar: un box per ogni evento OnItemAdded
    /// (un item raccolto = un box affiancato ai precedenti, da sinistra verso destra).
    /// Il giocatore può aprirne i dettagli in qualsiasi ordine (click sinistro); click destro sul box lo rimuove.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(20)]
    public class CollectionBoxStackController : MonoBehaviour
    {
        /// <summary>Sopra toast Foundation (150) e barra HUD (200), sotto PlantCard (600).</summary>
        private const float CollectionDetailSortOrder = 350f;

        [Header("UI Toolkit")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Templates")]
        [SerializeField] private VisualTreeAsset _collectionBoxTemplate;
        [SerializeField] private VisualTreeAsset _collectionDetailTemplate;

        [Header("Configuration")]
        [Tooltip("Numero massimo di box visibili in fila; oltre questo si rimuove il più vecchio (uno per item raccolto).")]
        [SerializeField] private int _maxBoxes = 5;

        private VisualElement _stack;
        private VisualElement _detailOverlay;
        private float _restoreUiDocumentSortOrder = 200f;
        private VisualElement _detailPanel;
        private Label _detailName;
        private Label _detailTypeId;
        private Label _detailQty;
        private Label _detailRoom;
        private VisualElement _detailIcon;

        private VisualElement _rowGenetic;
        private VisualElement _rowMutate;
        private VisualElement _rowStage;
        private VisualElement _rowFamily;
        private VisualElement _rowSource;
        private VisualElement _rowQualityMeta;
        private VisualElement _rowActive;
        private VisualElement _rowPassive;
        private Label _valGenetic;
        private Label _valMutate;
        private Label _valStage;
        private Label _valFamily;
        private Label _valSource;
        private Label _valQualityMeta;
        private Label _valActive;
        private Label _valPassive;

        private FoundationNotificationService _foundation;
        private readonly List<VisualElement> _boxes = new();
        private bool _initialized;

        private void OnEnable()
        {
            // Dopo disattivazione/riattivazione GO, riaggancia l'handler senza duplicati.
            if (_initialized)
                SubscribeToFoundation();
        }

        private void OnDisable()
        {
            if (_foundation != null)
                _foundation.OnItemAdded -= OnItemAdded;
        }

        private IEnumerator Start()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            // UIDocument root / albero UIToolkit può non essere pronto nel primo Start frame.
            yield return null;
            yield return null;

            TryInitializeFromTree();
        }

        private void OnDestroy()
        {
            if (_foundation != null)
                _foundation.OnItemAdded -= OnItemAdded;
        }

        private void TryInitializeFromTree()
        {
            if (_initialized) return;

            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (root == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[CollectionBoxStack] UIDocument root null; Collection stack non inizializzato.");
                return;
            }

            _stack = root.Q<VisualElement>("collection-box-stack");
            if (_stack == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[CollectionBoxStack] 'collection-box-stack' not found in UIDocument.");
                return;
            }

            if (_uiDocument != null)
                _restoreUiDocumentSortOrder = _uiDocument.sortingOrder;

            SetupDetailPanel(root);
            SubscribeToFoundation();
            _initialized = true;
        }

        // ── Foundation subscription ──

        private void SubscribeToFoundation()
        {
            _foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (_foundation == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[CollectionBoxStack] FoundationNotificationService non disponibile.");
                return;
            }
            _foundation.OnItemAdded -= OnItemAdded;
            _foundation.OnItemAdded += OnItemAdded;
        }

        // ── Detail panel setup ──

        private void SetupDetailPanel(VisualElement root)
        {
            _detailOverlay = root.Q<VisualElement>("collection-detail-overlay");
            if (_detailOverlay == null)
            {
                _detailOverlay = new VisualElement { name = "collection-detail-overlay", pickingMode = PickingMode.Position };
                _detailOverlay.AddToClassList("cbb-collection-detail-overlay");
                root.Add(_detailOverlay);
            }

            if (_collectionDetailTemplate != null)
            {
                var detailInstance = _collectionDetailTemplate.Instantiate();
                _detailOverlay.Add(detailInstance);
                _detailPanel = detailInstance.Q<VisualElement>("collection-detail");
            }
            else
            {
                _detailPanel = root.Q<VisualElement>("collection-detail");
            }

            if (_detailPanel == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[CollectionBoxStack] CollectionDetail panel non trovato.");
                return;
            }

            _detailName   = _detailPanel.Q<Label>("cdetail-name");
            _detailTypeId = _detailPanel.Q<Label>("cdetail-typeid");
            _detailQty    = _detailPanel.Q<Label>("cdetail-qty");
            _detailRoom   = _detailPanel.Q<Label>("cdetail-room");
            _detailIcon   = _detailPanel.Q<VisualElement>("cdetail-icon");

            _rowGenetic = _detailPanel.Q<VisualElement>("cdetail-row-genetic");
            _rowMutate = _detailPanel.Q<VisualElement>("cdetail-row-mutate");
            _rowStage = _detailPanel.Q<VisualElement>("cdetail-row-stage");
            _rowFamily = _detailPanel.Q<VisualElement>("cdetail-row-family");
            _rowSource = _detailPanel.Q<VisualElement>("cdetail-row-source");
            _rowQualityMeta = _detailPanel.Q<VisualElement>("cdetail-row-quality");
            _rowActive = _detailPanel.Q<VisualElement>("cdetail-row-active");
            _rowPassive = _detailPanel.Q<VisualElement>("cdetail-row-passive");
            _valGenetic = _detailPanel.Q<Label>("cdetail-val-genetic");
            _valMutate = _detailPanel.Q<Label>("cdetail-val-mutate");
            _valStage = _detailPanel.Q<Label>("cdetail-val-stage");
            _valFamily = _detailPanel.Q<Label>("cdetail-val-family");
            _valSource = _detailPanel.Q<Label>("cdetail-val-source");
            _valQualityMeta = _detailPanel.Q<Label>("cdetail-val-quality");
            _valActive = _detailPanel.Q<Label>("cdetail-val-active");
            _valPassive = _detailPanel.Q<Label>("cdetail-val-passive");

            // Click destro sul detail panel per chiuderlo
            _detailPanel.RegisterCallback<ContextClickEvent>(_ => HideDetail());
            // Click sullo sfondo (fuori dalla scheda) chiude come modale
            _detailOverlay.RegisterCallback<PointerDownEvent>(OnCollectionOverlayPointerDown, TrickleDown.TrickleDown);

            _detailOverlay.style.display = DisplayStyle.None;
        }

        private void OnCollectionOverlayPointerDown(PointerDownEvent evt)
        {
            if (_detailOverlay == null) return;
            if (!(evt.target is VisualElement ve)) return;
            if (_detailPanel != null && (_detailPanel == ve || _detailPanel.Contains(ve)))
                return;
            HideDetail();
        }

        // ── Item received ──

        private void OnItemAdded(NotificationPayload payload)
        {
            // Se la pila è piena, rimuovi il box più vecchio
            if (_boxes.Count >= _maxBoxes)
                RemoveBox(_boxes[0]);

            CreateBox(payload);
        }

        private void CreateBox(NotificationPayload payload)
        {
            // container = elemento tracciato e animato (mantiene il suo stylesheet)
            // inner     = "collection-box" dentro il template, usato per query di icona/qty
            VisualElement container;
            VisualElement inner;

            if (_collectionBoxTemplate != null)
            {
                // IMPORTANTE: aggiungiamo l'intero TemplateContainer allo stack, NON il figlio estratto.
                // Se si reparenta il figlio fuori dal TemplateContainer, il CollectionBox.uss cessa
                // di applicarsi e il box risulta invisibile (nessuno stile risolto).
                var instance = _collectionBoxTemplate.Instantiate();
                inner = instance.Q<VisualElement>("collection-box") ?? instance;

                // Il TemplateContainer è trasparente visivamente; il figlio "collection-box"
                // ha già width/height espliciti → il container si adatta.
                instance.style.flexShrink = 0;
                instance.style.flexGrow   = 0;
                container = instance;
            }
            else
            {
                inner = new VisualElement();
                inner.AddToClassList("cbox-root");
                container = inner;
            }

            // Icona (payload da PostAddedToInventory; fallback resolver come il toast Foundation)
            var iconEl = inner.Q<VisualElement>("cbox-icon");
            if (iconEl != null)
            {
                var sprite = payload.ItemIcon;
                if (sprite == null && !string.IsNullOrEmpty(payload.ItemTypeId))
                    sprite = NotificationItemIconResolver.GetIcon(payload.ItemTypeId);
                if (sprite != null)
                    iconEl.style.backgroundImage = new StyleBackground(sprite);
            }

            // Quantità badge
            var qtyLabel = inner.Q<Label>("cbox-qty");
            if (qtyLabel != null)
                qtyLabel.text = payload.ItemQuantity > 1 ? payload.ItemQuantity.ToString() : string.Empty;

            // Payload accessibile via userData (usato in ShowDetail)
            container.userData = payload;

            // Click sinistro → apri scheda dettaglio
            inner.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    ShowDetail(payload);
                    evt.StopPropagation();
                }
            });

            // Tasto destro (button==1) sul box → dismiss immediato con fade-out
            inner.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                evt.StopPropagation();
                DismissBox(container);
            });

            // Aggiungi alla stack PRIMA dell'animazione (il layout deve registrare posizione finale)
            _stack.Add(container);
            _boxes.Add(container);

            // Animazione slide-in: parte 56px a destra e trasparente
            container.style.translate        = new Translate(new Length(56, LengthUnit.Pixel), 0);
            container.style.opacity          = 0f;
            container.style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("translate"),
                new StylePropertyName("opacity"),
            };
            container.style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(280, TimeUnit.Millisecond),
                new TimeValue(180, TimeUnit.Millisecond),
            };
            container.style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseOut),
                new EasingFunction(EasingMode.EaseOut),
            };

            // Frame successivo: scatta la transizione verso posizione finale
            container.schedule.Execute(() =>
            {
                container.style.translate = new Translate(0, 0);
                container.style.opacity   = 1f;
            }).ExecuteLater(0);
        }

        /// <summary>Rimuove il box con una breve animazione fade-out, poi lo elimina dal DOM.</summary>
        private void DismissBox(VisualElement container)
        {
            if (!_boxes.Contains(container)) return;

            // Chiude la scheda dettaglio se era aperta su questo box
            HideDetail();

            // Rimuovi subito dalla lista logica (evita doppi dismiss)
            _boxes.Remove(container);

            // Transizione fade-out rapida
            container.style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("opacity"),
                new StylePropertyName("translate"),
            };
            container.style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(160, TimeUnit.Millisecond),
                new TimeValue(160, TimeUnit.Millisecond),
            };
            container.style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseIn),
                new EasingFunction(EasingMode.EaseIn),
            };
            container.style.opacity   = 0f;
            container.style.translate = new Translate(new Length(20, LengthUnit.Pixel), 0);

            // Rimuove dal DOM dopo la transizione
            container.schedule.Execute(() => _stack.Remove(container))
                     .ExecuteLater(170);
        }

        private void RemoveBox(VisualElement box)
        {
            if (_boxes.Remove(box))
                _stack.Remove(box);
        }

        // ── Detail panel ──

        private void ShowDetail(NotificationPayload payload)
        {
            if (_detailPanel == null) return;

            if (_detailName   != null) _detailName.text   = payload.ItemName ?? "—";
            if (_detailTypeId != null) _detailTypeId.text = payload.ItemTypeId ?? "—";
            if (_detailQty    != null) _detailQty.text    = payload.ItemQuantity.ToString();
            if (_detailRoom   != null) _detailRoom.text   = payload.ItemLocation ?? "—";

            if (_detailIcon != null)
            {
                var sprite = payload.ItemIcon;
                if (sprite == null && !string.IsNullOrEmpty(payload.ItemTypeId))
                    sprite = NotificationItemIconResolver.GetIcon(payload.ItemTypeId);
                if (sprite != null)
                    _detailIcon.style.backgroundImage = new StyleBackground(sprite);
            }

            ApplyDetailMetadataRows(payload);

            _detailPanel.style.display = DisplayStyle.Flex;
            if (_detailOverlay != null)
            {
                _detailOverlay.style.display = DisplayStyle.Flex;
                _detailOverlay.BringToFront();
            }
            if (_uiDocument != null)
                _uiDocument.sortingOrder = CollectionDetailSortOrder;
        }

        private void HideDetail()
        {
            if (_detailOverlay != null)
                _detailOverlay.style.display = DisplayStyle.None;
            if (_detailPanel != null)
                _detailPanel.style.display = DisplayStyle.None;
            if (_uiDocument != null)
                _uiDocument.sortingOrder = _restoreUiDocumentSortOrder;
        }

        private void ApplyDetailMetadataRows(NotificationPayload payload)
        {
            SetMetaRow(_rowGenetic, _valGenetic, payload, CollectionPayloadFactory.MetaGenetic);
            SetMetaRow(_rowMutate, _valMutate, payload, CollectionPayloadFactory.MetaMutatePct);
            SetMetaRow(_rowStage, _valStage, payload, CollectionPayloadFactory.MetaStage);
            SetMetaRow(_rowFamily, _valFamily, payload, CollectionPayloadFactory.MetaFamily);
            SetMetaRow(_rowSource, _valSource, payload, CollectionPayloadFactory.MetaSource);
            SetMetaRow(_rowQualityMeta, _valQualityMeta, payload, CollectionPayloadFactory.MetaQuality);
            SetMetaRow(_rowActive, _valActive, payload, CollectionPayloadFactory.MetaActive);
            SetMetaRow(_rowPassive, _valPassive, payload, CollectionPayloadFactory.MetaPassive);
        }

        private static void SetMetaRow(VisualElement row, Label val, NotificationPayload payload, string key)
        {
            if (row == null || val == null) return;
            string text = null;
            if (payload?.Args != null && payload.Args.TryGetValue(key, out var t))
                text = t;
            if (string.IsNullOrWhiteSpace(text) || text == "—")
            {
                row.style.display = DisplayStyle.None;
                return;
            }
            row.style.display = DisplayStyle.Flex;
            val.text = text;
        }
    }
}
