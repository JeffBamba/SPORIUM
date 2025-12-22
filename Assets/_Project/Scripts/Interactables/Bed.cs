using _Project.Sporae.Core;
using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    public class Bed : MonoBehaviour
    {
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
            
            if (_diaryUI == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "[BED] _diaryUI è NULL!");
                return;
            }
            
            if (canEndDay)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "[BED] Chiamando _diaryUI.Show()");
                _diaryUI.Show();
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "[BED] Non è possibile finire il giorno (CanEndDay = false)");
            }
        }
    }
}