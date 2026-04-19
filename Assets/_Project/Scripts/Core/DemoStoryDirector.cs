using System.Collections;
using UnityEngine;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.DevTools;

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
        private const float Beat1VoStartDelaySeconds = 5f;

        private DemoSessionState _session;

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
            StartCoroutine(RunBeat1WakeIntro());
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
            var presentation = VoLinePresentationOptions.ForDemoBeat(advance, highlightWords, highlightHex);

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
                mm.Append(cfg);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SporiumLogger.LogWarning(LogCategory.Core,
                "[Demo] Missione demo armadio non aggiunta: MissionManager o Resources M_Demo_Wardrobe assente.");
#endif
        }
    }
}
