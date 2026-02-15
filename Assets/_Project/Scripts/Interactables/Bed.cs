using _Project.Sporae.Core;
using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    public class Bed : MonoBehaviour
    {
        [Header("End of Day — prefer EoD sequence (UIToolkit)")]
        [SerializeField] private EndOfDaySequenceController _eodController;
        [SerializeField] private DiaryUI _diaryUI;

        private Interactable _interactable;
        private DayCycleSystem _dayCycleSystem;

        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            _interactable.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
        }

        private void HandleInteract()
        {
            SporiumLogger.LogDebug(LogCategory.UI, "[BED] HandleInteract chiamato");

            bool canEndDay = _dayCycleSystem.CanEndDay();
            SporiumLogger.LogDebug(LogCategory.UI, $"[BED] CanEndDay: {canEndDay}");

            if (!canEndDay)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[BED] Non è possibile finire il giorno (CanEndDay = false)");
                return;
            }

            if (_eodController != null)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "[BED] Avvio sequenza EoD (UIToolkit)");
                _eodController.StartSequence();
                return;
            }

            if (_diaryUI != null)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "[BED] Chiamando _diaryUI.Show() (fallback)");
                _diaryUI.Show();
                return;
            }

            SporiumLogger.LogError(LogCategory.UI, "[BED] Né EoD controller né DiaryUI assegnati!");
        }
    }
}