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
    /// Missione demo «Vai al Seed Storage»: completata entrando nella stanza con room id <c>storage</c>.
    /// </summary>
    public static class DemoSeedStorageMission
    {
        public const string DemoSeedStorageFlagKey = "demo_seed_storage_visited";
        public const string DemoSeedStorageMissionConfigName = "M_Demo_SeedStorage";

        public static event Action ProgressChanged;

        private static bool _enteredStorage;

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
        /// Chiamato quando il player entra nell’area Seed Storage (RoomTracker room id <c>storage</c>).
        /// </summary>
        public static void NotifyEnteredStorageRoom()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            if (mm == null || !HasActiveDemoSeedStorageMission(mm))
                return;
            if (_enteredStorage)
                return;

            _enteredStorage = true;
            ProgressChanged?.Invoke();
            ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true)
                ?.SetFlag(DemoSeedStorageFlagKey);
        }

        public static bool IsDemoSeedStorageConfig(MissionConfig cfg) =>
            cfg != null && string.Equals(cfg.name, DemoSeedStorageMissionConfigName, StringComparison.Ordinal);

        public static float GetObjectiveProgress01(MissionConfig cfg)
        {
            if (!IsDemoSeedStorageConfig(cfg))
                return -1f;
            return _enteredStorage ? 1f : 0f;
        }

        public static void RestoreProgressState(bool completed)
        {
            _enteredStorage = completed;
            ProgressChanged?.Invoke();
        }
    }
}
