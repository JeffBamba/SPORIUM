using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Single source of truth per Notifications (ex novo).
    /// Non è MonoBehaviour: va tickato da un runner.
    /// </summary>
    public sealed class FoundationNotificationService
    {
        // Runtime config (session-only) — modificabile da debug console
        public bool Enabled = true;
        public float ToastDurationSeconds = 8f;
        public int MaxVisibleRows = 5;
        public bool EnableStagger = true;
        public float StaggerSeconds = 0.1f;

        public bool EnableLoreScheduler = true;
        // Evita che le LORE partano "subito" ad inizio partita: warmup dopo boot.
        public float LoreWarmupSeconds = 30f;
        public float LoreMinIntervalSeconds = 60f;
        public float LoreCooldownBufferSeconds = 10f;
        public float LorePreemptAfterGameplaySeconds = 6f; // gameplay-preempt window

        public bool EnableRateLimit = true;
        public int RateLimitPerMinute = 20;

        public NotificationLanguage LanguageOverride
        {
            get => NotificationLocalization.OverrideLanguage;
            set => NotificationLocalization.OverrideLanguage = value;
        }

        private readonly Dictionary<string, NotificationEntry> _activeDangersByKey = new();
        private readonly List<NotificationEntry> _activeToasts = new();
        private readonly Queue<PendingEmission> _pending = new();
        private readonly Dictionary<string, float> _lastEmitByKey = new(); // cooldown gate
        private readonly Queue<float> _rateWindow = new(); // realtimeSinceStartup timestamps

        private int _nextId = 1;
        private float _lastLoreRealtime = -9999f;
        private float _lastGameplayRealtime = -9999f;
        private readonly float _startupRealtime;

        // Events for UI/debug
        public event Action OnChanged;

        /// <summary>
        /// Fired ogni volta che PostAddedToInventory viene chiamato con successo.
        /// Usato da CollectionBoxStackController per creare box persistenti.
        /// </summary>
        public event Action<NotificationPayload> OnItemAdded;

        public FoundationNotificationService()
        {
            _startupRealtime = Time.realtimeSinceStartup;
        }

        public IReadOnlyDictionary<string, NotificationEntry> ActiveDangers => _activeDangersByKey;
        public IReadOnlyList<NotificationEntry> ActiveToasts => _activeToasts;

        public void Tick(float realtimeSinceStartup)
        {
            if (!Enabled) return;

            // Expire toasts
            int expired = 0;
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                var t = _activeToasts[i];
                if (t.ExpiresAtRealtime.HasValue && realtimeSinceStartup >= t.ExpiresAtRealtime.Value)
                {
                    _activeToasts.RemoveAt(i);
                    expired++;
                }
            }

            // DEBUG_SAFE_FIX: UI refresh on expiry (otherwise OnChanged never fires and toasts can "stick" visually)
            if (expired > 0)
                OnChanged?.Invoke();

            // Rate window cleanup
            while (_rateWindow.Count > 0 && realtimeSinceStartup - _rateWindow.Peek() > 60f)
                _rateWindow.Dequeue();

            // Drain pending with stagger
            if (_pending.Count > 0)
            {
                var next = _pending.Peek();
                if (realtimeSinceStartup >= next.NotBeforeRealtime)
                {
                    _pending.Dequeue();
                    ApplyEmission(next, realtimeSinceStartup);
                }
            }

            // Notify once per tick if needed (simple)
            // (UI reads snapshots; the service triggers OnChanged on state modifications)
        }

        /// <summary>
        /// Mostra un toast temporaneo (non persistente). Dedup per cooldown su (code + optional key).
        /// </summary>
        public void PostToast(string code, NotificationPayload payload = null, NotificationSeverity? severityOverride = null, string dedupKey = null)
        {
            if (!Enabled) return;

            if (!NotificationTypeSpecResolver.TryGet(code, out var spec))
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"FoundationNotificationService: spec non trovato per code={code}");
                return;
            }

            // Lore preempt: se è lore, applica gating
            if (spec.Channel == NotificationChannel.Lore && !CanEmitLore(Time.realtimeSinceStartup))
                return;

            var effectiveSeverity = severityOverride ?? spec.DefaultSeverity;
            var key = BuildCooldownKey(code, dedupKey);
            if (!PassesCooldown(spec, key))
                return;

            if (!PassesRateLimit())
                return;

            var msg = BuildMessage(spec, payload);

            Enqueue(new PendingEmission
            {
                Kind = EmissionKind.Toast,
                Code = code,
                Spec = spec,
                Severity = effectiveSeverity,
                Message = msg,
                DedupKey = dedupKey,
                Payload = payload
            });

            MarkEmit(key, spec);
            if (spec.Channel == NotificationChannel.Lore) _lastLoreRealtime = Time.realtimeSinceStartup;
            else _lastGameplayRealtime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Aggiorna o inserisce un toast con chiave dedup: se esiste già un toast con questo dedupKey,
        /// ne aggiorna messaggio e scadenza; altrimenti ne aggiunge uno nuovo. Non applica cooldown.
        /// Utile per progressi (es. "Estrazione in Corso.. xx%").
        /// </summary>
        public void UpsertToast(string dedupKey, string code, NotificationPayload payload = null, NotificationSeverity? severityOverride = null)
        {
            if (!Enabled) return;
            if (string.IsNullOrWhiteSpace(dedupKey)) return;

            if (!NotificationTypeSpecResolver.TryGet(code, out var spec))
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"FoundationNotificationService: spec non trovato per code={code}");
                return;
            }

            var effectiveSeverity = severityOverride ?? spec.DefaultSeverity;
            var msg = BuildMessage(spec, payload);
            var now = Time.realtimeSinceStartup;
            var expires = now + ResolveToastDurationSeconds(effectiveSeverity);

            for (int i = 0; i < _activeToasts.Count; i++)
            {
                if (_activeToasts[i].DedupKey != dedupKey) continue;

                var existing = _activeToasts[i];
                var updated = new NotificationEntry(existing.Id, code, dedupKey, spec, effectiveSeverity, msg, existing.CreatedAtUtc, expires, existing.Payload);
                _activeToasts[i] = updated;
                _lastGameplayRealtime = now;
                OnChanged?.Invoke();
                return;
            }

            var id = _nextId++;
            var createdAt = DateTime.UtcNow;
            _activeToasts.Add(new NotificationEntry(id, code, dedupKey, spec, effectiveSeverity, msg, createdAt, expires, payload));
            _lastGameplayRealtime = now;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Mostra un toast subito senza passare dalla coda (nessun cooldown/rate limit).
        /// Utile dopo RemoveToast per mostrare il toast di completamento nella stessa frame.
        /// </summary>
        public void PostToastImmediate(string code, NotificationPayload payload = null, NotificationSeverity? severityOverride = null)
        {
            if (!Enabled) return;

            if (!NotificationTypeSpecResolver.TryGet(code, out var spec))
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"FoundationNotificationService: spec non trovato per code={code}");
                return;
            }

            var effectiveSeverity = severityOverride ?? spec.DefaultSeverity;
            var msg = BuildMessage(spec, payload);
            var now = Time.realtimeSinceStartup;
            var expires = now + ResolveToastDurationSeconds(effectiveSeverity);
            var id = _nextId++;
            var createdAt = DateTime.UtcNow;
            var entry = new NotificationEntry(id, code, null, spec, effectiveSeverity, msg, createdAt, expires, payload);
            _activeToasts.Add(entry);
            _lastGameplayRealtime = now;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Rimuove il toast con la chiave dedup indicata (es. prima di mostrare il toast di successo).
        /// </summary>
        public void RemoveToast(string dedupKey)
        {
            if (!Enabled) return;
            if (string.IsNullOrWhiteSpace(dedupKey)) return;

            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                if (_activeToasts[i].DedupKey == dedupKey)
                {
                    _activeToasts.RemoveAt(i);
                    OnChanged?.Invoke();
                    return;
                }
            }
        }

        /// <summary>
        /// Mostra subito un toast "Added To Inventory" con payload già popolato (metadati in <see cref="NotificationPayload.Args"/>).
        /// Bypassa coda e rate limit.
        /// </summary>
        public void PostAddedToInventory(NotificationPayload payload)
        {
            if (!Enabled) return;
            if (payload == null) return;
            if (string.IsNullOrEmpty(payload.ItemTypeId)) return;
            if (string.IsNullOrEmpty(payload.ItemName)) payload.ItemName = payload.ItemTypeId;
            if (string.IsNullOrEmpty(payload.ItemLocation)) payload.ItemLocation = "—";
            if (payload.ItemIcon == null)
                payload.ItemIcon = NotificationItemIconResolver.GetIcon(payload.ItemTypeId, payload.ItemSporeStage);
            PostToastImmediate("ADDED-TO-INVENTORY", payload, NotificationSeverity.Success);
            OnItemAdded?.Invoke(payload);
        }

        /// <summary>
        /// Mostra subito un toast "Added To Inventory" con dati item reali (titolo, icona, quantità, nome, room).
        /// Bypassa coda e rate limit. Usare per ogni raccolta/harvest/collection.
        /// </summary>
        public void PostAddedToInventory(string itemTypeId, string itemDisplayName, int quantity, string roomDisplayName)
        {
            if (!Enabled) return;
            if (string.IsNullOrEmpty(itemTypeId)) return;
            if (string.IsNullOrEmpty(itemDisplayName)) itemDisplayName = itemTypeId;
            if (string.IsNullOrEmpty(roomDisplayName)) roomDisplayName = "—";

            var payload = new NotificationPayload
            {
                ItemTypeId = itemTypeId,
                ItemName = itemDisplayName,
                ItemQuantity = quantity,
                ItemLocation = roomDisplayName
            };
            PostAddedToInventory(payload);
        }

        /// <summary>
        /// Toast item layout (tipo ITEM-GET). Sempre temporaneo.
        /// </summary>
        public void PostItem(string code, NotificationPayload payload, NotificationSeverity? severityOverride = null, string dedupKey = null)
        {
            if (!Enabled) return;
            if (payload == null) payload = new NotificationPayload();

            if (!NotificationTypeSpecResolver.TryGet(code, out var spec))
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"FoundationNotificationService: spec non trovato per code={code}");
                return;
            }

            var effectiveSeverity = severityOverride ?? spec.DefaultSeverity;
            var key = BuildCooldownKey(code, dedupKey);
            if (!PassesCooldown(spec, key))
                return;

            if (!PassesRateLimit())
                return;

            var msg = BuildMessage(spec, payload);

            Enqueue(new PendingEmission
            {
                Kind = EmissionKind.Item,
                Code = code,
                Spec = spec,
                Severity = effectiveSeverity,
                Message = msg,
                DedupKey = dedupKey,
                Payload = payload
            });

            MarkEmit(key, spec);
            _lastGameplayRealtime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// DANGER persistente state-driven: Upsert per key (dedup).
        /// </summary>
        public void UpsertDanger(string key, string code, NotificationPayload payload = null, NotificationSeverity? severityOverride = null)
        {
            if (!Enabled) return;
            if (string.IsNullOrWhiteSpace(key)) return;

            if (!NotificationTypeSpecResolver.TryGet(code, out var spec))
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"FoundationNotificationService: spec non trovato per code={code}");
                return;
            }

            var effectiveSeverity = severityOverride ?? spec.DefaultSeverity;
            var msg = BuildMessage(spec, payload);

            // Cooldown per key (evita spam di update)
            var cooldownKey = BuildCooldownKey(code, key);
            if (!PassesCooldown(spec, cooldownKey))
                return;

            var createdAt = DateTime.UtcNow;
            var id = _nextId++;

            if (_activeDangersByKey.TryGetValue(key, out var existing))
            {
                // Update in-place: preserva ID per stabilità UI
                var updated = new NotificationEntry(existing.Id, code, key, spec, effectiveSeverity, msg, existing.CreatedAtUtc, null, existing.Payload);
                _activeDangersByKey[key] = updated;
            }
            else
            {
                _activeDangersByKey[key] = new NotificationEntry(id, code, key, spec, effectiveSeverity, msg, createdAt, null, null);
            }

            MarkEmit(cooldownKey, spec);
            _lastGameplayRealtime = Time.realtimeSinceStartup;
            OnChanged?.Invoke();
        }

        public void ResolveDanger(string key)
        {
            if (!Enabled) return;
            if (string.IsNullOrWhiteSpace(key)) return;
            if (_activeDangersByKey.Remove(key))
                OnChanged?.Invoke();
        }

        /// <summary>
        /// Snapshot della lista visibile (max 5): DANGER pinnati sopra, poi toast temporanei.
        /// </summary>
        public IReadOnlyList<NotificationEntry> GetVisibleRows()
        {
            var rows = new List<NotificationEntry>(MaxVisibleRows);

            // DANGER pinned top — più recenti in alto
            var dangers = _activeDangersByKey.Values
                .OrderByDescending(d => d.CreatedAtUtc)
                .ToList();

            foreach (var d in dangers)
            {
                if (rows.Count >= MaxVisibleRows) break;
                rows.Add(d);
            }

            if (rows.Count >= MaxVisibleRows)
                return rows;

            // Toast temporanei sotto, più recenti in alto
            var toasts = _activeToasts
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToList();

            foreach (var t in toasts)
            {
                if (rows.Count >= MaxVisibleRows) break;
                rows.Add(t);
            }

            return rows;
        }

        public NotificationSeverity GetHeaderSeverity()
        {
            // severità massima tra attivi/visibili
            if (_activeDangersByKey.Count == 0 && _activeToasts.Count == 0)
                return NotificationSeverity.Idle;

            var max = NotificationSeverity.Idle;
            foreach (var d in _activeDangersByKey.Values)
                if (d.Severity > max) max = d.Severity;
            foreach (var t in _activeToasts)
                if (t.Severity > max) max = t.Severity;
            return max;
        }

        public int GetBadgeCount()
        {
            // Nel reference badge mostra count attivo (dangers+toasts), non solo visibili.
            return _activeDangersByKey.Count + _activeToasts.Count;
        }

        public bool CanEmitLore(float realtimeSinceStartup)
        {
            if (!EnableLoreScheduler) return false;
            if (LoreWarmupSeconds > 0f && (realtimeSinceStartup - _startupRealtime) < LoreWarmupSeconds)
            {
                return false;
            }
            if (_activeDangersByKey.Count > 0) return false;
            if (realtimeSinceStartup - _lastGameplayRealtime < LorePreemptAfterGameplaySeconds) return false;
            if (realtimeSinceStartup - _lastLoreRealtime < Mathf.Max(LoreMinIntervalSeconds, LoreCooldownBufferSeconds)) return false;
            return true;
        }

        private void Enqueue(PendingEmission emission)
        {
            var now = Time.realtimeSinceStartup;
            var notBefore = now;

            if (EnableStagger && _pending.Count > 0)
            {
                // schedule after last pending
                notBefore = _pending.Last().NotBeforeRealtime + StaggerSeconds;
            }
            else if (EnableStagger && _pending.Count == 0 && StaggerSeconds > 0f)
            {
                // slight delay for cascade effect only when multiple are emitted quickly:
                notBefore = now;
            }

            emission.NotBeforeRealtime = notBefore;
            _pending.Enqueue(emission);
        }

        private void ApplyEmission(PendingEmission emission, float realtimeSinceStartup)
        {
            // DANGER occupy slots rule is handled by UI snapshot; we still keep toasts even if hidden.
            var createdAt = DateTime.UtcNow;
            var id = _nextId++;

            float? expires = null;
            if (emission.Kind == EmissionKind.Toast || emission.Kind == EmissionKind.Item)
                expires = realtimeSinceStartup + ResolveToastDurationSeconds(emission.Severity);

            var entry = new NotificationEntry(id, emission.Code, emission.DedupKey, emission.Spec, emission.Severity, emission.Message, createdAt, expires, emission.Payload);

            _activeToasts.Add(entry);
            OnChanged?.Invoke();
        }

        private float ResolveToastDurationSeconds(NotificationSeverity severity)
        {
            if (severity == NotificationSeverity.Success)
                return ToastDurationSeconds + 5f;
            return ToastDurationSeconds;
        }

        private string BuildMessage(NotificationTypeSpec spec, NotificationPayload payload)
        {
            var template = NotificationLocalization.ResolveTemplate(spec);
            var args = payload?.Args;
            var msg = NotificationLocalization.Format(template, args);
            return msg;
        }

        private string BuildCooldownKey(string code, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return code;
            return $"{code}::{key}";
        }

        private bool PassesCooldown(NotificationTypeSpec spec, string cooldownKey)
        {
            if (spec == null) return true;
            if (spec.CooldownSeconds <= 0f) return true;

            var now = Time.realtimeSinceStartup;
            if (_lastEmitByKey.TryGetValue(cooldownKey, out var last))
            {
                if (now - last < spec.CooldownSeconds)
                    return false;
            }
            return true;
        }

        private void MarkEmit(string cooldownKey, NotificationTypeSpec spec)
        {
            if (spec == null || spec.CooldownSeconds <= 0f) return;
            _lastEmitByKey[cooldownKey] = Time.realtimeSinceStartup;
        }

        private bool PassesRateLimit()
        {
            if (!EnableRateLimit) return true;
            var now = Time.realtimeSinceStartup;
            while (_rateWindow.Count > 0 && now - _rateWindow.Peek() > 60f)
                _rateWindow.Dequeue();

            if (_rateWindow.Count >= RateLimitPerMinute)
                return false;

            _rateWindow.Enqueue(now);
            return true;
        }

        private enum EmissionKind { Toast, Item }

        private struct PendingEmission
        {
            public EmissionKind Kind;
            public string Code;
            public string DedupKey;
            public NotificationTypeSpec Spec;
            public NotificationSeverity Severity;
            public string Message;
            public NotificationPayload Payload;
            public float NotBeforeRealtime;
        }
    }
}


