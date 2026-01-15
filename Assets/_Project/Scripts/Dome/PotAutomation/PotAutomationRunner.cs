using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace Sporae.Dome.PotAutomation
{
    /// <summary>
    /// Esegue una coda di azioni sui Pot con delay scenico (60-180s).
    /// MVP: supporta solo UPROOT (per validare pipeline end-to-end).
    /// Non distruttivo: se non presente in scena non fa nulla.
    /// </summary>
    public class PotAutomationRunner : MonoBehaviour
    {
        [Serializable]
        public enum AutomationActionType
        {
            Plant,
            Fertilize,
            Spray,
            HydrationToggle,
            LedRedToggle,
            LedBlueToggle,
            Harvest,
            Uproot
        }

        [Serializable]
        public class AutomationAction
        {
            public AutomationActionType Type;
            public string PotId;
            public int ApCost = 1;

            // Optional payload
            public string ItemTypeId; // seed/fertilizer/additive
            public bool Irrigate;     // Plant only (future)
        }

        [Header("Timing")]
        [SerializeField] private Vector2 delaySecondsRange = new Vector2(60f, 180f);
        [SerializeField] private bool useArmAnimation = true;
        [SerializeField, Range(0.1f, 2f)] private float delayMultiplier = 0.25f;

        private readonly Queue<AutomationAction> _queue = new();
        private bool _isRunning;

        private GameManager _gameManager;
        private FoundationNotificationService _foundation;

        private void Awake()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (_gameManager == null)
                _gameManager = FindObjectOfType<GameManager>();

            _foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
        }

        public bool HasPending => _queue.Count > 0 || _isRunning;

        public void EnqueueBatch(IEnumerable<AutomationAction> actions)
        {
            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a == null || string.IsNullOrEmpty(a.PotId)) continue;
                _queue.Enqueue(a);
            }
        }

        /// <summary>
        /// Consuma AP immediatamente e avvia esecuzione con delay scenico.
        /// </summary>
        public bool ConfirmAndRun()
        {
            if (_queue.Count == 0) return true;

            int totalAp = 0;
            foreach (var a in _queue)
                totalAp += a != null ? Mathf.Max(0, a.ApCost) : 0;

            if (_gameManager == null || !_gameManager.TrySpendAction(totalAp))
            {
                PostToast("POT-AUTO-ERROR", $"Automation failed: insufficient AP ({totalAp})");
                return false;
            }

            if (!_isRunning)
                StartCoroutine(RunLoop());

            return true;
        }

        private IEnumerator RunLoop()
        {
            _isRunning = true;

            while (_queue.Count > 0)
            {
                var action = _queue.Dequeue();
                if (action == null) continue;

                PotSlot pot = null;
                PotAutomationArmAnimator armAnimator = null;
                if (useArmAnimation)
                {
                    pot = FindPotById(action.PotId);
                    if (pot != null)
                        armAnimator = pot.GetComponentInChildren<PotAutomationArmAnimator>(includeInactive: true);
                }

                float effectiveMultiplier = delayMultiplier * 0.5f;
                float delay = UnityEngine.Random.Range(delaySecondsRange.x, delaySecondsRange.y) * effectiveMultiplier;
                delay = Mathf.Max(1f, delay);
                string dangerKey = $"POT-AUTO:{action.PotId}";
                _foundation?.UpsertDanger(dangerKey, "POT-AUTO-INPROGRESS",
                    new NotificationPayload().With("message", $"{action.PotId}: {action.Type} in progress — ETA {Mathf.CeilToInt(delay)}s"));
                // Keep progress visible until success; avoid expiring toast.

                // #region agent log
                try
                {
                    System.IO.File.AppendAllText("d:\\Sporae_Build_Beta\\.cursor\\debug.log",
                        "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix\",\"hypothesisId\":\"H9\",\"location\":\"PotAutomationRunner.RunLoop\",\"message\":\"inprogress_toast\",\"data\":{\"potId\":\"" + action.PotId + "\",\"type\":\"" + action.Type + "\",\"delay\":" + delay.ToString("F2") + ",\"delayMultiplier\":" + delayMultiplier.ToString("F2") + ",\"effectiveMultiplier\":" + effectiveMultiplier.ToString("F2") + "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
                }
                catch { }
                // #endregion

                armAnimator?.StartActionAnimation(action.Type, action.PotId);
                yield return new WaitForSeconds(delay);

                ExecuteAction(action);
                armAnimator?.StopAnimation();
                _foundation?.ResolveDanger(dangerKey);

                // #region agent log
                try
                {
                    System.IO.File.AppendAllText("d:\\Sporae_Build_Beta\\.cursor\\debug.log",
                        "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix\",\"hypothesisId\":\"H10\",\"location\":\"PotAutomationRunner.RunLoop\",\"message\":\"action_complete\",\"data\":{\"potId\":\"" + action.PotId + "\",\"type\":\"" + action.Type + "\",\"dangerKey\":\"" + dangerKey + "\",\"potActive\":" + (pot != null && pot.gameObject.activeInHierarchy ? "true" : "false") + "},\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
                }
                catch { }
                // #endregion
            }

            _isRunning = false;
        }

        private void ExecuteAction(AutomationAction action)
        {
            try
            {
                var pot = FindPotById(action.PotId);
                if (pot == null || pot.PotActions == null)
                {
                    PostToast("POT-AUTO-ERROR", $"{action.PotId}: action failed (pot not found)");
                    return;
                }

                bool ok = false;
                using (pot.PotActions.BeginAutomationContext())
                {
                    switch (action.Type)
                    {
                        case AutomationActionType.Plant:
                            ok = pot.PotActions.DoPlant(action.ItemTypeId, action.Irrigate);
                            break;
                        case AutomationActionType.Fertilize:
                            ok = pot.PotActions.DoFertilize(action.ItemTypeId);
                            break;
                        case AutomationActionType.Spray:
                            ok = pot.PotActions.DoApplyAdditive(action.ItemTypeId);
                            break;
                        case AutomationActionType.HydrationToggle:
                            ok = pot.PotActions.DoWater();
                            break;
                        case AutomationActionType.LedRedToggle:
                            ok = pot.PotActions.DoLight(LedType.Red);
                            break;
                        case AutomationActionType.LedBlueToggle:
                            ok = pot.PotActions.DoLight(LedType.Blue);
                            break;
                        case AutomationActionType.Harvest:
                            ok = pot.PotActions.DoHarvest();
                            break;
                        case AutomationActionType.Uproot:
                            ok = pot.PotActions.DoUproot();
                            break;
                    }
                }

                PostToast(ok ? "POT-AUTO-SUCCESS" : "POT-AUTO-ERROR", $"{action.PotId}: {action.Type} {(ok ? "completed" : "failed")}");
            }
            catch (Exception ex)
            {
                // Make failures visible in-game instead of silently stopping the automation.
                PostToast("POT-AUTO-ERROR", $"{action?.PotId ?? "POT-???"}: exception {ex.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private static PotSlot FindPotById(string potId)
        {
            var all = FindObjectsOfType<PotSlot>();
            foreach (var p in all)
            {
                if (p != null && string.Equals(p.PotId, potId, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        private void PostToast(string code, string text)
        {
            // MVP: se NotificationsFoundation non è disponibile, fallback Debug.Log.
            if (_foundation != null)
            {
                _foundation.PostToast(code, new NotificationPayload().With("message", text));
                return;
            }
            Debug.Log($"[PotAutomationRunner] {code}: {text}");
        }
    }
}

