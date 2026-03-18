using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project;
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
            Prune,
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

        private readonly Dictionary<string, Queue<AutomationAction>> _potQueues = new();
        private readonly HashSet<string> _runningPotIds = new();
        private readonly Dictionary<string, AutomationAction> _currentActions = new();

        private GameManager _gameManager;
        private FoundationNotificationService _foundation;

        private void Awake()
        {
            _gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            if (_gameManager == null)
                _gameManager = FindObjectOfType<GameManager>();

            _foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
        }

        public bool HasPending => HasQueuedActions() || _runningPotIds.Count > 0;

        public bool EnqueueAndRun(IEnumerable<AutomationAction> actions)
        {
            if (actions == null) return true;

            var list = new List<AutomationAction>();
            int totalAp = 0;
            foreach (var a in actions)
            {
                if (a == null || string.IsNullOrEmpty(a.PotId)) continue;
                list.Add(a);
                totalAp += Mathf.Max(0, a.ApCost);
            }
            if (list.Count == 0) return true;

            if (_gameManager == null || !_gameManager.TrySpendAction(totalAp))
            {
                PostToast("POT-AUTO-ERROR", $"Automation failed: insufficient AP ({totalAp})");
                return false;
            }

            EnqueueBatch(list);

            return true;
        }

        public bool HasPlantPendingOrRunning(string potId)
        {
            if (string.IsNullOrEmpty(potId))
                return false;

            if (_currentActions.TryGetValue(potId, out var running)
                && running != null
                && running.Type == AutomationActionType.Plant)
                return true;

            if (_potQueues.TryGetValue(potId, out var queue))
            {
                foreach (var a in queue)
                {
                    if (a != null && a.Type == AutomationActionType.Plant)
                        return true;
                }
            }

            return false;
        }

        public void EnqueueBatch(IEnumerable<AutomationAction> actions)
        {
            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a == null || string.IsNullOrEmpty(a.PotId)) continue;
                if (!_potQueues.TryGetValue(a.PotId, out var queue))
                {
                    queue = new Queue<AutomationAction>();
                    _potQueues[a.PotId] = queue;
                }
                queue.Enqueue(a);
                EnsurePotCoroutine(a.PotId);
            }
        }

        /// <summary>
        /// Consuma AP immediatamente e avvia esecuzione con delay scenico.
        /// </summary>
        public bool ConfirmAndRun()
        {
            if (!HasQueuedActions()) return true;

            int totalAp = 0;
            foreach (var q in _potQueues.Values)
            {
                foreach (var a in q)
                    totalAp += a != null ? Mathf.Max(0, a.ApCost) : 0;
            }

            if (_gameManager == null || !_gameManager.TrySpendAction(totalAp))
            {
                PostToast("POT-AUTO-ERROR", $"Automation failed: insufficient AP ({totalAp})");
                return false;
            }

            foreach (var potId in new List<string>(_potQueues.Keys))
                EnsurePotCoroutine(potId);

            return true;
        }

        private IEnumerator RunPotQueue(string potId)
        {
            if (string.IsNullOrEmpty(potId))
                yield break;

            while (_potQueues.TryGetValue(potId, out var queue) && queue.Count > 0)
            {
                var action = queue.Dequeue();
                if (action == null) continue;
                _currentActions[potId] = action;
                RecordDomeActionForDiary(action);
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

                // If the arm animation has an explicit scenic duration, sync the runner delay
                // (and therefore the in-progress + success timing) to what the player sees.
                if (armAnimator != null)
                {
                    float scenicDuration = armAnimator.GetConfiguredScenicDurationSeconds(action.Type);
                    if (scenicDuration > 0f)
                        delay = scenicDuration;
                }

                delay = Mathf.Max(0.25f, delay);
                string dangerKey = $"POT-AUTO:{action.PotId}";
                _foundation?.UpsertDanger(dangerKey, "POT-AUTO-INPROGRESS",
                    new NotificationPayload().With("message", $"{action.PotId}: {action.Type} in progress — ETA {Mathf.CeilToInt(delay)}s"));
                // Keep progress visible until success; avoid expiring toast.

                armAnimator?.StartActionAnimation(action.Type, action.PotId);
                yield return new WaitForSeconds(delay);

                ExecuteAction(action);
                armAnimator?.StopAnimation();
                _foundation?.ResolveDanger(dangerKey);
                _currentActions.Remove(potId);
            }
            _runningPotIds.Remove(potId);
            _currentActions.Remove(potId);
        }

        private void EnsurePotCoroutine(string potId)
        {
            if (string.IsNullOrEmpty(potId)) return;
            if (_runningPotIds.Contains(potId)) return;

            _runningPotIds.Add(potId);
            StartCoroutine(RunPotQueue(potId));
        }

        private bool HasQueuedActions()
        {
            foreach (var q in _potQueues.Values)
            {
                if (q != null && q.Count > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Registra l'azione nel diario non appena è "in progress" (toast visibile),
        /// così il Report EoD la include anche se il giocatore va a letto prima della fine.
        /// </summary>
        private void RecordDomeActionForDiary(AutomationAction action)
        {
            if (action == null || string.IsNullOrEmpty(action.PotId)) return;
            var dayActivityLog = ServiceContainer.Instance?.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog == null) return;

            string actionKind = MapAutomationTypeToActionKind(action.Type);
            string plantCode = null;
            string plantDisplayName = null;
            if (action.Type == AutomationActionType.Plant && !string.IsNullOrEmpty(action.ItemTypeId))
            {
                var plantData = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(action.ItemTypeId);
                if (plantData != null)
                {
                    plantCode = plantData.PlantCode ?? "";
                    plantDisplayName = plantData.name ?? plantData.PlantCode ?? plantCode;
                }
                else
                    plantCode = action.ItemTypeId;
            }

            dayActivityLog.RecordDomeAction(new DayActivityLog.DomeActivityEntry
            {
                PotId = action.PotId,
                ActionKind = actionKind,
                PlantCode = plantCode,
                PlantDisplayName = plantDisplayName
            });
        }

        private static string MapAutomationTypeToActionKind(AutomationActionType type)
        {
            switch (type)
            {
                case AutomationActionType.Plant: return "Plant";
                case AutomationActionType.HydrationToggle: return "Water";
                case AutomationActionType.LedRedToggle:
                case AutomationActionType.LedBlueToggle: return "Light";
                case AutomationActionType.Fertilize: return "Fertilize";
                case AutomationActionType.Prune: return "Pruning";
                default: return "Started";
            }
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
                        case AutomationActionType.Prune:
                            ok = pot.PotActions.DoPruning(useSpray: false);
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
                _foundation.PostToast(code, new NotificationPayload().With("message", text));
        }
    }
}

