using System.Collections;
using UnityEngine;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlayerInventory;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Orchestrazione narrativa demo Alpha (beat, VO, gate anti-spoiler).
    /// Presente solo quando <see cref="DemoSessionState.IsDemo"/> è true.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class DemoStoryDirector : MonoBehaviour
    {
        private const string NarrativeConfigResourcePath = "Demo/DemoAlphaNarrativeConfig";
        private const string WardrobeMissionResourcePath = "Missions/M_Demo_Wardrobe";
        private const string BreakfastMissionResourcePath = "Missions/M_Demo_Breakfast";
        private const float Beat1VoStartDelaySeconds = 5f;
        private const float KitchenMissionTriggerDelaySeconds = 2f;
        private const string KitchenRoomId = "kitchen";

        private DemoSessionState _session;
        private RoomTracker _roomTracker;
        private bool _kitchenBeatTriggered;
        private Coroutine _kitchenDelayRoutine;

        private void Awake()
        {
            _session = ServiceContainer.Instance?.Get<DemoSessionState>(suppressWarning: true);
        }

        private void Start()
        {
            if (_session == null || !_session.IsDemo)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SporiumLogger.LogInfo(LogCategory.Core, "[Demo] DemoStoryDirector: sessione demo attiva.");
#endif
            StartCoroutine(BindRoomTrackerRoutine());
            StartCoroutine(RunBeat1WakeIntro());
        }

        private void OnDestroy()
        {
            if (_roomTracker != null)
                _roomTracker.OnRoomChanged -= HandleRoomChanged;
            if (_kitchenDelayRoutine != null)
            {
                StopCoroutine(_kitchenDelayRoutine);
                _kitchenDelayRoutine = null;
            }
        }

        /// <summary>Beat 1 — Wake: imposta traccia e prima riga VO (integrazione Task 1+2).</summary>
        private IEnumerator RunBeat1WakeIntro()
        {
            _session.SetBeat(1);

            var vo = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
            if (vo == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                SporiumLogger.LogWarning(LogCategory.Core, "[Demo] VoOverlayController assente — VO beat 1 saltato.");
#endif
                yield break;
            }

            yield return null;
            yield return new WaitForSeconds(Beat1VoStartDelaySeconds);

            var config = Resources.Load<DemoAlphaNarrativeConfig>(NarrativeConfigResourcePath);
            string line = config != null && !string.IsNullOrWhiteSpace(config.Beat1WakeLine)
                ? config.Beat1WakeLine
                : DemoAlphaNarrativeDefaults.Beat1WakeLine;
            VoRegister reg = config != null ? config.Beat1WakeRegister : DemoAlphaNarrativeDefaults.Beat1WakeRegister;
            var advance = config != null
                ? config.Beat1WakeSentenceAdvance
                : DemoAlphaNarrativeDefaults.Beat1WakeSentenceAdvance;
            var highlightWords = config != null
                ? config.Beat1MissionHighlightWords
                : null;
            string highlightHex = config != null && !string.IsNullOrWhiteSpace(config.MissionHighlightColorHex)
                ? config.MissionHighlightColorHex
                : DemoAlphaNarrativeDefaults.MissionHighlightColorHex;
            var presentation = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: true,
                advanceMode: advance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: highlightWords,
                highlightColorHex: highlightHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: true,
                cameraFocusOrthographicSize: 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SporiumLogger.LogInfo(LogCategory.Core, "[Demo] Beat 1 — VO Wake (prima riga).");
#endif
            vo.ShowLine(line, reg, null, AppendDemoWardrobeMissionIfPossible, hideAfterTypingWithoutIdle: false, presentation);
        }

        private void AppendDemoWardrobeMissionIfPossible()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            var cfg = Resources.Load<MissionConfig>(WardrobeMissionResourcePath);
            if (mm != null && cfg != null)
            {
                mm.AppendIfMissing(cfg);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SporiumLogger.LogWarning(LogCategory.Core,
                "[Demo] Missione demo armadio non aggiunta: MissionManager o Resources M_Demo_Wardrobe assente.");
#endif
        }

        private IEnumerator BindRoomTrackerRoutine()
        {
            while (_session != null && _session.IsDemo && _roomTracker == null)
            {
                BindRoomTrackerIfAvailable();
                if (_roomTracker != null)
                    yield break;
                yield return null;
            }
        }

        private void BindRoomTrackerIfAvailable()
        {
            if (_roomTracker != null)
                return;

            _roomTracker = ServiceContainer.Instance?.Get<RoomTracker>(suppressWarning: true);
            if (_roomTracker == null)
                return;

            _roomTracker.OnRoomChanged -= HandleRoomChanged;
            _roomTracker.OnRoomChanged += HandleRoomChanged;
        }

        private void HandleRoomChanged(string roomId)
        {
            if (_session == null || !_session.IsDemo)
                return;
            if (_kitchenBeatTriggered)
                return;

            bool isKitchen = string.Equals(roomId, KitchenRoomId, System.StringComparison.OrdinalIgnoreCase);
            if (!isKitchen)
            {
                if (_kitchenDelayRoutine != null)
                {
                    StopCoroutine(_kitchenDelayRoutine);
                    _kitchenDelayRoutine = null;
                }
                return;
            }

            var flags = ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true);
            if (flags == null || !flags.HasFlag(WardrobeMission.DemoWardrobeFlagKey))
                return;

            if (_kitchenDelayRoutine == null)
                _kitchenDelayRoutine = StartCoroutine(TriggerKitchenBreakfastBeatWithDelay());
        }

        private IEnumerator TriggerKitchenBreakfastBeatWithDelay()
        {
            yield return new WaitForSeconds(KitchenMissionTriggerDelaySeconds);
            _kitchenDelayRoutine = null;

            if (_kitchenBeatTriggered)
                yield break;
            if (_roomTracker == null || !string.Equals(_roomTracker.CurrentRoomId, KitchenRoomId, System.StringComparison.OrdinalIgnoreCase))
                yield break;

            var flags = ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true);
            if (flags == null || !flags.HasFlag(WardrobeMission.DemoWardrobeFlagKey))
                yield break;

            _kitchenBeatTriggered = true;
            StartCoroutine(RunKitchenBreakfastBeat());
        }

        private IEnumerator RunKitchenBreakfastBeat()
        {
            var vo = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
            if (vo == null)
                yield break;

            var config = Resources.Load<DemoAlphaNarrativeConfig>(NarrativeConfigResourcePath);

            string kitchenLine = config != null && !string.IsNullOrWhiteSpace(config.Beat2KitchenLine)
                ? config.Beat2KitchenLine
                : DemoAlphaNarrativeDefaults.Beat2KitchenLine;
            VoRegister kitchenReg = config != null
                ? config.Beat2KitchenRegister
                : DemoAlphaNarrativeDefaults.Beat2KitchenRegister;
            var kitchenAdvance = config != null
                ? config.Beat2KitchenSentenceAdvance
                : DemoAlphaNarrativeDefaults.Beat2KitchenSentenceAdvance;
            var kitchenHighlightWords = config != null && config.Beat2MissionHighlightWords != null && config.Beat2MissionHighlightWords.Count > 0
                ? (System.Collections.Generic.IReadOnlyList<string>)config.Beat2MissionHighlightWords
                : DemoAlphaNarrativeDefaults.Beat2MissionHighlightWords;
            string kitchenHighlightHex = config != null && !string.IsNullOrWhiteSpace(config.MissionHighlightColorHex)
                ? config.MissionHighlightColorHex
                : DemoAlphaNarrativeDefaults.MissionHighlightColorHex;

            var presentation = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: true,
                advanceMode: kitchenAdvance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: kitchenHighlightWords,
                highlightColorHex: kitchenHighlightHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: false,
                cameraFocusOrthographicSize: 0f);

            bool voCompleted = false;
            vo.ShowLine(kitchenLine, kitchenReg, null, () => voCompleted = true, false, presentation);
            while (!voCompleted)
                yield return null;

            AppendDemoBreakfastMissionIfPossible();
        }

        private void AppendDemoBreakfastMissionIfPossible()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            var cfg = Resources.Load<MissionConfig>(BreakfastMissionResourcePath);
            if (mm == null || cfg == null)
                return;

            mm.AppendIfMissing(cfg);

            var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            var panel = ServiceContainer.Instance?.Get<PlayerInventoryPanelController>(suppressWarning: true);
            if (gm?.PlayerInventory == null || panel == null)
                return;

            DemoBreakfastMission.BeginTracking(gm.PlayerInventory, panel);
        }
    }
}
