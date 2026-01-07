using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

/// <summary>
/// Runner per watchers state-driven: legge stato di gioco e Upsert/Resolve DANGER/WARNING persistenti.
/// Dev'essere attivato via feature flag (coexistence).
/// </summary>
namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    public sealed class FoundationNotificationsWatchersRunner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _pollIntervalSeconds = 0.5f;
        [SerializeField] private float _refreshPotSlotsIntervalSeconds = 2f;

        private FoundationNotificationService _service;
        private PhSystem _phSystem;
        private PotSystemConfig _potConfig;
        private PotSlot[] _potSlots = new PotSlot[0];

        private float _nextPoll;
        private float _nextRefreshPots;
        private bool _primed;

        private void Awake()
        {
            _service = ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning: true);
            // PhSystem non è un MonoBehaviour/UnityEngine.Object: va recuperato dal ServiceContainer.
            _phSystem = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            _potConfig = Resources.Load<PotSystemConfig>("Configs/PotSystemConfig");

            RefreshPotSlots();

            if (_phSystem != null)
            {
                _phSystem.OnPhChanged += OnPhChanged;
            }

            // Evita notifiche al primissimo snapshot (inizio partita): priming nel primo poll.
            _primed = false;
        }

        private void OnDestroy()
        {
            if (_phSystem != null)
                _phSystem.OnPhChanged -= OnPhChanged;
        }

        private void Update()
        {
            if (_service == null || !_service.Enabled) return;

            var now = Time.realtimeSinceStartup;
            if (now >= _nextRefreshPots)
            {
                _nextRefreshPots = now + _refreshPotSlotsIntervalSeconds;
                RefreshPotSlots();
            }

            if (now < _nextPoll) return;
            _nextPoll = now + _pollIntervalSeconds;

            // Primo snapshot: non emettere nulla all'avvio.
            if (!_primed)
            {
                _primed = true;
                return;
            }

            EvaluatePhUltra();
            EvaluatePots();
        }

        private void RefreshPotSlots()
        {
            _potSlots = FindObjectsOfType<PotSlot>();
        }

        private void OnPhChanged(float ph, float delta)
        {
            // Re-evaluate immediato su cambio pH
            if (!_primed) return;
            EvaluatePhUltra();
        }

        private void EvaluatePhUltra()
        {
            if (_service == null || _phSystem == null) return;

            var band = _phSystem.EvaluateState();
            var phStr = _phSystem.CurrentPh.ToString("F1");

            if (band == PhSystem.PhBand.UltraAcid)
            {
                _service.UpsertDanger("PH:ULTRA:ACID", "PH-ULTRA-ACID", new NotificationPayload().With("ph", phStr));
            }
            else
            {
                _service.ResolveDanger("PH:ULTRA:ACID");
            }

            if (band == PhSystem.PhBand.UltraBasic)
            {
                _service.UpsertDanger("PH:ULTRA:BASIC", "PH-ULTRA-BASIC", new NotificationPayload().With("ph", phStr));
            }
            else
            {
                _service.ResolveDanger("PH:ULTRA:BASIC");
            }

            // pH instabile (warning persistente) per bande stabili non neutrali
            if (band == PhSystem.PhBand.StableAcid || band == PhSystem.PhBand.StableBasic)
            {
                _service.UpsertDanger("PH:UNSTABLE", "PH-003", new NotificationPayload().With("ph", phStr));
            }
            else
            {
                _service.ResolveDanger("PH:UNSTABLE");
            }
        }

        private void EvaluatePots()
        {
            if (_potSlots == null || _potSlots.Length == 0) return;

            int maxHydration = _potConfig != null ? _potConfig.MaxHydration : 10;
            int maxDaysStress = _potConfig != null ? _potConfig.MaxDaysForFullStress : 5;

            foreach (var potSlot in _potSlots)
            {
                if (potSlot == null || potSlot.PotActions == null) continue;
                var state = potSlot.PotActions.GetCurrentState();
                if (state == null || !state.HasPlant) continue;

                string potId = state.PotId;

                // ---- Overwatering (WARNING + DANGER) ----
                int hydrationPercent = maxHydration > 0 ? Mathf.RoundToInt((float)state.Hydration / maxHydration * 100f) : 0;
                if (hydrationPercent > DifficultyCalibrationConfig.HydrationWetThreshold)
                {
                    _service.UpsertDanger($"WAT:OVR:WARN:{potId}", "WAT-OVR-WARN",
                        new NotificationPayload()
                            .With("potId", potId)
                            .With("pct", hydrationPercent.ToString()));
                }
                else
                {
                    _service.ResolveDanger($"WAT:OVR:WARN:{potId}");
                }

                if (state.DaysOverwateringConsecutive >= 2)
                {
                    _service.UpsertDanger($"WAT:OVR:DANGER:{potId}", "WAT-OVR-DANGER",
                        new NotificationPayload()
                            .With("potId", potId)
                            .With("days", state.DaysOverwateringConsecutive.ToString()));
                }
                else
                {
                    _service.ResolveDanger($"WAT:OVR:DANGER:{potId}");
                }

                // ---- Light stress 100% (DANGER) ----
                int consecutiveLedDays = Mathf.Max(state.DaysLedBlueConsecutive, state.DaysLedRedConsecutive);
                float stressPct = maxDaysStress > 0 ? Mathf.Clamp01((float)consecutiveLedDays / maxDaysStress) * 100f : 0f;
                if (stressPct >= 100f)
                {
                    _service.UpsertDanger($"LGT:STR:{potId}", "LGT-STR-100",
                        new NotificationPayload().With("potId", potId));
                }
                else
                {
                    _service.ResolveDanger($"LGT:STR:{potId}");
                }

                // ---- Fertilizer missing / out-of-range ----
                var plantData = PlantDatabase.Instance != null ? PlantDatabase.Instance.GetPlantDataByCode(state.PlantCode) : null;
                if (plantData != null)
                {
                    PlantStage stage = (PlantStage)state.Stage;
                    var stageReq = plantData.GetStageRequirements(stage);
                    bool fertilizerOptional = (stage == PlantStage.Seed || stage == PlantStage.Sprout);

                    bool fertilizerInRange = stageReq != null && stageReq.IsFertilizerInRange(state.FertilizerLevel);
                    if (!fertilizerOptional)
                    {
                        if (state.FertilizerLevel <= 0)
                        {
                            _service.UpsertDanger($"FRT:MISS:{potId}", "FRT-MISSING-BLOCK",
                                new NotificationPayload().With("potId", potId));
                        }
                        else
                        {
                            _service.ResolveDanger($"FRT:MISS:{potId}");
                        }

                        if (state.FertilizerLevel > 0 && !fertilizerInRange)
                        {
                            _service.UpsertDanger($"FRT:OUT:{potId}", "FRT-OUT-RANGE",
                                new NotificationPayload().With("potId", potId));
                        }
                        else
                        {
                            _service.ResolveDanger($"FRT:OUT:{potId}");
                        }
                    }
                    else
                    {
                        _service.ResolveDanger($"FRT:MISS:{potId}");
                        _service.ResolveDanger($"FRT:OUT:{potId}");
                    }
                }
                else
                {
                    _service.ResolveDanger($"FRT:MISS:{potId}");
                    _service.ResolveDanger($"FRT:OUT:{potId}");
                }

                // ---- Mold ----
                if (state.IsInfested)
                {
                    _service.UpsertDanger($"MLD:INF:{potId}", "MLD-INFESTED",
                        new NotificationPayload().With("potId", potId));

                    // Codice tematico richiesto (ref): MLD-201
                    _service.UpsertDanger($"MLD:201:{potId}", "MLD-201",
                        new NotificationPayload().With("potId", potId));
                }
                else
                {
                    _service.ResolveDanger($"MLD:INF:{potId}");
                    _service.ResolveDanger($"MLD:201:{potId}");
                }

                if (state.MoldRiskLevel >= 3)
                {
                    _service.UpsertDanger($"MLD:RISK:{potId}", "MLD-RISK-CRIT",
                        new NotificationPayload().With("potId", potId));
                }
                else
                {
                    _service.ResolveDanger($"MLD:RISK:{potId}");
                }

                // ---- pH extreme per pot countdown ----
                if (state.ExtremePhDeathCountdown > 0)
                {
                    _service.UpsertDanger($"PH:RISK:{potId}", "PH-RISK-COUNTDOWN",
                        new NotificationPayload()
                            .With("potId", potId)
                            .With("plant", state.PlantCode ?? "Plant")
                            .With("days", state.ExtremePhDeathCountdown.ToString()));
                }
                else
                {
                    _service.ResolveDanger($"PH:RISK:{potId}");
                }
            }
        }
    }
}


