using UnityEngine;
using UnityEngine.UIElements;
using _Project.Sporae.Core;
using System;
using System.Linq;

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
        [SerializeField] private bool _enableDebugLogs = false;

        private FoundationNotificationService _service;

        private VisualElement _root;
        private Button _headerButton;
        private VisualElement _badge;
        private Label _badgeText;
        private Label _chevron;
        private VisualElement _list;

        private RowUI[] _rows = new RowUI[3];
        private bool _expanded;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument != null)
            {
                // Mettiamo il pannello leggermente sopra la HUD base (TopBar usa 50)
                _uiDocument.sortingOrder = 60;
            }
        }

        private void OnEnable()
        {
            _service = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            if (_service != null)
                _service.OnChanged += Refresh;

            SetupUI();
            _expanded = _startExpanded;
            ApplyExpandedState();
            Refresh();
        }

        private void OnDisable()
        {
            if (_service != null)
                _service.OnChanged -= Refresh;
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

            _rows[0] = RowUI.Bind(_root, 0);
            _rows[1] = RowUI.Bind(_root, 1);
            _rows[2] = RowUI.Bind(_root, 2);

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

            // Header severity
            ApplySeverityClass(_headerButton, _service.GetHeaderSeverity());

            // Visible rows (max 3, danger pinned)
            var rows = _service.GetVisibleRows();
            for (int i = 0; i < _rows.Length; i++)
            {
                if (i < rows.Count)
                {
                    _rows[i].Show(rows[i]);
                }
                else
                {
                    _rows[i].Hide();
                }
            }
        }

        private static void ApplySeverityClass(VisualElement el, NotificationSeverity severity)
        {
            if (el == null) return;
            el.EnableInClassList("nf-sev-info", severity == NotificationSeverity.Info);
            el.EnableInClassList("nf-sev-success", severity == NotificationSeverity.Success);
            el.EnableInClassList("nf-sev-warning", severity == NotificationSeverity.Warning);
            el.EnableInClassList("nf-sev-danger", severity == NotificationSeverity.Danger);
        }

        private sealed class RowUI
        {
            public VisualElement Root;
            public Label Icon;
            public Label Code;
            public Label Msg;
            public int CurrentEntryId;
            public bool HasCurrent;

            public static RowUI Bind(VisualElement root, int idx)
            {
                var row = root.Q<VisualElement>($"nf-row-{idx}");
                if (row != null) row.style.display = DisplayStyle.None;
                return new RowUI
                {
                    Root = row,
                    Icon = row?.Q<Label>($"nf-row-{idx}-icon"),
                    Code = row?.Q<Label>($"nf-row-{idx}-code"),
                    Msg = row?.Q<Label>($"nf-row-{idx}-msg")
                };
            }

            public void Hide()
            {
                if (Root == null) return;
                if (Root.style.display == DisplayStyle.None) return;

                Root.RemoveFromClassList("nf-anim-enter");
                Root.AddToClassList("nf-anim-exit");

                var exitingId = HasCurrent ? CurrentEntryId : -1;
                var exitingCode = Code != null ? Code.text : "";

                EventCallback<TransitionEndEvent> onEnd = null;
                onEnd = (e) =>
                {
                    // Quando finisce la transition, nascondi e pulisci.
                    Root.style.display = DisplayStyle.None;
                    Root.RemoveFromClassList("nf-anim-exit");
                    HasCurrent = false;
                    CurrentEntryId = 0;
                    Root.UnregisterCallback(onEnd);
                };
                Root.RegisterCallback(onEnd);
            }

            public void Show(NotificationEntry entry)
            {
                if (Root == null) return;
                Root.style.display = DisplayStyle.Flex;

                ApplySeverityClass(Root, entry.Severity);

                if (Code != null) Code.text = entry.Code ?? "N/A";
                if (Msg != null) Msg.text = entry.Message ?? string.Empty;
                if (Icon != null) Icon.text = IconFor(entry.Severity);

                var isNew = !HasCurrent || CurrentEntryId != entry.Id;
                HasCurrent = true;
                CurrentEntryId = entry.Id;

                if (isNew)
                {
                    Root.RemoveFromClassList("nf-anim-exit");
                    Root.AddToClassList("nf-anim-enter");

                    // DEBUG_SAFE_FIX: GeometryChangedEvent non scatta sempre quando lo slot è già in layout.
                    // Usiamo lo scheduler UI Toolkit per rimuovere la classe al prossimo tick UI e garantire l'entry.
                    Root.schedule.Execute(() =>
                    {
                        Root.RemoveFromClassList("nf-anim-enter");
                    });
                }
            }

            private static string IconFor(NotificationSeverity severity)
            {
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


