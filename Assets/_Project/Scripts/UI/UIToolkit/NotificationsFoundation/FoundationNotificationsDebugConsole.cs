#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Debug console runtime (IMGUI) per Notifications Foundation.
    /// Dev-only, session-only, hotkey configurabile runtime.
    /// </summary>
    public sealed class FoundationNotificationsDebugConsole : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.N;
        [SerializeField] private bool showOnStart = false;

        private bool _open;
        private Rect _rect;
        private Vector2 _scroll;

        private FoundationNotificationService _service;
        private List<NotificationTypeSpec> _specs;

        // Push inputs
        private int _selectedSpecIndex = 0;
        private NotificationSeverity _severityOverride = NotificationSeverity.Info;
        private bool _useSeverityOverride = false;
        private string _dedupKeyInput = "";
        private string _dangerKeyInput = "POT-001";
        private string _argsMultiline = "potId=POT-001\nph=0.0\ndelta=+500\namount=3\nseedCode=SDE-001\nlocation=Laboratory";

        private void Awake()
        {
            _open = showOnStart;
            _rect = new Rect(Screen.width - 640f, 20f, 620f, 780f);
        }

        private void Update()
        {
            if (!enableConsole) return;

            if (Input.GetKeyDown(toggleKey))
                _open = !_open;

            if (_service == null)
            {
                _service = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            }

            if (_specs == null || _specs.Count == 0)
            {
                _specs = NotificationTypeSpecResolver.GetAll()
                    .OrderBy(s => s.Channel)
                    .ThenBy(s => s.Category)
                    .ThenBy(s => s.Code)
                    .ToList();
            }
        }

        private void OnGUI()
        {
            if (!enableConsole || !_open) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f));

            GUILayout.BeginArea(_rect, boxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Foundation Notifications Debug Console", HeaderStyle());
            if (GUILayout.Button("X", GUILayout.Width(30))) _open = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Toggle Key:", GUILayout.Width(90));
            toggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), GUILayout.TextField(toggleKey.ToString(), GUILayout.Width(110)));
            enableConsole = GUILayout.Toggle(enableConsole, "Enable", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll);

            DrawPushSection();
            GUILayout.Space(8);
            DrawRuntimeTuningSection();
            GUILayout.Space(8);
            DrawViewerSection();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPushSection()
        {
            GUILayout.Label("1) Push / Test", SubHeaderStyle());

            if (_service == null)
            {
                GUILayout.Label("Service non trovato. Assicurati che FoundationNotificationService sia registrato.", LabelStyle());
                return;
            }

            // Spec selection
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("TypeSpec:", LabelStyle());

            string[] names = _specs.Select(s => $"{s.Code} [{s.Channel}/{s.Category}/{s.DefaultSeverity}]").ToArray();
            _selectedSpecIndex = Mathf.Clamp(_selectedSpecIndex, 0, Mathf.Max(0, names.Length - 1));
            _selectedSpecIndex = GUILayout.SelectionGrid(_selectedSpecIndex, names, 1);

            // Overrides
            GUILayout.Space(6);
            _useSeverityOverride = GUILayout.Toggle(_useSeverityOverride, "Override severity");
            if (_useSeverityOverride)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Severity:", GUILayout.Width(70));
                _severityOverride = (NotificationSeverity)GUILayout.SelectionGrid((int)_severityOverride, Enum.GetNames(typeof(NotificationSeverity)), 4);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("DedupKey:", GUILayout.Width(70));
            _dedupKeyInput = GUILayout.TextField(_dedupKeyInput);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Args (key=value per line):", LabelStyle());
            _argsMultiline = GUILayout.TextArea(_argsMultiline, GUILayout.Height(80));

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("PostToast"))
            {
                var spec = _specs[_selectedSpecIndex];
                _service.PostToast(spec.Code, BuildPayloadFromArgs(), _useSeverityOverride ? _severityOverride : null,
                    string.IsNullOrWhiteSpace(_dedupKeyInput) ? null : _dedupKeyInput);
            }
            if (GUILayout.Button("PostItem"))
            {
                var spec = _specs[_selectedSpecIndex];
                var payload = BuildPayloadFromArgs();
                payload.ItemName = payload.Args.TryGetValue("itemName", out var n) ? n : "Item";
                payload.ItemLocation = payload.Args.TryGetValue("location", out var loc) ? loc : "Vault";
                payload.ItemQuantity = payload.Args.TryGetValue("amount", out var a) && int.TryParse(a, out var q) ? q : 1;
                _service.PostItem(spec.Code, payload, _useSeverityOverride ? _severityOverride : null,
                    string.IsNullOrWhiteSpace(_dedupKeyInput) ? null : _dedupKeyInput);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("DangerKey:", GUILayout.Width(70));
            _dangerKeyInput = GUILayout.TextField(_dangerKeyInput);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("UpsertDanger"))
            {
                var spec = _specs[_selectedSpecIndex];
                _service.UpsertDanger(_dangerKeyInput, spec.Code, BuildPayloadFromArgs(),
                    _useSeverityOverride ? _severityOverride : null);
            }
            if (GUILayout.Button("ResolveDanger"))
            {
                _service.ResolveDanger(_dangerKeyInput);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawRuntimeTuningSection()
        {
            GUILayout.Label("2) Runtime tuning (session-only)", SubHeaderStyle());

            if (_service == null) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enabled", GUILayout.Width(110));
            _service.Enabled = GUILayout.Toggle(_service.Enabled, _service.Enabled ? "ON" : "OFF", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("ToastDuration", GUILayout.Width(110));
            float.TryParse(GUILayout.TextField(_service.ToastDurationSeconds.ToString("F1"), GUILayout.Width(80)), out _service.ToastDurationSeconds);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MaxVisibleRows", GUILayout.Width(110));
            int.TryParse(GUILayout.TextField(_service.MaxVisibleRows.ToString(), GUILayout.Width(80)), out _service.MaxVisibleRows);
            _service.MaxVisibleRows = Mathf.Clamp(_service.MaxVisibleRows, 1, 6);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stagger", GUILayout.Width(110));
            _service.EnableStagger = GUILayout.Toggle(_service.EnableStagger, _service.EnableStagger ? "ON" : "OFF", GUILayout.Width(60));
            GUILayout.Label("sec", GUILayout.Width(30));
            float.TryParse(GUILayout.TextField(_service.StaggerSeconds.ToString("F2"), GUILayout.Width(60)), out _service.StaggerSeconds);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("RateLimit", GUILayout.Width(110));
            _service.EnableRateLimit = GUILayout.Toggle(_service.EnableRateLimit, _service.EnableRateLimit ? "ON" : "OFF", GUILayout.Width(60));
            int.TryParse(GUILayout.TextField(_service.RateLimitPerMinute.ToString(), GUILayout.Width(60)), out _service.RateLimitPerMinute);
            GUILayout.Label("/min", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Lore", GUILayout.Width(110));
            _service.EnableLoreScheduler = GUILayout.Toggle(_service.EnableLoreScheduler, _service.EnableLoreScheduler ? "ON" : "OFF", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("LoreMinInterval", GUILayout.Width(110));
            float.TryParse(GUILayout.TextField(_service.LoreMinIntervalSeconds.ToString("F0"), GUILayout.Width(60)), out _service.LoreMinIntervalSeconds);
            GUILayout.Label("sec", GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("PreemptWindow", GUILayout.Width(110));
            float.TryParse(GUILayout.TextField(_service.LorePreemptAfterGameplaySeconds.ToString("F0"), GUILayout.Width(60)), out _service.LorePreemptAfterGameplaySeconds);
            GUILayout.Label("sec", GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Language", GUILayout.Width(110));
            var langNames = Enum.GetNames(typeof(NotificationLanguage));
            int langIdx = (int)_service.LanguageOverride;
            langIdx = GUILayout.SelectionGrid(langIdx, langNames, 3);
            _service.LanguageOverride = (NotificationLanguage)langIdx;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawViewerSection()
        {
            GUILayout.Label("3) Viewer", SubHeaderStyle());
            if (_service == null) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"HeaderSeverity: {_service.GetHeaderSeverity()}  Badge: {_service.GetBadgeCount()}  VisibleRows: {_service.GetVisibleRows().Count}", LabelStyle());

            GUILayout.Space(6);
            GUILayout.Label("Active Dangers:", LabelStyle());
            foreach (var kv in _service.ActiveDangers)
            {
                GUILayout.Label($"- [{kv.Value.Code}] key={kv.Key} :: {kv.Value.Message}", SmallStyle());
            }

            GUILayout.Space(6);
            GUILayout.Label("Active Toasts:", LabelStyle());
            foreach (var t in _service.ActiveToasts.OrderByDescending(x => x.CreatedAtUtc).Take(10))
            {
                GUILayout.Label($"- [{t.Code}] {t.Severity} :: {t.Message}", SmallStyle());
            }

            GUILayout.EndVertical();
        }

        private NotificationPayload BuildPayloadFromArgs()
        {
            var payload = new NotificationPayload();
            var lines = (_argsMultiline ?? "").Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var lineRaw in lines)
            {
                var line = lineRaw.Trim();
                if (line.Length == 0) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                payload.With(key, val);
            }
            return payload;
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static GUIStyle HeaderStyle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 16;
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = Color.cyan;
            return s;
        }

        private static GUIStyle SubHeaderStyle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 13;
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = Color.white;
            return s;
        }

        private static GUIStyle LabelStyle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 12;
            s.normal.textColor = Color.white;
            return s;
        }

        private static GUIStyle SmallStyle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 10;
            s.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            return s;
        }
    }
}
#endif


