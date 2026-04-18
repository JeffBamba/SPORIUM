using UnityEngine;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Orchestrazione narrativa demo Alpha (beat, cutscene statiche, gate anti-spoiler).
    /// Presente solo quando <see cref="DemoSessionState.IsDemo"/> è true.
    /// </summary>
    public sealed class DemoStoryDirector : MonoBehaviour
    {
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
            var vo = ServiceContainer.Instance?.Get<VoOverlayController>(suppressWarning: true);
            if (vo != null)
                SporiumLogger.LogDebug(LogCategory.Core, "[Demo] VoOverlay pronto (ServiceContainer).");
#endif
        }
    }
}
