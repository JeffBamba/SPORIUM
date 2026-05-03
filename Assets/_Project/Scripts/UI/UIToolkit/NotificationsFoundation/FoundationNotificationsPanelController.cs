using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using System;
using System.Linq;
using Sporae.Core.Localization;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Controller UI Toolkit per il pannello Notifications (reference UI).
    /// Nota: i riferimenti ai StyleSheet vanno assegnati via Inspector (ex novo, non rompe nulla).
    /// </summary>
    public sealed class FoundationNotificationsPanelController : MonoBehaviour
    {
        [Header("UI Toolkit References")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("StyleSheets (assign in Inspector)")]
        [SerializeField] private StyleSheet _spFoundation;
        [SerializeField] private StyleSheet _spPanelBase;
        [SerializeField] private StyleSheet _notificationsUss;

        [Header("Behavior")]
        [SerializeField] private bool _startExpanded = true;
        #pragma warning disable CS0414
        [SerializeField] private bool _enableDebugLogs = false; // Reserved for future debug toggles
#pragma warning restore CS0414
        [Tooltip("Sprite mostrato nel box icona quando l'item non ha icona (es. Icona_placeholder).")]
        [SerializeField] private Sprite _itemIconPlaceholder;

        private FoundationNotificationService _service;

        private VisualElement _root;
        private Button _headerButton;
        private VisualElement _badge;
        private Label _badgeText;
        private Label _chevron;
        private Label _headerTitleLabel;
        private VisualElement _list;
        private VisualElement _toastTooltip;
        private Label _toastTooltipLabel;

        private RowUI[] _rows = new RowUI[5];
        private bool _expanded;
        private bool _languageSubscribed;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument != null)
            {
                // Ordine relativo agli altri UIDocument: vale solo se condividono lo stesso Panel Settings
                // (vedi SCN_VaultMap: Notifications Foundation usa PlayerStatusPanelSettings come TopBar/Bottom).
                // Sotto TopBar/Bottom (200): tooltip sopra i toast; sopra viewport (100) e PlayerStatus (50).
                _uiDocument.sortingOrder = 150;
            }
        }

        private void OnEnable()
        {
            _service = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            if (_service != null)
                _service.OnChanged += Refresh;

            if (!_languageSubscribed)
            {
                GameLanguageSettings.OnLanguageChanged += OnLanguageChanged;
                _languageSubscribed = true;
            }

            SetupUI();
            ApplyLocalizedHeaderTitle();
            _expanded = _startExpanded;
            ApplyExpandedState();
            Refresh();
        }

        private void OnDisable()
        {
            if (_service != null)
                _service.OnChanged -= Refresh;
            if (_languageSubscribed)
            {
                GameLanguageSettings.OnLanguageChanged -= OnLanguageChanged;
                _languageSubscribed = false;
            }
        }

        private void OnLanguageChanged(GameLanguage _) => ApplyLocalizedHeaderTitle();

        private void ApplyLocalizedHeaderTitle()
        {
            if (_headerTitleLabel != null)
                _headerTitleLabel.text = LocalizationManager.GetString("notifications.title");
        }

        private void Update()
        {
            bool hideFixedHud = GameplayUiModalLock.HidesContextHud;
            if (_root != null)
                _root.style.display = hideFixedHud ? DisplayStyle.None : DisplayStyle.Flex;
            if (hideFixedHud && _toastTooltip != null)
                _toastTooltip.style.display = DisplayStyle.None;
        }

        private void SetupUI()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            // Attach styles (serialized refs)
            if (_spFoundation != null && !_root.styleSheets.Contains(_spFoundation))
                _root.styleSheets.Add(_spFoundation);
            if (_spPanelBase != null && !_root.styleSheets.Contains(_spPanelBase))
                _root.styleSheets.Add(_spPanelBase);
            if (_notificationsUss != null && !_root.styleSheets.Contains(_notificationsUss))
                _root.styleSheets.Add(_notificationsUss);

            var nfRoot = _root.Q<VisualElement>("nf-root");
            if (nfRoot != null) _root = nfRoot;

            _headerButton = _root.Q<Button>("nf-header");
            _badge = _root.Q<VisualElement>("nf-badge");
            _badgeText = _root.Q<Label>("nf-badge-text");
            _chevron = _root.Q<Label>("nf-chevron");
            _list = _root.Q<VisualElement>("nf-list");
            _headerTitleLabel = _root.Q<Label>("nf-header-title");

            _toastTooltip = new VisualElement();
            _toastTooltip.AddToClassList("nf-toast-tooltip");
            _toastTooltip.style.display = DisplayStyle.None;
            _toastTooltip.pickingMode = PickingMode.Ignore;
            _toastTooltipLabel = new Label();
            _toastTooltipLabel.AddToClassList("nf-toast-tooltip__text");
            _toastTooltipLabel.style.whiteSpace = WhiteSpace.Normal;
            _toastTooltip.Add(_toastTooltipLabel);
            _root.Add(_toastTooltip);

            _rows[0] = RowUI.Bind(_root, 0, _toastTooltip, _toastTooltipLabel);
            _rows[1] = RowUI.Bind(_root, 1, _toastTooltip, _toastTooltipLabel);
            _rows[2] = RowUI.Bind(_root, 2, _toastTooltip, _toastTooltipLabel);
            _rows[3] = RowUI.Bind(_root, 3, _toastTooltip, _toastTooltipLabel);
            _rows[4] = RowUI.Bind(_root, 4, _toastTooltip, _toastTooltipLabel);

            if (_headerButton != null)
            {
                _headerButton.clicked -= ToggleExpanded;
                _headerButton.clicked += ToggleExpanded;
            }
        }

        private void ToggleExpanded()
        {
            _expanded = !_expanded;
            ApplyExpandedState();
        }

        private void ApplyExpandedState()
        {
            if (_root == null) return;
            _root.EnableInClassList("nf-collapsed", !_expanded);
            if (_chevron != null)
                _chevron.text = _expanded ? "^" : "v";
        }

        private void Refresh()
        {
            if (_service == null || _root == null) return;

            // Badge
            var badgeCount = _service.GetBadgeCount();
            if (_badgeText != null)
                _badgeText.text = badgeCount.ToString();
            if (_badge != null)
                _badge.style.display = badgeCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            // Chevron: se non ci sono notifiche, non mostrare l'espansione
            if (_chevron != null)
                _chevron.style.display = badgeCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            // Header: danger/warning vincono; altrimenti se c’è un toast MIS-* informativo (≠ success) usa cyan mission recap.
            var headerSev = _service.GetHeaderSeverity();
            if ((int)headerSev >= (int)NotificationSeverity.Warning)
                ApplySeverityClass(_headerButton, headerSev);
            else if (AnyVisibleMissionCyanAccent())
                ApplyMissionPanelClass(_headerButton);
            else
                ApplySeverityClass(_headerButton, headerSev);

            // Visible rows (max 5, danger pinned)
            var rows = _service.GetVisibleRows();
            for (int i = 0; i < _rows.Length; i++)
            {
                if (i < rows.Count)
                {
                    _rows[i].Show(rows[i], _itemIconPlaceholder);
                }
                else
                {
                    _rows[i].Hide();
                }
            }
        }

        /// <summary>MIS-* con severità diversa da Success (es. nuova missione) usano l’accento ciano recap; completamento = verde success.</summary>
        private bool AnyVisibleMissionCyanAccent()
        {
            if (_service == null) return false;
            foreach (var e in _service.GetVisibleRows())
            {
                if (string.IsNullOrEmpty(e.Code) || !e.Code.StartsWith("MIS-", StringComparison.Ordinal))
                    continue;
                if (e.Severity == NotificationSeverity.Success)
                    continue;
                return true;
            }
            return false;
        }

        private static void ApplyMissionPanelClass(VisualElement el)
        {
            if (el == null) return;
            el.EnableInClassList("nf-sev-info", false);
            el.EnableInClassList("nf-sev-success", false);
            el.EnableInClassList("nf-sev-warning", false);
            el.EnableInClassList("nf-sev-danger", false);
            el.EnableInClassList("nf-sev-mission", true);
        }

        private static void ApplySeverityClass(VisualElement el, NotificationSeverity severity)
        {
            if (el == null) return;
            el.EnableInClassList("nf-sev-mission", false);
            el.EnableInClassList("nf-sev-info", severity == NotificationSeverity.Info);
            el.EnableInClassList("nf-sev-success", severity == NotificationSeverity.Success);
            el.EnableInClassList("nf-sev-warning", severity == NotificationSeverity.Warning);
            el.EnableInClassList("nf-sev-danger", severity == NotificationSeverity.Danger);
        }

        private sealed class RowUI
        {
            public VisualElement Root;
            public VisualElement Iconbox;
            public Label Icon;
            public Label Code;
            public Label Msg;
            public VisualElement ToastTooltip;
            public Label ToastTooltipLabel;
            public VisualElement ItemLayout;
            public VisualElement ItemIconBox;
            public VisualElement ItemIcon;
            public Label ItemTitle;
            public Label Qty;
            public Label ItemName;
            public VisualElement RoomIcon;
            public Label Room;
            public int CurrentEntryId;
            public bool HasCurrent;
            public string TooltipText;

            public static RowUI Bind(VisualElement root, int idx, VisualElement toastTooltip, Label toastTooltipLabel)
            {
                var row = root.Q<VisualElement>($"nf-row-{idx}");
                if (row != null) row.style.display = DisplayStyle.None;
                return new RowUI
                {
                    Root = row,
                    ToastTooltip = toastTooltip,
                    ToastTooltipLabel = toastTooltipLabel,
                    Iconbox = row?.Q<VisualElement>($"nf-row-{idx}-iconbox"),
                    Icon = row?.Q<Label>($"nf-row-{idx}-icon"),
                    Code = row?.Q<Label>($"nf-row-{idx}-code"),
                    Msg = row?.Q<Label>($"nf-row-{idx}-msg"),
                    ItemLayout = row?.Q<VisualElement>($"nf-row-{idx}-item-layout"),
                    ItemIconBox = row?.Q<VisualElement>($"nf-row-{idx}-item-icon-box"),
                    ItemIcon = row?.Q<VisualElement>($"nf-row-{idx}-item-icon"),
                    ItemTitle = row?.Q<Label>($"nf-row-{idx}-title"),
                    Qty = row?.Q<Label>($"nf-row-{idx}-qty"),
                    ItemName = row?.Q<Label>($"nf-row-{idx}-item-name"),
                    RoomIcon = row?.Q<VisualElement>($"nf-row-{idx}-room-icon"),
                    Room = row?.Q<Label>($"nf-row-{idx}-room")
                };
            }

            public void Hide()
            {
                if (Root == null) return;
                if (Root.style.display == DisplayStyle.None) return;

                Root.UnregisterCallback<MouseEnterEvent>(OnRowMouseEnter);
                Root.UnregisterCallback<MouseLeaveEvent>(OnRowMouseLeave);
                Root.RemoveFromClassList("nf-anim-enter");
                Root.RemoveFromClassList("nf-row--item-layout");
                Root.AddToClassList("nf-anim-exit");

                var exitingId = HasCurrent ? CurrentEntryId : -1;
                var exitingCode = Code != null ? Code.text : "";

                EventCallback<TransitionEndEvent> onEnd = null;
                onEnd = (e) =>
                {
                    Root.style.display = DisplayStyle.None;
                    Root.RemoveFromClassList("nf-anim-exit");
                    HasCurrent = false;
                    CurrentEntryId = 0;
                    Root.UnregisterCallback(onEnd);
                };
                Root.RegisterCallback(onEnd);
            }

            public void Show(NotificationEntry entry, Sprite placeholderSprite = null)
            {
                if (Root == null) return;
                Root.style.display = DisplayStyle.Flex;

                var isItemLayout = entry.Spec != null && entry.Spec.IsItemLayout && entry.Payload != null;

                TooltipText = NotificationLocalization.Format(NotificationLocalization.ResolveTooltip(entry.Spec), entry.Payload?.Args);
                if (ToastTooltip != null && Root != null)
                {
                    Root.UnregisterCallback<MouseEnterEvent>(OnRowMouseEnter);
                    Root.UnregisterCallback<MouseLeaveEvent>(OnRowMouseLeave);
                    if (!string.IsNullOrWhiteSpace(TooltipText))
                    {
                        Root.RegisterCallback<MouseEnterEvent>(OnRowMouseEnter);
                        Root.RegisterCallback<MouseLeaveEvent>(OnRowMouseLeave);
                    }
                }

                if (isItemLayout)
                {
                    Root.AddToClassList("nf-row--item-layout");
                    if (Iconbox != null) Iconbox.style.display = DisplayStyle.None;
                    if (Code != null && Code.parent != null) Code.parent.style.display = DisplayStyle.None;
                    if (ItemLayout != null) ItemLayout.style.display = DisplayStyle.Flex;

                    var p = entry.Payload;
                    if (ItemTitle != null) ItemTitle.text = NotificationLocalization.GetAddedToInventoryTitle();
                    if (Qty != null) Qty.text = ("+" + p.ItemQuantity).ToUpperInvariant();
                    if (ItemName != null) ItemName.text = p.ItemName ?? "";
                    if (Room != null) Room.text = p.ItemLocation ?? "";

                    if (ItemIcon != null)
                    {
                        Sprite sprite = null;
                        if (p.ItemIcon != null)
                            sprite = p.ItemIcon;
                        else if (placeholderSprite == null)
                            sprite = NotificationItemIconResolver.GetIcon(p.ItemTypeId, p.ItemSporeStage);
                        Texture2D tex = null;
                        if (sprite != null && sprite.texture != null)
                            tex = sprite.texture;
                        else if (placeholderSprite != null && placeholderSprite.texture != null)
                            tex = placeholderSprite.texture;
                        if (tex != null)
                        {
                            ItemIcon.style.backgroundImage = Background.FromTexture2D(tex);
                        }
                        else
                        {
                            var fallbackSprite = Resources.Load<Sprite>("icona_Placeholder") ?? Resources.Load<Sprite>("Icons/Items/placeholder");
                            var fallbackTex = fallbackSprite != null ? fallbackSprite.texture : Resources.Load<Texture2D>("icona_Placeholder");
                            if (fallbackTex != null)
                                ItemIcon.style.backgroundImage = Background.FromTexture2D(fallbackTex);
                            else
                                ItemIcon.style.backgroundImage = new StyleBackground(StyleKeyword.Initial);
                        }
                    }
                }
                else
                {
                    Root.RemoveFromClassList("nf-row--item-layout");
                    if (Iconbox != null) Iconbox.style.display = DisplayStyle.Flex;
                    if (Code != null && Code.parent != null) Code.parent.style.display = DisplayStyle.Flex;
                    if (ItemLayout != null) ItemLayout.style.display = DisplayStyle.None;

                    if (IsMissionNotificationCode(entry.Code) && entry.Severity != NotificationSeverity.Success)
                        ApplyMissionRowClass(Root);
                    else
                        ApplySeverityClass(Root, entry.Severity);
                    ApplyCodeClass(Root, entry.Code);

                    if (Code != null) Code.text = entry.Code ?? "N/A";
                    if (Msg != null) Msg.text = entry.Message ?? string.Empty;
                    if (Icon != null) Icon.text = IconFor(entry.Severity, entry.Code);
                }

                var isNew = !HasCurrent || CurrentEntryId != entry.Id;
                HasCurrent = true;
                CurrentEntryId = entry.Id;

                if (isNew)
                {
                    Root.RemoveFromClassList("nf-anim-exit");
                    Root.AddToClassList("nf-anim-enter");

                    Root.schedule.Execute(() =>
                    {
                        Root.RemoveFromClassList("nf-anim-enter");
                    });
                }
            }

            private void OnRowMouseEnter(MouseEnterEvent evt)
            {
                if (ToastTooltip == null || ToastTooltipLabel == null || string.IsNullOrWhiteSpace(TooltipText)) return;
                ToastTooltipLabel.text = TooltipText.Replace("\\n", "\n");
                ToastTooltip.style.display = DisplayStyle.Flex;
                var rowWorld = Root.worldBound;
                var rootParent = ToastTooltip.parent;
                if (rootParent != null)
                {
                    var localPos = rootParent.WorldToLocal(new Vector2(rowWorld.x, rowWorld.yMax + 4f));
                    ToastTooltip.style.left = localPos.x;
                    ToastTooltip.style.top = localPos.y;
                    ToastTooltip.style.position = Position.Absolute;
                }
            }

            private void OnRowMouseLeave(MouseLeaveEvent evt)
            {
                if (ToastTooltip != null)
                    ToastTooltip.style.display = DisplayStyle.None;
            }
            
            private static bool IsMissionNotificationCode(string code) =>
                !string.IsNullOrEmpty(code) && code.StartsWith("MIS-", StringComparison.Ordinal);

            private static void ApplyMissionRowClass(VisualElement root)
            {
                if (root == null) return;
                root.EnableInClassList("nf-sev-info", false);
                root.EnableInClassList("nf-sev-success", false);
                root.EnableInClassList("nf-sev-warning", false);
                root.EnableInClassList("nf-sev-danger", false);
                root.EnableInClassList("nf-sev-mission", true);
            }

            private static void ApplyCodeClass(VisualElement el, string code)
            {
                if (el == null) return;
                bool isVisitor = string.Equals(code, "VIS-001", StringComparison.Ordinal);
                el.EnableInClassList("nf-code-vis", isVisitor);
            }

            private static string IconFor(NotificationSeverity severity, string code)
            {
                if (IsMissionNotificationCode(code))
                    return severity == NotificationSeverity.Success ? "+" : "★";
                return severity switch
                {
                    NotificationSeverity.Info => "i",
                    NotificationSeverity.Success => "+",
                    NotificationSeverity.Warning => "!",
                    NotificationSeverity.Danger => "!",
                    _ => "i"
                };
            }
        }
    }
}


