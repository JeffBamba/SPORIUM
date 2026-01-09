using System.Collections.Generic;
using _Project.NightResearch;
using _Project.Sporae.Core;
using Sporae.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

namespace _Project
{
    public class DiaryUI : MonoBehaviour
    {
        [SerializeField] private List<string> _voicesText = new();
        
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _textsLabel;
        [SerializeField] private TextMeshProUGUI _voiceLabel;
        
        [SerializeField] private TextMeshProUGUI _checkWikiLabel;
        [SerializeField] private Button _buttonGoToSleep;

        [SerializeField] private NightResearchUI _nightResearchUI;
        
        private DiaryStatistics _diaryStatistics;
        private DayCycleSystem _dayCycleSystem;
        private ActionSystem _actionSystem;
        
        private void Awake()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _diaryStatistics = ServiceContainer.Instance.Get<DiaryStatistics>();
            // Usa ServiceContainer invece di FindObjectOfType
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _actionSystem = gameManager?.ActionSystem;
            if (_actionSystem == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "ActionSystem non disponibile via ServiceContainer!");
            }
            
            _buttonGoToSleep.onClick.AddListener(HandleGoToSleep);
        }

        public void Show()
        {
            SporiumLogger.LogDebug(LogCategory.UI, "[DiaryUI] Show() chiamato");
            SporiumLogger.LogDebug(LogCategory.UI, $"[DiaryUI] GameObject stato prima: activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
            
            gameObject.SetActive(true);

            // DEBUG_SAFE_FIX: ensure diary panel is rendered on top within the same Canvas hierarchy.
            transform.SetAsLastSibling(); // DEBUG_SAFE_FIX
            
            SporiumLogger.LogDebug(LogCategory.UI, $"[DiaryUI] GameObject stato dopo SetActive(true): activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");

            _titleLabel.text = $"Day {_dayCycleSystem.CurrentDay} - Diary";
            _textsLabel.text = $"" +
                               $"- {_diaryStatistics.ActionsSpent} Actions used\n" +
                               $"- {_diaryStatistics.CryEarned} Cry Earned\n" +
                               $"- {_diaryStatistics.CrySpent} Cry Spent\n" +
                               (_diaryStatistics.FruitsHarvested <= 0 ? "" : $"- {_diaryStatistics.FruitsHarvested} Fruit harvested\n") +
                               (_diaryStatistics.PlantsWatered <= 0 ? "" : $"- {_diaryStatistics.PlantsWatered} Plants watered\n");
            
            if (_voicesText.Count > 0)
                _voiceLabel.text = _voicesText[Random.Range(0, _voicesText.Count)];
            
            SporiumLogger.LogInfo(LogCategory.UI, "[DiaryUI] DiaryUI mostrato correttamente");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleGoToSleep()
        {
            if (_actionSystem.ActionsLeft >= 1)
                _nightResearchUI.Show();
            else 
                _dayCycleSystem.EndDay();
            
            Hide();
        }
    }
}