using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Quando il puntatore è sopra un elemento UI Toolkit con classe <c>hud-tooltip-host</c>,
    /// imposta un cursore personalizzato (freccia + punto di domanda). Altrimenti ripristina il default.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class HudTooltipCursorDriver : MonoBehaviour
    {
        [SerializeField] private bool _enable = true;
        [Tooltip("Se assegnata, sostituisce la texture generata a runtime.")]
        [SerializeField] private Texture2D _cursorOverride;
        [SerializeField] private Vector2 _hotspotOverride = new Vector2(0f, 0f);

        private UIDocument[] _documents;
        private bool _lastHintActive;

        // #region agent log
        private bool _prevComputedHint;
        private int _logFrameThrottle;

        private static void AgentLogNdjson(string hypothesisId, string location, string message, string dataJson)
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "debug-416e12.log"));
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var sb = new StringBuilder(256);
                sb.Append("{\"sessionId\":\"416e12\",\"hypothesisId\":\"");
                sb.Append(hypothesisId);
                sb.Append("\",\"location\":\"");
                sb.Append(location.Replace("\\", "\\\\").Replace("\"", "\\\""));
                sb.Append("\",\"message\":\"");
                sb.Append((message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""));
                sb.Append("\",\"data\":");
                sb.Append(string.IsNullOrEmpty(dataJson) ? "{}" : dataJson);
                sb.Append(",\"timestamp\":");
                sb.Append(ts);
                sb.Append("}\n");
                File.AppendAllText(path, sb.ToString());
            }
            catch
            {
                /* ignore */
            }
        }

        // #endregion

        private void OnEnable()
        {
            RefreshDocuments();
            ResetOsCursor();
        }

        private void OnDisable()
        {
            ResetOsCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                ResetOsCursor();
        }

        private void RefreshDocuments()
        {
            _documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            if (_documents == null || _documents.Length == 0)
                return;
            Array.Sort(_documents, (a, b) => b.sortingOrder.CompareTo(a.sortingOrder));
        }

        private void LateUpdate()
        {
            if (!_enable)
            {
                if (_lastHintActive)
                    ResetOsCursor();
                return;
            }

            bool overUiRaycast = UIBlocker.IsPointerOverUI();
            if (!overUiRaycast)
            {
                // #region agent log
                if (_prevComputedHint)
                    AgentLogNdjson("H1", "HudTooltipCursorDriver:L56", "pointer_over_ui_false_while_hint_was_true",
                        "{\"from\":\"UIBlocker\"}");
                // #endregion
                _prevComputedHint = false;
                SetHintActive(false);
                return;
            }

            if (_documents == null || _documents.Length == 0)
                RefreshDocuments();

            Vector2 screen = Input.mousePosition;
            bool hint = false;
            var scanSb = new StringBuilder(128);
            int docIndex = 0;
            string breakReason = "none";
            if (_documents != null)
            {
                foreach (var doc in _documents)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null || root.panel == null) continue;

                    Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screen);
                    VisualElement picked = root.panel.Pick(panelPos);
                    bool hasHost = picked != null && HudTooltipCursor.IsUnderTopBarTooltipHost(picked);
                    string pName = picked != null ? (picked.name ?? "?") : "null";
                    scanSb.Append("[i=").Append(docIndex).Append(",sort=").Append(doc.sortingOrder)
                        .Append(",pick=").Append(pName).Append(",host=").Append(hasHost).Append("]");

                    if (picked == null)
                    {
                        docIndex++;
                        continue;
                    }

                    if (hasHost)
                    {
                        hint = true;
                        breakReason = "found_host";
                        docIndex++;
                        break;
                    }

                    // Stesso documento: pick su elemento senza host (es. contenitore flex / gap) — non fermare la scansione:
                    // prova il panel del documento UI successivo (sortingOrder più basso) per overlay tipo game-viewport.
                    breakReason = "continue_next_panel_no_host";
                    docIndex++;
                    continue;
                }
            }

            if (!hint && breakReason == "none")
                breakReason = "no_host_any_panel";

            // #region agent log
            if (hint != _prevComputedHint || (_logFrameThrottle++ % 45 == 0))
            {
                AgentLogNdjson("H3_H4_H5", "HudTooltipCursorDriver:L110", "pick_scan",
                    $"{{\"runId\":\"post-fix\",\"computedHint\":{(hint ? "true" : "false")},\"breakReason\":\"{breakReason}\",\"mouse\":\"{screen.x:F0},{screen.y:F0}\",\"scan\":\"{scanSb.ToString().Replace("\"", "'")}\"}}");
            }

            _prevComputedHint = hint;
            // #endregion

            SetHintActive(hint);
        }

        private void SetHintActive(bool active)
        {
            if (active == _lastHintActive)
                return;

            _lastHintActive = active;
            if (active)
            {
                Texture2D tex = _cursorOverride != null ? _cursorOverride : HudTooltipCursor.GetOrCreateDefaultCursorTexture();
                Vector2 hs = _cursorOverride != null ? _hotspotOverride : new Vector2(HudTooltipCursor.Hotspot.x, HudTooltipCursor.Hotspot.y);
                UnityEngine.Cursor.SetCursor(tex, hs, CursorMode.Auto);
            }
            else
                ResetOsCursor();
        }

        private void ResetOsCursor()
        {
            _lastHintActive = false;
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
