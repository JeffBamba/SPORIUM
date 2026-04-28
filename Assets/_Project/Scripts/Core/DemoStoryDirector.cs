using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlayerInventory;
using Sporae.UI.UIToolkit.HUD;
using Sporae.UI.UIToolkit.SeedStorage;
using _Project.Systems.SeedStorage;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Orchestrazione narrativa demo Alpha (beat, VO, gate anti-spoiler).
    /// Presente solo quando <see cref="DemoSessionState.IsDemo"/> è true.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class DemoStoryDirector : MonoBehaviour
    {
        public enum DemoDebugCheckpoint
        {
            Beat1WakeAndWardrobe = 1,
            Beat2Breakfast = 2,
            Beat3SeedStorage = 3
        }

        private const string NarrativeConfigResourcePath = "Demo/DemoAlphaNarrativeConfig";
        private const string WardrobeMissionResourcePath = "Missions/M_Demo_Wardrobe";
        private const string BreakfastMissionResourcePath = "Missions/M_Demo_Breakfast";
        private const string SeedStorageMissionResourcePath = "Missions/M_Demo_SeedStorage";
        private const float Beat1VoStartDelaySeconds = 5f;
        private const float Beat1Group1HoldSeconds = 2f;
        private const float Beat1Group2HoldSeconds = 3f;
        private const float KitchenMissionTriggerDelaySeconds = 2f;
        private const string KitchenRoomId = "kitchen";

        private DemoSessionState _session;
        private RoomTracker _roomTracker;
        private bool _kitchenBeatTriggered;
        private Coroutine _kitchenDelayRoutine;
        private MissionManager _missionManagerSubscribed;
        private bool _postBreakfastNarrativeLaunched;
        private SeedStoragePanelController _seedStoragePanel;
        private CompactBottomBarController _compactBottomBar;
        private bool _beat3SeedStorageAutoplayStarted;
        private bool _waitingForSeedStoragePowerOn;
        private bool _waitingForCryHover;
        private bool _seedStoragePowerOnObserved;
        private bool _cryHoverObserved;

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
            StartCoroutine(BindSeedStorageAndCompactHudRoutine());
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

            if (_seedStoragePanel != null)
            {
                _seedStoragePanel.PanelShown -= HandleSeedStoragePanelShown;
                _seedStoragePanel.PowerToggled -= HandleSeedStoragePowerToggled;
            }

            if (_compactBottomBar != null)
                _compactBottomBar.CryTooltipShown -= HandleCryTooltipShown;
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
            VoRegister reg = config != null ? config.Beat1WakeRegister : DemoAlphaNarrativeDefaults.Beat1WakeRegister;
            var highlightWords = config != null
                ? config.Beat1MissionHighlightWords
                : null;
            string highlightHex = config != null && !string.IsNullOrWhiteSpace(config.MissionHighlightColorHex)
                ? config.MissionHighlightColorHex
                : DemoAlphaNarrativeDefaults.MissionHighlightColorHex;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SporiumLogger.LogInfo(LogCategory.Core, "[Demo] Beat 1 — VO Wake a gruppi.");
#endif

            yield return PlayBeat1TimedLine(
                vo,
                "Protocollo 01 riattivato.\nVault-07...",
                reg,
                Beat1Group1HoldSeconds);

            yield return PlayBeat1TimedLine(
                vo,
                "Operativo.",
                reg,
                1.2f,
                new[] { "Operativo" },
                new[] { "#7FFF7A" });

            yield return PlayBeat1TimedLine(
                vo,
                "La Cupola tiene ancora......",
                reg,
                Beat1Group2HoldSeconds);

            yield return PlayBeat1TimedLine(
                vo,
                "Tu no da quello che vedo.\nMa… stai comunque respirando.",
                reg,
                2.0f);

            yield return PlayBeat1TimedLine(
                vo,
                "Buone notizie: il sistema funziona.\nCattive notizie: ora devi farlo anche tu",
                reg,
                2.0f);

            yield return PlayBeat1TimedLine(
                vo,
                "Quindi ascolta bene, Biologo:\napri quell'armadio e vestiti.",
                reg,
                2.4f,
                highlightWords,
                null,
                highlightHex,
                hideAfterHold: false);

            yield return PlayBeat1TimedLine(
                vo,
                "La fine del mondo è già abbastanza complicata…\nsenza affrontarla in mutande. :)",
                reg,
                2.6f,
                null,
                null,
                highlightHex,
                hideAfterHold: true);

            AppendDemoWardrobeMissionIfPossible();
        }

        private static IEnumerator PlayBeat1TimedLine(
            VoOverlayController vo,
            string line,
            VoRegister register,
            float holdSecondsAfterTyping,
            IReadOnlyList<string> highlightWords = null,
            IReadOnlyList<string> highlightColorHexes = null,
            string fallbackHighlightHex = null,
            bool hideAfterHold = false)
        {
            if (vo == null || string.IsNullOrWhiteSpace(line))
                yield break;

            var presentation = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: false,
                advanceMode: VoSentenceAdvanceMode.AutoReadingPause,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: highlightWords,
                highlightColorHex: string.IsNullOrWhiteSpace(fallbackHighlightHex)
                    ? DemoAlphaNarrativeDefaults.MissionHighlightColorHex
                    : fallbackHighlightHex,
                forceContinueAtEnd: false,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: true,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: highlightColorHexes,
                holdAfterTypingSeconds: holdSecondsAfterTyping);

            bool done = false;
            vo.ShowLine(line, register, null, () => done = true, hideAfterTypingWithoutIdle: false, presentation);

            while (!done)
                yield return null;

            if (hideAfterHold)
                vo.Hide();
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

        }

        private IEnumerator BindSeedStorageAndCompactHudRoutine()
        {
            while (_session != null && _session.IsDemo)
            {
                if (_seedStoragePanel == null)
                {
                    _seedStoragePanel = ServiceContainer.Instance?.Get<SeedStoragePanelController>(suppressWarning: true);
                    if (_seedStoragePanel != null)
                    {
                        _seedStoragePanel.PanelShown -= HandleSeedStoragePanelShown;
                        _seedStoragePanel.PanelShown += HandleSeedStoragePanelShown;
                        _seedStoragePanel.PowerToggled -= HandleSeedStoragePowerToggled;
                        _seedStoragePanel.PowerToggled += HandleSeedStoragePowerToggled;
                        if (_seedStoragePanel.IsOpen && !_beat3SeedStorageAutoplayStarted &&
                            _session != null && _session.IsDemo && _session.CurrentBeat >= 3)
                        {
                            _beat3SeedStorageAutoplayStarted = true;
                            StartCoroutine(RunBeat3SeedStorageAnomalyAutoplay());
                        }
                    }
                }

                if (_compactBottomBar == null)
                {
                    _compactBottomBar = ServiceContainer.Instance?.Get<CompactBottomBarController>(suppressWarning: true);
                    if (_compactBottomBar != null)
                    {
                        _compactBottomBar.CryTooltipShown -= HandleCryTooltipShown;
                        _compactBottomBar.CryTooltipShown += HandleCryTooltipShown;
                    }
                }

                if (_seedStoragePanel != null && _compactBottomBar != null)
                    yield break;

                yield return null;
            }
        }

        private void HandleSeedStoragePanelShown()
        {
            if (_session == null || !_session.IsDemo || _session.CurrentBeat < 3)
                return;

            if (_beat3SeedStorageAutoplayStarted)
                return;

            _beat3SeedStorageAutoplayStarted = true;
            StartCoroutine(RunBeat3SeedStorageAnomalyAutoplay());
        }

        private void HandleSeedStoragePowerToggled(bool isOn)
        {
            if (!_waitingForSeedStoragePowerOn)
                return;
            if (isOn)
                _seedStoragePowerOnObserved = true;
        }

        private void HandleCryTooltipShown()
        {
            if (_waitingForCryHover)
                _cryHoverObserved = true;
        }

        private IEnumerator RunBeat3SeedStorageAnomalyAutoplay()
        {
            var gm = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            SeedStorageSystem seedStorage = gm?.SeedStorageSystem;
            seedStorage?.EnsureDemoBeat3AnomalyState();

            var vo = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
            if (vo == null)
            {
                _waitingForSeedStoragePowerOn = true;
                yield break;
            }

            var config = Resources.Load<DemoAlphaNarrativeConfig>(NarrativeConfigResourcePath);
            VoRegister reg = config != null
                ? config.Beat3SeedStorageIntroRegister
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageIntroRegister;
            var advance = config != null
                ? config.Beat3SeedStorageIntroSentenceAdvance
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageIntroSentenceAdvance;

            string part1 = config != null && !string.IsNullOrWhiteSpace(config.Beat3SeedStorageAnomalyPart1Line)
                ? config.Beat3SeedStorageAnomalyPart1Line
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageAnomalyPart1Line;
            string part2 = config != null && !string.IsNullOrWhiteSpace(config.Beat3SeedStorageAnomalyPart2Line)
                ? config.Beat3SeedStorageAnomalyPart2Line
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageAnomalyPart2Line;
            string powerOnRequest = config != null && !string.IsNullOrWhiteSpace(config.Beat3SeedStorageAnomalyPowerOnRequestLine)
                ? config.Beat3SeedStorageAnomalyPowerOnRequestLine
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageAnomalyPowerOnRequestLine;

            string missionHex = config != null && !string.IsNullOrWhiteSpace(config.MissionHighlightColorHex)
                ? config.MissionHighlightColorHex
                : DemoAlphaNarrativeDefaults.MissionHighlightColorHex;

            yield return PlayVoBlock(
                vo,
                part1,
                reg,
                advance,
                new[] { "Seed Storage", "semi", "spore" },
                new[] { missionHex, missionHex, missionHex });
            yield return PlayVoBlock(
                vo,
                part2,
                VoRegister.RegisterB,
                advance,
                new[] { "STORAGE OFF", "Solo residui organici", "Contenuto perso" },
                new[] { "#FF9F43", "#FF5A5A", "#FF3B30" });
            yield return PlayVoBlock(
                vo,
                powerOnRequest,
                reg,
                advance,
                new[] { "Riaccendi", "Seed Storage" },
                new[] { missionHex, missionHex });

            _waitingForSeedStoragePowerOn = true;
            _seedStoragePowerOnObserved = seedStorage != null && seedStorage.IsOn;
            while (!_seedStoragePowerOnObserved)
                yield return null;
            _waitingForSeedStoragePowerOn = false;

            while (_seedStoragePanel != null && _seedStoragePanel.IsOpen)
                yield return null;

            DemoSeedStorageMission.NotifyRecoveredAndPanelClosed();

            string cryHoverRequest = config != null && !string.IsNullOrWhiteSpace(config.Beat3SeedStorageCryHoverRequestLine)
                ? config.Beat3SeedStorageCryHoverRequestLine
                : DemoAlphaNarrativeDefaults.Beat3SeedStorageCryHoverRequestLine;
            yield return PlayVoBlock(
                vo,
                cryHoverRequest,
                reg,
                advance,
                new[] { "box CRY", "costo fisso" },
                new[] { missionHex, missionHex });

            _waitingForCryHover = true;
            _cryHoverObserved = false;
            while (!_cryHoverObserved)
                yield return null;
            _waitingForCryHover = false;

            string cryCostsExplain = config != null && !string.IsNullOrWhiteSpace(config.Beat3CryTooltipCostsExplainLine)
                ? config.Beat3CryTooltipCostsExplainLine
                : DemoAlphaNarrativeDefaults.Beat3CryTooltipCostsExplainLine;
            yield return PlayVoBlock(
                vo,
                cryCostsExplain,
                reg,
                advance,
                new[] { "CRY", "costi fissi", "ogni giorno" },
                new[] { missionHex, missionHex, missionHex });

            string cryIncomeExplain = config != null && !string.IsNullOrWhiteSpace(config.Beat3CryTooltipIncomeExplainLine)
                ? config.Beat3CryTooltipIncomeExplainLine
                : DemoAlphaNarrativeDefaults.Beat3CryTooltipIncomeExplainLine;
            yield return PlayVoBlock(
                vo,
                cryIncomeExplain,
                reg,
                advance,
                new[] { "missioni", "mercanti", "black market", "trading", "CRY" },
                new[] { missionHex, missionHex, missionHex, missionHex, missionHex });
        }

        private static IEnumerator PlayVoBlock(
            VoOverlayController vo,
            string line,
            VoRegister register,
            VoSentenceAdvanceMode advanceMode,
            IReadOnlyList<string> highlightWords = null,
            IReadOnlyList<string> highlightColorHexes = null)
        {
            if (vo == null || string.IsNullOrWhiteSpace(line))
                yield break;

            var presentation = new VoLinePresentationOptions(
                useMultiSentenceWhenSplit: false,
                advanceMode: advanceMode,
                minReadSeconds: 0.55f,
                readSecondsPerChar: 0.042f,
                continueHintText: "Clicca o Spazio per continuare",
                highlightWords: highlightWords,
                highlightColorHex: DemoAlphaNarrativeDefaults.MissionHighlightColorHex,
                forceContinueAtEnd: true,
                lockWorldInputWhileVisible: true,
                enableCameraFocus: false,
                cameraFocusOrthographicSize: 0f,
                highlightColorHexes: highlightColorHexes);

            bool done = false;
            vo.ShowLine(line, register, null, () => done = true, false, presentation);
            while (!done)
                yield return null;
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

        /// <summary>
        /// Entry point debug per saltare direttamente a un checkpoint demo.
        /// Mantiene il binario runtime reale (stesse missioni/flag), ma evita
        /// di rifare sempre l'intera sequenza durante i test.
        /// </summary>
        public bool DebugJumpToCheckpoint(DemoDebugCheckpoint checkpoint)
        {
            if (_session == null || !_session.IsDemo)
                return false;

            var flags = ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true);
            var mm = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
            if (mm == null)
                return false;

            switch (checkpoint)
            {
                case DemoDebugCheckpoint.Beat1WakeAndWardrobe:
                    _session.SetBeat(1);
                    _kitchenBeatTriggered = false;
                    _postBreakfastNarrativeLaunched = false;
                    _beat3SeedStorageAutoplayStarted = false;
                    _waitingForSeedStoragePowerOn = false;
                    _waitingForCryHover = false;
                    _seedStoragePowerOnObserved = false;
                    _cryHoverObserved = false;
                    flags?.ClearFlag(WardrobeMission.DemoWardrobeFlagKey);
                    flags?.ClearFlag(DemoBreakfastMission.DemoBreakfastCompletedFlagKey);
                    flags?.ClearFlag(DemoSeedStorageMission.DemoSeedStorageFlagKey);
                    AppendDemoWardrobeMissionIfPossible();
                    return true;

                case DemoDebugCheckpoint.Beat2Breakfast:
                    _session.SetBeat(2);
                    _kitchenBeatTriggered = true;
                    _postBreakfastNarrativeLaunched = false;
                    _beat3SeedStorageAutoplayStarted = false;
                    _waitingForSeedStoragePowerOn = false;
                    _waitingForCryHover = false;
                    _seedStoragePowerOnObserved = false;
                    _cryHoverObserved = false;
                    flags?.SetFlag(WardrobeMission.DemoWardrobeFlagKey);
                    flags?.ClearFlag(DemoBreakfastMission.DemoBreakfastCompletedFlagKey);
                    flags?.ClearFlag(DemoSeedStorageMission.DemoSeedStorageFlagKey);
                    AppendDemoBreakfastMissionIfPossible();
                    return true;

                case DemoDebugCheckpoint.Beat3SeedStorage:
                    _session.SetBeat(3);
                    _kitchenBeatTriggered = true;
                    _postBreakfastNarrativeLaunched = true;
                    _beat3SeedStorageAutoplayStarted = false;
                    _waitingForSeedStoragePowerOn = false;
                    _waitingForCryHover = false;
                    _seedStoragePowerOnObserved = false;
                    _cryHoverObserved = false;
                    flags?.SetFlag(WardrobeMission.DemoWardrobeFlagKey);
                    flags?.SetFlag(DemoBreakfastMission.DemoBreakfastCompletedFlagKey);
                    flags?.ClearFlag(DemoSeedStorageMission.DemoSeedStorageFlagKey);
                    AppendDemoSeedStorageMissionIfPossible();
                    return true;

                default:
                    return false;
            }
        }
    }
}
