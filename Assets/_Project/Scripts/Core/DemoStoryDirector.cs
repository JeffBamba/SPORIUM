using System.Collections;
using System.Collections.Generic;
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
        private const string SeedStorageMissionResourcePath = "Missions/M_Demo_SeedStorage";
        private const float Beat1VoStartDelaySeconds = 5f;
        private const float KitchenMissionTriggerDelaySeconds = 2f;
        private const string KitchenRoomId = "kitchen";
        private const string StorageRoomId = "storage";

        private DemoSessionState _session;
        private RoomTracker _roomTracker;
        private bool _kitchenBeatTriggered;
        private Coroutine _kitchenDelayRoutine;
        private MissionManager _missionManagerSubscribed;
        private bool _postBreakfastNarrativeLaunched;

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
            StartCoroutine(BindMissionManagerForPostBreakfastRoutine());
            StartCoroutine(DeferredResolveSeedStorageIfAlreadyInRoomRoutine());
        }

        private void OnDestroy()
        {
            if (_missionManagerSubscribed != null)
            {
                _missionManagerSubscribed.OnMissionComplete -= HandleMissionCompletePostBreakfast;
                _missionManagerSubscribed = null;
            }

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
                useMultiSentenceWhenSplit: false,
                advanceMode: advance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: highlightWords,
                highlightColorHex: highlightHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: true,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: null);

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

            if (string.Equals(roomId, StorageRoomId, System.StringComparison.OrdinalIgnoreCase))
                DemoSeedStorageMission.NotifyEnteredStorageRoom();

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
                useMultiSentenceWhenSplit: false,
                advanceMode: kitchenAdvance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: kitchenHighlightWords,
                highlightColorHex: kitchenHighlightHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: false,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: null);

            bool voCompleted = false;
            vo.ShowLine(kitchenLine, kitchenReg, null, () => voCompleted = true, false, presentation);
            while (!voCompleted)
                yield return null;

            AppendDemoBreakfastMissionIfPossible();
            _session.SetBeat(2);
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

        private IEnumerator BindMissionManagerForPostBreakfastRoutine()
        {
            while (_session != null && _session.IsDemo)
            {
                var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
                if (mm != null)
                {
                    _missionManagerSubscribed = mm;
                    mm.OnMissionComplete += HandleMissionCompletePostBreakfast;
                    yield break;
                }

                yield return null;
            }
        }

        private void HandleMissionCompletePostBreakfast(MissionChecker checker)
        {
            if (_session == null || !_session.IsDemo)
                return;
            if (checker?.Config == null || !DemoBreakfastMission.IsDemoBreakfastConfig(checker.Config))
                return;
            if (_postBreakfastNarrativeLaunched)
                return;
            _postBreakfastNarrativeLaunched = true;
            StartCoroutine(RunPostBreakfastTutorialAndBeat3Vo());
        }

        private IEnumerator RunPostBreakfastTutorialAndBeat3Vo()
        {
            var vo = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
            if (vo == null)
            {
                _session?.SetBeat(3);
                AppendDemoSeedStorageMissionIfPossible();
                yield break;
            }

            var config = Resources.Load<DemoAlphaNarrativeConfig>(NarrativeConfigResourcePath);

            VoRegister postReg = config != null
                ? config.Beat2PostBreakfastRegister
                : DemoAlphaNarrativeDefaults.Beat2PostBreakfastRegister;
            var postAdvance = config != null
                ? config.Beat2PostBreakfastSentenceAdvance
                : DemoAlphaNarrativeDefaults.Beat2PostBreakfastSentenceAdvance;
            string postFallbackHex = config != null && !string.IsNullOrWhiteSpace(config.MissionHighlightColorHex)
                ? config.MissionHighlightColorHex
                : DemoAlphaNarrativeDefaults.MissionHighlightColorHex;

            string part1 = config != null && !string.IsNullOrWhiteSpace(config.Beat2PostBreakfastPart1Line)
                ? config.Beat2PostBreakfastPart1Line
                : DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart1Line;
            var w1 = ResolvePostBreakfastPart1HighlightWords(config);
            var h1 = ResolvePostBreakfastPart1HighlightHexes(config, w1);

            var presentation1 = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: false,
                advanceMode: postAdvance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: w1,
                highlightColorHex: postFallbackHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: false,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: h1);

            bool part1Done = false;
            vo.ShowLine(part1, postReg, null, () => part1Done = true, false, presentation1);
            while (!part1Done)
                yield return null;

            string part2 = config != null && !string.IsNullOrWhiteSpace(config.Beat2PostBreakfastPart2Line)
                ? config.Beat2PostBreakfastPart2Line
                : DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart2Line;
            var w2 = ResolvePostBreakfastPart2HighlightWords(config);
            var h2 = ResolvePostBreakfastPart2HighlightHexes(config, w2);

            var presentation2 = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: false,
                advanceMode: postAdvance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: w2,
                highlightColorHex: postFallbackHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: false,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: h2);

            bool part2Done = false;
            vo.ShowLine(part2, postReg, null, () => part2Done = true, false, presentation2);
            while (!part2Done)
                yield return null;

            _session.SetBeat(3);

            string b3Line = config != null && !string.IsNullOrWhiteSpace(config.Beat3SeedStorageIntroLine)
                ? config.Beat3SeedStorageIntroLine
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageIntroLine;
            VoRegister b3Reg = config != null
                ? config.Beat3SeedStorageIntroRegister
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageIntroRegister;
            var b3Advance = config != null
                ? config.Beat3SeedStorageIntroSentenceAdvance
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageIntroSentenceAdvance;
            var b3Words = ResolveBeat3IntroHighlightWords(config);
            string b3Hex = config != null && !string.IsNullOrWhiteSpace(config.MissionHighlightColorHex)
                ? config.MissionHighlightColorHex
                : DemoAlphaNarrativeDefaults.MissionHighlightColorHex;

            var b3Presentation = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: false,
                advanceMode: b3Advance,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: b3Words,
                highlightColorHex: b3Hex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: false,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: null);

            bool b3Done = false;
            vo.ShowLine(b3Line, b3Reg, null, () => b3Done = true, false, b3Presentation);
            while (!b3Done)
                yield return null;

            AppendDemoSeedStorageMissionIfPossible();
        }

        private void AppendDemoSeedStorageMissionIfPossible()
        {
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            var cfg = Resources.Load<MissionConfig>(SeedStorageMissionResourcePath);
            if (mm == null || cfg == null)
                return;

            if (!mm.AppendIfMissing(cfg))
                return;

            var rt = ServiceContainer.Instance?.Get<RoomTracker>(suppressWarning: true);
            if (rt != null && string.Equals(rt.CurrentRoomId, StorageRoomId, System.StringComparison.OrdinalIgnoreCase))
                DemoSeedStorageMission.NotifyEnteredStorageRoom();
        }

        private IEnumerator DeferredResolveSeedStorageIfAlreadyInRoomRoutine()
        {
            for (var i = 0; i < 90; i++)
            {
                yield return null;
                if (_session == null || !_session.IsDemo)
                    yield break;

                var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
                var rt = ServiceContainer.Instance?.Get<RoomTracker>(suppressWarning: true);
                if (mm != null && rt != null &&
                    DemoSeedStorageMission.HasActiveDemoSeedStorageMission(mm) &&
                    string.Equals(rt.CurrentRoomId, StorageRoomId, System.StringComparison.OrdinalIgnoreCase))
                {
                    DemoSeedStorageMission.NotifyEnteredStorageRoom();
                    yield break;
                }
            }
        }

        private static IReadOnlyList<string> ResolvePostBreakfastPart1HighlightWords(DemoAlphaNarrativeConfig config)
        {
            if (config?.Beat2PostBreakfastPart1HighlightWords != null && config.Beat2PostBreakfastPart1HighlightWords.Count > 0)
                return config.Beat2PostBreakfastPart1HighlightWords;
            return DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart1HighlightWords;
        }

        private static IReadOnlyList<string> ResolvePostBreakfastPart1HighlightHexes(
            DemoAlphaNarrativeConfig config, IReadOnlyList<string> words)
        {
            if (words == null || words.Count == 0)
                return null;
            if (config?.Beat2PostBreakfastPart1HighlightColorHexes != null &&
                config.Beat2PostBreakfastPart1HighlightColorHexes.Count == words.Count)
                return config.Beat2PostBreakfastPart1HighlightColorHexes;
            if (words.Count == DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart1HighlightWords.Count)
                return DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart1HighlightColorHexes;
            return null;
        }

        private static IReadOnlyList<string> ResolvePostBreakfastPart2HighlightWords(DemoAlphaNarrativeConfig config)
        {
            if (config?.Beat2PostBreakfastPart2HighlightWords != null && config.Beat2PostBreakfastPart2HighlightWords.Count > 0)
                return config.Beat2PostBreakfastPart2HighlightWords;
            return DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart2HighlightWords;
        }

        private static IReadOnlyList<string> ResolvePostBreakfastPart2HighlightHexes(
            DemoAlphaNarrativeConfig config, IReadOnlyList<string> words)
        {
            if (words == null || words.Count == 0)
                return null;
            if (config?.Beat2PostBreakfastPart2HighlightColorHexes != null &&
                config.Beat2PostBreakfastPart2HighlightColorHexes.Count == words.Count)
                return config.Beat2PostBreakfastPart2HighlightColorHexes;
            if (words.Count == DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart2HighlightWords.Count)
                return DemoAlphaNarrativeDefaults.Beat2PostBreakfastPart2HighlightColorHexes;
            return null;
        }

        private static IReadOnlyList<string> ResolveBeat3IntroHighlightWords(DemoAlphaNarrativeConfig config)
        {
            if (config?.Beat3SeedStorageIntroHighlightWords != null && config.Beat3SeedStorageIntroHighlightWords.Count > 0)
                return config.Beat3SeedStorageIntroHighlightWords;
            return DemoAlphaNarrativeDefaults.Beat3SeedStorageIntroHighlightWords;
        }
    }
}
