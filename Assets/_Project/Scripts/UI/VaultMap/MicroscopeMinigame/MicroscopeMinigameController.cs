using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;

using Random = UnityEngine.Random;

namespace _Project
{
    public class MicroscopeMinigameController : MonoBehaviour
    {
        [SerializeField] private MicroscopeConfig _config;
        [SerializeField] private MicroscopeHUDView _hudView;
        [SerializeField] private MicroscopeDialInput _dialInput;
        [SerializeField] private PlayerClickMover2D _player;
      
        [SerializeField] private int _levels;

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
            var gameManager = FindObjectOfType<GameManager>();
            
            _inventoryService = gameManager.PlayerInventory;
            _actionsService = gameManager.ActionSystem;
            
            _currentSporeId = sporeId;
        }

        public void StartRun()
        {
            _isPlaying = true;
            _targetAngle = Random.Range(0, 360);
            
            _hudView.Show();
            _hudView.UpdateInRangeArc(_targetAngle);
            _player.SuspendMovement(_isPlaying);   
        }

        public void CancelRun()
        {
            _isPlaying = false;
            _player.SuspendMovement(_isPlaying);
            _hudView.Hide();
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