using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;
using Sporae.DevTools;

using Random = UnityEngine.Random;

namespace _Project
{
    public class MicroscopeMinigameController : MonoBehaviour
    {
        [Serializable]
        public struct LevelConfig
        {
            public float ArcWindow;
            public float Tolerance;
        }

        public struct LevelResult
        {
            public float Precision;
            public bool Hit;
        }
        
        [SerializeField] private MicroscopeConfig _config;
        [SerializeField] private MicroscopeHUDView _hudView;
        [SerializeField] private MicroscopeDialInput _dialInput;
        [SerializeField] private PlayerClickMover2D _player;
      
        [SerializeField] private List<LevelConfig> _levels;

        private readonly List<LevelResult> _levelResults = new();
        
        public ReadOnlyCollection<LevelResult> LevelResults => _levelResults.AsReadOnly();
        public int CurrentLevel => _currentLevel;
        
        private int _currentLevel;
        private bool _isPlaying;
        private string _currentSporeId; 

        private Inventory _inventoryService;
        private ActionSystem _actionsService;

        private float _levelStartTime;

        private float _targetAngle;
        private float _precision;
        
        private void Start()
        {
            Initialize(Items.SporeGeneric);
            StartRun();
        }

        private void Initialize(string sporeId)
        {
            // Usa ServiceContainer invece di FindObjectOfType
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (gameManager != null)
            {
                _inventoryService = gameManager.PlayerInventory;
                _actionsService = gameManager.ActionSystem;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer!");
            }
            
            _currentSporeId = sporeId;
        }

        public void StartRun(int level = 0)
        {
            _isPlaying = true;
            _targetAngle = Random.Range(0, 360);
            _currentLevel = level;
            
            _hudView.UpdateInRangeArc(_targetAngle, _levels[level].ArcWindow);
            _player.SuspendMovement(_isPlaying);   
            
            if (level == 0)
                _levelResults.Clear();
        }

        public bool NextLevel()
        {
            _levelResults.Add(new LevelResult()
            {
                Precision = _precision,
                Hit = _precision > 100 - _levels[_currentLevel].Tolerance
            });
            
            if (_currentLevel >= _levels.Count - 1)
            {
                _isPlaying = false;
                _player.SuspendMovement(_isPlaying);
                _hudView.ShowResult();
                
                return false;
            }

            StartRun(_currentLevel + 1);
            return true;
        }
        
        private void Update()
        {
            if (!_isPlaying)
                return;

            var currentAngle = _dialInput.CurrentAngle;
            var delta = Mathf.DeltaAngle(currentAngle, _targetAngle);
            var distance = Mathf.Abs(delta);
            
            _precision = (1f - distance / 180f) * 100f;
            
            _hudView.UpdateArrow(currentAngle);
            _hudView.SetPrecision(_precision);
        }
    }
}