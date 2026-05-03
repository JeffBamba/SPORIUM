using System;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Hook missione Armadio (Both). Il flag è idempotente: può essere inviato sia in apertura che in chiusura pannello.
    /// </summary>
    public static class WardrobeMission
    {
        public const string DemoWardrobeFlagKey = "demo_wardrobe";
        public const string DemoWardrobeMissionConfigName = "M_Demo_Wardrobe";

        /// <summary>Notifica avanzamento mission recap (0/100) per il task armadio.</summary>
        public static event System.Action ProgressChanged;

        private static bool _wardrobeAccessed;

        /// <summary>Segnala che il player ha interagito con il guardaroba.</summary>
        public static void NotifyWardrobeAccessed()
        {
            if (!_wardrobeAccessed)
            {
                _wardrobeAccessed = true;
                ProgressChanged?.Invoke();
            }
            ServiceContainer.Instance?.Get<MissionFlagTracker>()?.SetFlag(DemoWardrobeFlagKey);
        }

        /// <summary>Compatibilità chiamate esistenti.</summary>
        public static void NotifyWardrobeClosed() => NotifyWardrobeAccessed();

        public static bool IsDemoWardrobeConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoWardrobeMissionConfigName, System.StringComparison.Ordinal);

        /// <summary>Progress 0/1 della missione armadio nel recap.</summary>
        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (!IsDemoWardrobeConfig(cfg))
                return -1f;
            return _wardrobeAccessed ? 1f : 0f;
        }

        public static void RestoreProgressState(bool accessed)
        {
            _wardrobeAccessed = accessed;
            ProgressChanged?.Invoke();
        }
    }

    /// <summary>
    /// Missione demo «Vai al Seed Storage»: completata alla chiusura del pannello
    /// Seed Storage dopo la sequenza beat 3 in stanza (VO + visita).
    /// </summary>
    public static class DemoSeedStorageMission
    {
        public const string DemoSeedStorageFlagKey = "demo_seed_storage_visited";
        public const string DemoSeedStorageMissionConfigName = "M_Demo_SeedStorage";

        public static event Action ProgressChanged;

        private static bool _seedStoragePanelClosedDone;

        public static bool HasActiveDemoSeedStorageMission(MissionManager missionManager)
        {
            if (missionManager?.CurrentMissions == null)
                return false;
            foreach (var m in missionManager.CurrentMissions)
            {
                if (m?.Config == null || m.IsCompleted)
                    continue;
                if (IsDemoSeedStorageConfig(m.Config))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Legacy placeholder: non completa più la missione.
        /// </summary>
        public static void NotifyEnteredStorageRoom()
        {
        }

        /// <summary>
        /// Completa la missione demo Seed Storage quando il player chiude il pannello Seed Storage.
        /// </summary>
        public static void NotifySeedStoragePanelClosed()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            if (mm == null || !HasActiveDemoSeedStorageMission(mm))
                return;
            if (_seedStoragePanelClosedDone)
                return;

            _seedStoragePanelClosedDone = true;
            ProgressChanged?.Invoke();
            ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true)
                ?.SetFlag(DemoSeedStorageFlagKey);
        }

        /// <inheritdoc cref="NotifySeedStoragePanelClosed"/>
        public static void NotifyRecoveredAndPanelClosed() => NotifySeedStoragePanelClosed();

        public static bool IsDemoSeedStorageConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoSeedStorageMissionConfigName, StringComparison.Ordinal);

        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (!IsDemoSeedStorageConfig(cfg))
                return -1f;
            return _seedStoragePanelClosedDone ? 1f : 0f;
        }

        public static void RestoreProgressState(bool completed)
        {
            _seedStoragePanelClosedDone = completed;
            ProgressChanged?.Invoke();
        }
    }

    /// <summary>Missione demo «Accedi al PC»: completata aprendo il pannello di controllo sul PC in Camera.</summary>
    public static class DemoPcAccessMission
    {
        public const string DemoPcAccessFlagKey = "demo_pc_access_done";
        public const string DemoPcAccessMissionConfigName = "M_Demo_PcAccess";

        public static event Action ProgressChanged;

        private static bool _controlPanelReached;

        public static bool HasActive(MissionManager missionManager)
        {
            if (missionManager?.CurrentMissions == null)
                return false;
            foreach (var m in missionManager.CurrentMissions)
            {
                if (m?.Config == null || m.IsCompleted)
                    continue;
                if (IsDemoPcAccessConfig(m.Config))
                    return true;
            }

            return false;
        }

        public static void NotifyControlPanelOpened()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            if (mm == null || !HasActive(mm))
                return;
            if (_controlPanelReached)
                return;

            _controlPanelReached = true;
            ProgressChanged?.Invoke();
            ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true)
                ?.SetFlag(DemoPcAccessFlagKey);
        }

        public static bool IsDemoPcAccessConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoPcAccessMissionConfigName, StringComparison.Ordinal);

        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (!IsDemoPcAccessConfig(cfg))
                return -1f;
            return _controlPanelReached ? 1f : 0f;
        }

        public static void RestoreProgressState(bool completed)
        {
            _controlPanelReached = completed;
            ProgressChanged?.Invoke();
        }
    }

    /// <summary>Missione demo «Accendi il Seed Storage»: completata uscendo dal PC dopo l'accensione da pannello di controllo.</summary>
    public static class DemoPcSeedPowerMission
    {
        public const string DemoPcSeedPowerFlagKey = "demo_pc_seed_power_done";
        public const string DemoPcSeedPowerMissionConfigName = "M_Demo_PcSeedPower";

        public static event Action ProgressChanged;

        private static bool _routineDone;

        public static bool HasActive(MissionManager missionManager)
        {
            if (missionManager?.CurrentMissions == null)
                return false;
            foreach (var m in missionManager.CurrentMissions)
            {
                if (m?.Config == null || m.IsCompleted)
                    continue;
                if (IsDemoPcSeedPowerConfig(m.Config))
                    return true;
            }

            return false;
        }

        public static void NotifySeedPowerRoutineComplete()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            if (mm == null || !HasActive(mm))
                return;
            if (_routineDone)
                return;

            _routineDone = true;
            ProgressChanged?.Invoke();
            ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true)
                ?.SetFlag(DemoPcSeedPowerFlagKey);
        }

        public static bool IsDemoPcSeedPowerConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoPcSeedPowerMissionConfigName, StringComparison.Ordinal);

        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (!IsDemoPcSeedPowerConfig(cfg))
                return -1f;
            return _routineDone ? 1f : 0f;
        }

        public static void RestoreProgressState(bool completed)
        {
            _routineDone = completed;
            ProgressChanged?.Invoke();
        }
    }
}
