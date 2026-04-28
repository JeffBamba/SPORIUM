using _Project.Sporae.Core;
using UnityEngine;

namespace Sporae.DevTools
{
    /// <summary>
    /// Debug console DEMO (ALT + D): jump rapido ai beat/milestone.
    /// Aggiornare la lista checkpoint quando vengono aggiunte nuove missioni demo.
    /// </summary>
    public sealed class DemoDebugConsole : MonoBehaviour
    {
        [SerializeField] private bool _enableDebugConsole = true;
        [SerializeField] private bool _showOnStart;

        private bool _isOpen;
        private Rect _windowRect = new Rect(24f, 24f, 520f, 300f);
        private DemoStoryDirector _director;
        private DemoSessionState _session;
        private MissionManager _missionManager;
        private MissionFlagTracker _flags;

        private readonly (string label, DemoStoryDirector.DemoDebugCheckpoint checkpoint)[] _checkpoints =
        {
            ("Beat 1 - Wake / Armadio", DemoStoryDirector.DemoDebugCheckpoint.Beat1WakeAndWardrobe),
            ("Beat 2 - Colazione", DemoStoryDirector.DemoDebugCheckpoint.Beat2Breakfast),
            ("Beat 3 - Seed Storage", DemoStoryDirector.DemoDebugCheckpoint.Beat3SeedStorage)
        };

        private void Awake()
        {
            _isOpen = _showOnStart;
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            _enableDebugConsole = false;
#endif
        }

        private void Update()
        {
            if (!_enableDebugConsole)
                return;

            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (alt && Input.GetKeyDown(KeyCode.D))
            {
                _isOpen = !_isOpen;
                ResolveRuntimeRefs();
            }
        }

        private void ResolveRuntimeRefs()
        {
            _session = ServiceContainer.Instance?.Get<DemoSessionState>(suppressWarning: true);
            _director = ServiceContainer.Instance?.Get<DemoStoryDirector>(suppressWarning: true);
            _missionManager = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            _flags = ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true);
            if (_director == null)
                _director = FindFirstObjectByType<DemoStoryDirector>();
        }

        private void OnGUI()
        {
            if (!_enableDebugConsole || !_isOpen)
                return;

            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "DEMO DEBUG CONSOLE (ALT + D)");
        }

        private void DrawWindow(int _)
        {
            ResolveRuntimeRefs();

            bool isDemo = _session != null && _session.IsDemo;
            GUILayout.Label(isDemo
                ? $"Sessione DEMO attiva | Beat corrente: {_session.CurrentBeat}"
                : "Sessione DEMO non attiva in questa run.");
            GUILayout.Space(8f);

            if (isDemo && _director != null)
            {
                GUILayout.Label("Jump rapido a beat/milestone:");
                foreach (var row in _checkpoints)
                {
                    if (GUILayout.Button(row.label, GUILayout.Height(28f)))
                        _director.DebugJumpToCheckpoint(row.checkpoint);
                }

                GUILayout.Space(8f);
                GUILayout.Label("Utility missioni demo:");
                if (GUILayout.Button("Completa missione corrente", GUILayout.Height(26f)))
                    ForceCompleteCurrentDemoMission();
                if (GUILayout.Button("Reset flag demo missioni", GUILayout.Height(26f)))
                    ResetDemoMissionFlags();
            }
            else
            {
                GUILayout.Label("Director demo non trovato: avvia una sessione 'Gioca demo'.");
            }

            GUILayout.Space(10f);
            GUILayout.Label("Nota: aggiornare questa lista quando aggiungi nuove missioni demo.");
            if (GUILayout.Button("Chiudi", GUILayout.Height(24f)))
                _isOpen = false;

            GUI.DragWindow(new Rect(0, 0, 520f, 20f));
        }

        private void ForceCompleteCurrentDemoMission()
        {
            if (_missionManager == null || _flags == null)
                return;

            var mission = _missionManager.CurrentMissions.Count > 0 ? _missionManager.CurrentMissions[0] : null;
            if (mission?.Config == null)
                return;

            if (WardrobeMission.IsDemoWardrobeConfig(mission.Config))
            {
                _flags.SetFlag(WardrobeMission.DemoWardrobeFlagKey);
            }
            else if (DemoBreakfastMission.IsDemoBreakfastConfig(mission.Config))
            {
                _flags.SetFlag(DemoBreakfastMission.DemoBreakfastCompletedFlagKey);
            }
            else if (DemoSeedStorageMission.IsDemoSeedStorageConfig(mission.Config))
            {
                _flags.SetFlag(DemoSeedStorageMission.DemoSeedStorageFlagKey);
                DemoSeedStorageMission.NotifyRecoveredAndPanelClosed();
            }

            _missionManager.Check();
        }

        private void ResetDemoMissionFlags()
        {
            if (_flags == null)
                return;

            _flags.ClearFlag(WardrobeMission.DemoWardrobeFlagKey);
            _flags.ClearFlag(DemoBreakfastMission.DemoBreakfastCompletedFlagKey);
            _flags.ClearFlag(DemoSeedStorageMission.DemoSeedStorageFlagKey);
            WardrobeMission.RestoreProgressState(false);
            DemoSeedStorageMission.RestoreProgressState(false);
        }
    }
}
